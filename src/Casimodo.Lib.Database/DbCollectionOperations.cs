using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    public class UnidirM2MCollectionOperationOptions<TOwner, TItem>
    {
        public DbContext Db { get; set; }
        public Guid OwnerId { get; set; }
        public Guid[] ItemIds { get; set; }
        public string PropPath { get; set; }
        public string ForeignKeyToOwner { get; set; }
        public string ForeignKeyToItem { get; set; }
        public Action<TOwner, TItem> ValidateItem { get; set; }
        public bool IsAutoSaveEnabled { get; set; }
    }

    public static class DbCollectionOperations
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
            public DbContext Db { get; set; }
            public Guid[] ItemIds;
            public TOwner Owner;
            public Guid OwnerId;
            public DbSet<TOwner> OwnerDbSet;
            public DbSet<TItem> ItemDbSet;
            public List<Entry<TLink, TItem>> Entries = new List<Entry<TLink, TItem>>();
            public bool IsChanged;
            //public System.Data.Entity.Core.Objects.ObjectStateManager StateManager;
            public string PropPath;
            public string LinksPropName;
            public string ItemPropName;
            public string ForeignKeyToItemPropName;
            public string ForeignKeyToOwnerPropName;
            public Action<TOwner, TItem> ValidateItem;

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
                        throw new EntityNotFoundException($"Collection item not found (type: {typeof(TItem).Name}, ID: {itemId}).");

                    ValidateItem?.Invoke(Owner, item);

                    // Add link between owner and collection item.
                    var link = new TLink();
                    link.GenerateGuid();
                    // Set foreign key on link to owner entitiy.
                    typeof(TLink).GetProperty(ForeignKeyToOwnerPropName).SetValue(link, OwnerId);
                    // Set foreign key on link to collection item entity.
                    typeof(TLink).GetProperty(ForeignKeyToItemPropName).SetValue(link, itemId);
                    // Add to DB.
                    Db.Add(link);
                    // TODO: REMOVE: LinkDbCollection.Add(link);
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

                    // Remove link.
                    Db.Remove(entry.Link);
                    // TODO: REMOVE: LinkDbCollection.Remove(entry.Link);
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

                    // Remove link.
                    Db.Remove(entry.Link);
                    // TODO: REMOVE: LinkDbCollection.Remove(entry.Link);
                    Entries.Remove(entry);
                    // TODO: REMOVE: StateManager.ChangeRelationshipState(Owner, item, CollectionPropName, EntityState.Deleted);
                }
            }
        }

        static async Task<bool> ModifyUnidirM2MCollection<TOwner, TLink, TItem>(
            UnidirM2MCollectionOperationOptions<TOwner, TItem> options,
            Action<UnidirM2MCollectionOperationContext<TOwner, TLink, TItem>> operation)
            where TOwner : class, IIdGetter
            where TLink : class, IIdGetter, IGuidGenerateable, new()
            where TItem : class, IIdGetter
        {

            // Disable lazy loading.
            // TODO: We would like to just throw an exception if lazy loading is enabled,
            //   I currently do not see a way of asking the DbContext is really enabled.
            //   LazyLoadingEnabled might be set to true even if the DbContext was not configured
            //   with UseLazyLoadingProxies.
            options.Db.ChangeTracker.LazyLoadingEnabled = false;
            // TODO: If would try to restore the original value of AutoDetectChangesEnabled,
            //   would that trigger a re-detection of the already added/removed entities?
            options.Db.ChangeTracker.AutoDetectChangesEnabled = false;

            // TODO: REMOVE:
            //if (options.Db.ChangeTracker.LazyLoadingEnabled)
            //    throw new Exception("Modifying collection: 'Lazy loading' must not be enabled for this operation.");
            //if (options.Db.ChangeTracker.AutoDetectChangesEnabled)
            //    throw new Exception("Modifying collection: 'Auto detect changes' must not be enabled for this operation.");

            var context = new UnidirM2MCollectionOperationContext<TOwner, TLink, TItem>();
            context.Db = options.Db;
            context.OwnerDbSet = options.Db.Set<TOwner>();
            context.ItemDbSet = options.Db.Set<TItem>();

            context.ItemIds = options.ItemIds;

            // Property names.
            context.PropPath = options.PropPath;
            var propPathSteps = context.PropPath.Split('.');
            context.LinksPropName = propPathSteps[0];
            context.ItemPropName = propPathSteps[1];
            context.ForeignKeyToOwnerPropName = options.ForeignKeyToOwner;
            context.ForeignKeyToItemPropName = options.ForeignKeyToItem;

            context.ValidateItem = options.ValidateItem;

            context.OwnerId = options.OwnerId;
            // Load owner with collection items.
            context.Owner = await context.OwnerDbSet
                .Include(context.PropPath)
                .FirstOrDefaultAsync(x => x.Id == context.OwnerId);

            if (context.Owner == null)
                throw new EntityNotFoundException($"Owner of unidirectional many to many collection not found (ID: {context.OwnerId}).");

            foreach (var link in (ICollection<TLink>)typeof(TOwner).GetProperty(context.LinksPropName).GetValue(context.Owner))
            {
                context.Entries.Add(new Entry<TLink, TItem>
                {
                    Link = link,
                    Item = (TItem)typeof(TLink).GetProperty(context.ItemPropName).GetValue(link)
                });
            }

            operation(context);

            if (options.IsAutoSaveEnabled)
                await context.Db.SaveChangesAsync();

            return context.IsChanged;
        }

        public static async Task<bool> UpdateUnidirM2MCollection<TOwner, TLink, TItem>(
            UnidirM2MCollectionOperationOptions<TOwner, TItem> options)
            where TOwner : class, IIdGetter
            where TLink : class, IIdGetter, IGuidGenerateable, new()
            where TItem : class, IIdGetter
        {
            return await ModifyUnidirM2MCollection<TOwner, TLink, TItem>(options,
               (context) =>
               {
                   context.RemoveMissing();
                   context.AddItems();
               });
        }

        public static async Task<bool> AddToUnidirM2MCollection<TOwner, TLink, TItem>(
            UnidirM2MCollectionOperationOptions<TOwner, TItem> options)
            where TOwner : class, IIdGetter
            where TLink : class, IIdGetter, IGuidGenerateable, new()
            where TItem : class, IIdGetter
        {
            return await ModifyUnidirM2MCollection<TOwner, TLink, TItem>(options,
                (context) =>
                {
                    context.AddItems();
                });
        }

        public static async Task<bool> RemoveFromUnidirM2MCollection<TOwner, TLink, TItem>(
            UnidirM2MCollectionOperationOptions<TOwner, TItem> options)
            where TOwner : class, IIdGetter
            where TLink : class, IIdGetter, IGuidGenerateable, new()
            where TItem : class, IIdGetter
        {
            return await ModifyUnidirM2MCollection<TOwner, TLink, TItem>(options,
                operation: (context) =>
                {
                    context.RemoveItems();
                });
        }

        // TODO: REVISIT: Independent lists are currently disabled because EF Core does
        //   not support those. Use unidirectional many to many lists instead.
        // The code below was for EF independent collections in the web odata layer.
        // TODO: Remove web & odata related stuff if this needs to be revived some day.
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
