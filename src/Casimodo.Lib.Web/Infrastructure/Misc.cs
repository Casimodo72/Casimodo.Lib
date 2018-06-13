using Casimodo.Lib;
using Casimodo.Lib.Data;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.OData;

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
        public static bool UpdateIndependentCollection<TEntity, TCollectionEntity>(
            this ODataControllerBase controller,
            ODataActionParameters parameters,
            DbContext db,
            string collectionPropName,
            Action<ODataControllerBase, TEntity, TCollectionEntity> validateItem = null)         
            where TEntity : class, IIdGetter
            where TCollectionEntity : class, IIdGetter
        {
            Guid id = (Guid)parameters["id"];
            Guid[] itemIds = (parameters["itemIds"] as IEnumerable<Guid>).ToArray();         

            DbSet<TEntity> ownerSet = db.Set<TEntity>();
            DbSet<TCollectionEntity> itemSet = db.Set<TCollectionEntity>();

            // Load owner with collection items.
            TEntity owner = ownerSet
                .Include(collectionPropName)
                .FirstOrDefault(x => x.Id == id);

            if (owner == null)
                controller.ThrowNotFound($"Owner not found (ID: {id}).");

            var items = (ICollection<TCollectionEntity>)typeof(TEntity).GetProperty(collectionPropName).GetValue(owner);

            bool changed = false;

            var objectStateManager = ((IObjectContextAdapter)db).ObjectContext.ObjectStateManager;

            // Handle removed items.
            foreach (var item in items.ToArray())
            {
                if (itemIds.Contains(item.Id))
                    continue;

                // Remove tag.
                items.Remove(item);
                objectStateManager.ChangeRelationshipState(owner, item, collectionPropName, EntityState.Deleted);

                changed = true;
            }

            // Handle added items.
            foreach (var itemId in itemIds)
            {
                if (items.Any(x => x.Id == itemId))
                    // Item is already in the collection.
                    continue;

                changed = true;

                // Load item entity.
                var item = itemSet.Find(itemId);
                if (item == null)
                    controller.ThrowNotFound($"Item not found (ID: {itemId}).");

                if (validateItem != null)
                    validateItem(controller, owner, item);

                // Add tag.
                items.Add(item);
                objectStateManager.ChangeRelationshipState(owner, item, collectionPropName, EntityState.Added);
            }

            return changed;
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

        public static System.Web.Mvc.RedirectToRouteResult TenantNotSelected()
        {
            return new System.Web.Mvc.RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary(new { action = "Index", controller = "TenantSelector" }));
        }
    }
}

