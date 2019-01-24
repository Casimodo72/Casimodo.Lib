using Microsoft.AspNet.OData;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Web
{
    public class UnidirM2MCollectionOperationOptions<TOwner, TItem>
    {
        public DbContext Db { get; set; }
        public Guid OwnerId { get; set; }
        public Guid[] ItemIds { get; set; }
        public string PropPath { get; set; }
        public string ForeignKeyToOwner { get; set; }
        public string ForeignKeyToItem { get; set; }
        public Action<ODataControllerBase, TOwner, TItem> ValidateItem { get; set; }
    }

    public static class ODataControllerExtensions
    {
        private class Entry<TLink, TItem>
        {
            public TLink Link { get; set; }
            public TItem Item { get; set; }
        }



        private class UnidirM2MCollectionOperationContext<TOwner, TLink, TItem>
            where TOwner : class, IIdGetter
            where TLink : class, IIdGetter, IGuidGenerateable, new()
            where TItem : class, IIdGetter
        {
            public Guid[] ItemIds;
            public TOwner Owner;
            public Guid OwnerId;
            public DbSet<TOwner> OwnerSet;
            public DbSet<TItem> ItemDbSet;
            public ICollection<TLink> LinkDbCollection;
            public List<Entry<TLink, TItem>> Entries = new List<Entry<TLink, TItem>>();
            public bool IsChanged;
            //public System.Data.Entity.Core.Objects.ObjectStateManager StateManager;
            public string PropPath;
            public string LinksPropName;
            public string ItemPropName;
            public string ForeignKeyToItemPropName;
            public string ForeignKeyToOwnerPropName;
            public Action<ODataControllerBase, TOwner, TItem> ValidateItem;
            public ODataControllerBase ODataController;

            public void AddItems()
            {
                // Handle added items.
                foreach (var itemId in ItemIds)
                {
                    if (Entries.Any(x => x.Item.Id == itemId))
                        // This item is already in the collection.
                        continue;

                    IsChanged = true;

                    // Load collection item entity.
                    var item = ItemDbSet.Find(itemId);
                    if (item == null)
                        ODataController.ThrowNotFound($"Collection item not found (type: {typeof(TItem).Name}, ID: {itemId}).");

                    ValidateItem?.Invoke(ODataController, Owner, item);

                    // Add link between owner and collection item.
                    var link = new TLink();
                    link.GenerateGuid();
                    // Set foreign key on link to owner entitiy.
                    typeof(TLink).GetProperty(ForeignKeyToOwnerPropName).SetValue(link, OwnerId);
                    // Set foreign key on link to collection item entity.
                    typeof(TLink).GetProperty(ForeignKeyToItemPropName).SetValue(link, itemId);
                    // NOTE: Effectively we are adding to a DbSet here.
                    LinkDbCollection.Add(link);
                    // Also add to entries in order to ignore duplicates.
                    Entries.Add(new Entry<TLink, TItem>
                    {
                        Link = link,
                        Item = item
                    });

                    // TODO: REMOVE: StateManager.ChangeRelationshipState(Owner, item, CollectionPropName, EntityState.Added);
                }
            }

            public void RemoveItems()
            {
                // Removes items if they have IDs equal to those in ItemIds.
                foreach (var entry in Entries.ToArray())
                {
                    if (!ItemIds.Contains(entry.Item.Id))
                        continue;

                    IsChanged = true;

                    // Remove link. NOTE: Effectively we are removing from a DbSet here.
                    LinkDbCollection.Remove(entry.Link);
                    Entries.Remove(entry);
                    // TODO: REMOVE: StateManager.ChangeRelationshipState(Owner, item, CollectionPropName, EntityState.Deleted);
                }
            }

            /// <summary>
            /// Removes existing items if they are not included in the provided list.
            /// </summary>
            public void RemoveMissing()
            {
                // Handle removed items.
                foreach (var entry in Entries.ToArray())
                {
                    if (ItemIds.Contains(entry.Item.Id))
                        continue;

                    IsChanged = true;

                    // Remove link. NOTE: Effectively we are removing from a DbSet here.
                    LinkDbCollection.Remove(entry.Link);
                    Entries.Remove(entry);
                    // TODO: REMOVE: StateManager.ChangeRelationshipState(Owner, item, CollectionPropName, EntityState.Deleted);
                }
            }
        }

        static bool ModifyUnidirM2MCollection<TOwner, TLink, TItem>(
            this ODataControllerBase controller,
            UnidirM2MCollectionOperationOptions<TOwner, TItem> options,
            Action<UnidirM2MCollectionOperationContext<TOwner, TLink, TItem>> operation)
            where TOwner : class, IIdGetter
            where TLink : class, IIdGetter, IGuidGenerateable, new()
            where TItem : class, IIdGetter
        {
            if (options.Db.ChangeTracker.LazyLoadingEnabled)
                throw new Exception("Modifying collection: 'Lazy loading' must not be enabled for this operation.");
            if (options.Db.ChangeTracker.AutoDetectChangesEnabled)
                throw new Exception("Modifying collection: 'Auto detect changes' must not be enabled for this operation.");

            var context = new UnidirM2MCollectionOperationContext<TOwner, TLink, TItem>();

            context.ODataController = controller;
            context.ItemIds = options.ItemIds;

            // Property names.
            context.PropPath = options.PropPath;
            var propPathSteps = context.PropPath.Split('.');
            context.LinksPropName = propPathSteps[0];
            context.ItemPropName = propPathSteps[1];
            context.ForeignKeyToOwnerPropName = options.ForeignKeyToOwner;
            context.ForeignKeyToItemPropName = options.ForeignKeyToItem;

            context.ValidateItem = options.ValidateItem;

            context.OwnerSet = options.Db.Set<TOwner>();
            context.ItemDbSet = options.Db.Set<TItem>();

            // Load owner with collection items.
            context.OwnerId = options.OwnerId;
            // TODO: Use async?
            context.Owner = context.OwnerSet
                // TODO: Check if the include path works with EF Core.
                .Include(context.PropPath)
                .FirstOrDefault(x => x.Id == context.OwnerId);

            if (context.Owner == null)
                controller.ThrowNotFound($"Owner of unidirectional many to many collection not found (ID: {context.OwnerId}).");

            context.LinkDbCollection = (ICollection<TLink>)typeof(TOwner).GetProperty(context.LinksPropName).GetValue(context.Owner);

            foreach (var link in context.LinkDbCollection)
            {
                context.Entries.Add(new Entry<TLink, TItem>
                {
                    Link = link,
                    Item = (TItem)typeof(TLink).GetProperty(context.ItemPropName).GetValue(link)
                });
            }

            // TODO: REMOVE: context.StateManager = ((IObjectContextAdapter)db).ObjectContext.ObjectStateManager;

            operation(context);

            return context.IsChanged;
        }

        public static bool UpdateUnidirM2MCollection<TOwner, TLink, TItem>(
            this ODataControllerBase controller,
            ODataActionParameters parameters,
            UnidirM2MCollectionOperationOptions<TOwner, TItem> options)
            where TOwner : class, IIdGetter
            where TLink : class, IIdGetter, IGuidGenerateable, new()
            where TItem : class, IIdGetter
        {
            options.OwnerId = (Guid)parameters["id"];
            options.ItemIds = (parameters["itemIds"] as IEnumerable<Guid>).ToArray();

            return ModifyUnidirM2MCollection<TOwner, TLink, TItem>(controller, options,
               (context) =>
               {
                   context.RemoveMissing();
                   context.AddItems();
               });
        }

        public static bool AddToUnidirM2MCollection<TOwner, TLink, TItem>(
            this ODataControllerBase controller,
            UnidirM2MCollectionOperationOptions<TOwner, TItem> options)
            where TOwner : class, IIdGetter
            where TLink : class, IIdGetter, IGuidGenerateable, new()
            where TItem : class, IIdGetter
        {
            return ModifyUnidirM2MCollection<TOwner, TLink, TItem>(controller, options,
                (context) =>
                {
                    context.AddItems();
                });
        }

        public static bool RemoveFromUnidirM2MCollection<TOwner, TLink, TItem>(
            this ODataControllerBase controller,
            UnidirM2MCollectionOperationOptions<TOwner, TItem> options)
            where TOwner : class, IIdGetter
            where TLink : class, IIdGetter, IGuidGenerateable, new()
            where TItem : class, IIdGetter
        {
            return ModifyUnidirM2MCollection<TOwner, TLink, TItem>(controller, options,
                operation: (context) =>
                {
                    context.RemoveItems();
                });
        }

        // TODO: REVISIT: Independent lists are currently disabled because EF Core does
        // not support those. Use unidirectional many to many lists instead.
#if (false)
        // KABU TODO: Think about using two one-to-many associations instead
        //   of those black-box many-to-many associations, because sometimes
        //   we would like to have an Index property (for ordering of collection items)
        //   on the association table which is not possible with EF.
        //   See: https://stackoverflow.com/questions/7050404/create-code-first-many-to-many-with-additional-fields-in-association-table

        private class IndependentCollectionOperationContext<TOwner, TItem>
            where TOwner : class, IIdGetter
            where TItem : class, IIdGetter
        {
            public Guid[] ItemIds;
            public TOwner Owner;
            public DbSet<TOwner> OwnerSet;
            public DbSet<TItem> ItemSet;
            public ICollection<TItem> ExistingItems;
            public bool IsChanged;
            public System.Data.Entity.Core.Objects.ObjectStateManager StateManager;
            public string CollectionPropName;
            public Action<ODataControllerBase, TOwner, TItem> ValidateItem;
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

        static bool ModifyIndependentCollection<TOwner, TItem>(
            this ODataControllerBase controller,
            DbContext db,
            Guid id,
            Guid[] itemIds,
            string collectionPropName,
            Action<ODataControllerBase, TOwner, TItem> validateItem,
            Action<IndependentCollectionOperationContext<TOwner, TItem>> operation)
            where TOwner : class, IIdGetter
            where TItem : class, IIdGetter
        {
            if (db.Configuration.LazyLoadingEnabled)
                throw new Exception("Modifying independed collection: 'Lazy loading' must not be enabled for this operation.");
            if (db.Configuration.AutoDetectChangesEnabled)
                throw new Exception("Modifying independed collection: 'Auto detect changes' must not be enabled for this operation.");

            var context = new IndependentCollectionOperationContext<TOwner, TItem>();

            context.ODataController = controller;
            context.ItemIds = itemIds;
            context.CollectionPropName = collectionPropName;
            context.ValidateItem = validateItem;

            context.OwnerSet = db.Set<TOwner>();
            context.ItemSet = db.Set<TItem>();

            // Load owner with collection items.
            context.Owner = context.OwnerSet
                .Include(collectionPropName)
                .FirstOrDefault(x => x.Id == id);

            if (context.Owner == null)
                controller.ThrowNotFound($"Owner of independent collection not found (ID: {id}).");

            // KABU TODO: Would like to have an async method for this.
            context.ExistingItems = (ICollection<TItem>)typeof(TOwner).GetProperty(collectionPropName).GetValue(context.Owner);

            context.StateManager = ((IObjectContextAdapter)db).ObjectContext.ObjectStateManager;

            operation(context);

            return context.IsChanged;
        }

        public static bool UpdateIndependentCollection<TOwner, TItem>(
            this ODataControllerBase controller,
            DbContext db,
            ODataActionParameters parameters,
            string collectionPropName,
            Action<ODataControllerBase, TOwner, TItem> validateItem = null)
            where TOwner : class, IIdGetter
            where TItem : class, IIdGetter
        {
            Guid entityId = (Guid)parameters["id"];
            Guid[] collectionEntityIds = (parameters["itemIds"] as IEnumerable<Guid>).ToArray();

            return ModifyIndependentCollection<TOwner, TItem>(controller, db, entityId, collectionEntityIds, collectionPropName, validateItem,
               (context) =>
               {
                   context.RemoveMissing();
                   context.AddItems();

               });
        }

        public static bool AddToIndependentCollection<TOwner, TItem>(
            this ODataControllerBase controller,
            DbContext db,
            Guid entityId,
            Guid[] collectionEntityIds,
            string collectionPropName,
            Action<ODataControllerBase, TOwner, TItem> validateItem = null)
            where TOwner : class, IIdGetter
            where TItem : class, IIdGetter
        {
            return ModifyIndependentCollection<TOwner, TItem>(controller, db, entityId, collectionEntityIds, collectionPropName, validateItem,
                (context) =>
                {
                    context.AddItems();
                });
        }

        public static bool RemoveFromIndependentCollection<TOwner, TItem>(
            this ODataControllerBase controller,
            DbContext db,
            Guid entityId,
            Guid[] collectionEntityIds,
            string collectionPropName)
            where TOwner : class, IIdGetter
            where TItem : class, IIdGetter
        {
            return ModifyIndependentCollection<TOwner, TItem>(controller, db, entityId, collectionEntityIds, collectionPropName,
                validateItem: null,
                operation: (context) =>
                {
                    context.RemoveItems();
                });
        }
#endif
    }
}
