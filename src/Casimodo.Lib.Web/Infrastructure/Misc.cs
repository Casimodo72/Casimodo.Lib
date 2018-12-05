using Casimodo.Lib;
using Casimodo.Lib.Data;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Http.Controllers;
using Microsoft.AspNet.OData;

// KABU TODO: REVISIT: This file is just a temporary bucket until I find better places.

namespace Casimodo.Lib.Web
{
    public interface ITenantManager
    {
        void SetDefaultTenant();
        void SetTenant(Guid id);
        bool TrySetTenant(Guid id);
    }

    public static class ODataControllerExtensions
    {
        // Many-to-many associations ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        // KABU TODO: Think about using two one-to-many associations instead
        //   of those black-box many-to-many associations, because sometimes
        //   we would like to have an Index property (for ordering of collection items)
        //   on the association table which is not possible with EF.
        //   See: https://stackoverflow.com/questions/7050404/create-code-first-many-to-many-with-additional-fields-in-association-table

        private class IndependentCollectionOperationContext<TEntity, TCollectionEntity>
            where TEntity : class, IIdGetter
            where TCollectionEntity : class, IIdGetter
        {
            public Guid[] ItemIds;
            public TEntity Owner;
            public DbSet<TEntity> OwnerSet;
            public DbSet<TCollectionEntity> ItemSet;
            public ICollection<TCollectionEntity> ExistingItems;
            public bool IsChanged;
            public System.Data.Entity.Core.Objects.ObjectStateManager StateManager;
            public string CollectionPropName;
            public Action<ODataControllerBase, TEntity, TCollectionEntity> ValidateItem;
            public ODataControllerBase ODataController;

            public void AddItems()
            {
                // Handle added items.
                foreach (var itemId in ItemIds)
                {
                    if (ExistingItems.Any(x => x.Id == itemId))
                        // This item is already in the collection.
                        continue;

                    IsChanged = true;

                    // Load item entity.
                    var item = ItemSet.Find(itemId);
                    if (item == null)
                        ODataController.ThrowNotFound($"Independent collection item not found (ID: {itemId}).");

                    ValidateItem?.Invoke(ODataController, Owner, item);

                    // Add item.
                    ExistingItems.Add(item);
                    StateManager.ChangeRelationshipState(Owner, item, CollectionPropName, EntityState.Added);
                }
            }

            public void RemoveItems()
            {
                foreach (var item in ExistingItems.ToArray())
                {
                    if (!ItemIds.Contains(item.Id))
                        continue;

                    IsChanged = true;

                    // Remove item.
                    ExistingItems.Remove(item);
                    StateManager.ChangeRelationshipState(Owner, item, CollectionPropName, EntityState.Deleted);
                }
            }

            /// <summary>
            /// Removes existing items if they are not included in the provided list.
            /// </summary>
            public void RemoveMissing()
            {
                // Handle removed items.
                foreach (var item in ExistingItems.ToArray())
                {
                    if (ItemIds.Contains(item.Id))
                        continue;

                    IsChanged = true;

                    // Remove item.
                    ExistingItems.Remove(item);
                    StateManager.ChangeRelationshipState(Owner, item, CollectionPropName, EntityState.Deleted);
                }
            }
        }

        static bool ModifyIndependentCollection<TEntity, TCollectionEntity>(
            this ODataControllerBase controller,
            DbContext db,
            Guid id,
            Guid[] itemIds,
            string collectionPropName,
            Action<ODataControllerBase, TEntity, TCollectionEntity> validateItem,
            Action<IndependentCollectionOperationContext<TEntity, TCollectionEntity>> operation)
            where TEntity : class, IIdGetter
            where TCollectionEntity : class, IIdGetter
        {
            if (db.Configuration.LazyLoadingEnabled)
                throw new Exception("Modifying independed collection: 'Lazy loading' must not be enabled for this operation.");
            if (db.Configuration.AutoDetectChangesEnabled)
                throw new Exception("Modifying independed collection: 'Auto detect changes' must not be enabled for this operation.");

            var context = new IndependentCollectionOperationContext<TEntity, TCollectionEntity>();

            context.ODataController = controller;
            context.ItemIds = itemIds;
            context.CollectionPropName = collectionPropName;
            context.ValidateItem = validateItem;

            context.OwnerSet = db.Set<TEntity>();
            context.ItemSet = db.Set<TCollectionEntity>();

            // Load owner with collection items.
            context.Owner = context.OwnerSet
                .Include(collectionPropName)
                .FirstOrDefault(x => x.Id == id);

            if (context.Owner == null)
                controller.ThrowNotFound($"Owner of independent collection not found (ID: {id}).");

            // KABU TODO: Would like to have an async method for this.
            context.ExistingItems = (ICollection<TCollectionEntity>)typeof(TEntity).GetProperty(collectionPropName).GetValue(context.Owner);

            context.StateManager = ((IObjectContextAdapter)db).ObjectContext.ObjectStateManager;

            operation(context);

            return context.IsChanged;
        }

        public static bool UpdateIndependentCollection<TEntity, TCollectionEntity>(
            this ODataControllerBase controller,
            DbContext db,
            ODataActionParameters parameters,
            string collectionPropName,
            Action<ODataControllerBase, TEntity, TCollectionEntity> validateItem = null)
            where TEntity : class, IIdGetter
            where TCollectionEntity : class, IIdGetter
        {
            Guid entityId = (Guid)parameters["id"];
            Guid[] collectionEntityIds = (parameters["itemIds"] as IEnumerable<Guid>).ToArray();

            return ModifyIndependentCollection<TEntity, TCollectionEntity>(controller, db, entityId, collectionEntityIds, collectionPropName, validateItem,
               (context) =>
               {
                   context.RemoveMissing();
                   context.AddItems();

               });
        }

        public static bool AddToIndependentCollection<TEntity, TCollectionEntity>(
            this ODataControllerBase controller,
            DbContext db,
            Guid entityId,
            Guid[] collectionEntityIds,
            string collectionPropName,
            Action<ODataControllerBase, TEntity, TCollectionEntity> validateItem = null)
            where TEntity : class, IIdGetter
            where TCollectionEntity : class, IIdGetter
        {
            return ModifyIndependentCollection<TEntity, TCollectionEntity>(controller, db, entityId, collectionEntityIds, collectionPropName, validateItem,
                (context) =>
                {
                    context.AddItems();
                });
        }

        public static bool RemoveFromIndependentCollection<TEntity, TCollectionEntity>(
            this ODataControllerBase controller,
            DbContext db,
            Guid entityId,
            Guid[] collectionEntityIds,
            string collectionPropName)
            where TEntity : class, IIdGetter
            where TCollectionEntity : class, IIdGetter
        {
            return ModifyIndependentCollection<TEntity, TCollectionEntity>(controller, db, entityId, collectionEntityIds, collectionPropName,
                validateItem: null,
                operation: (context) =>
                {
                    context.RemoveItems();
                });
        }
    }

    /// <summary>
    /// MVC: Ensures all requests operate in the context of a tenant.
    /// </summary>
    public class TenantScopeFilterAttribute : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuting(System.Web.Mvc.ActionExecutingContext filterContext)
        {
            TenantScopeFilterHelper.RegisterDefaultTenantIfMissing();
        }
    }

    /// <summary>
    /// Web API. Ensures all requests operate in the context of a tenant.
    /// </summary>
    public class TenantScopeApiFilterAttribute : System.Web.Http.Filters.ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            TenantScopeFilterHelper.RegisterDefaultTenantIfMissing();
        }
    }

    static class TenantScopeFilterHelper
    {
        public static void RegisterDefaultTenantIfMissing()
        {
            // Ensure we operate in the context of a tenant.
#if (true)
            var tenantGuid = ServiceLocator.Current.GetInstance<ICurrentTenantProvider>().GetTenantId(required: false);
            if (tenantGuid == null)
            {
                // KABU TODO: REVISIT: We'll use a default tenant until we need additional tenants (if ever).
#if (true)
                ServiceLocator.Current.GetInstance<ITenantManager>().SetDefaultTenant();
#else
                filterContext.Result = TenantNotSelected;
#endif
                return;
            }
#endif
        }

        // KABU TODO: Not used. Remove?
        public static System.Web.Mvc.RedirectToRouteResult TenantNotSelected()
        {
            return new System.Web.Mvc.RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary(new { action = "Index", controller = "TenantSelector" }));
        }
    }
}

