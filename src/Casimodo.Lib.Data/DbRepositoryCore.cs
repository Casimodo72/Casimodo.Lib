﻿using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Casimodo.Lib.Data
{
    public class DbRepositoryCoreProvider
    {
        public DbRepositoryCoreProvider()
        {
            Items = new Dictionary<Type, DbRepositoryCore>();
        }

        Dictionary<Type, DbRepositoryCore> Items { get; set; }

        public void Add<TContext>(DbRepositoryCore core)
            where TContext : DbContext
        {
            Items.Add(typeof(TContext), core);
        }

        public DbRepositoryCore GetCoreFor<TContext>()
            where TContext : DbContext
        {
            return Items[typeof(TContext)];
        }
    }

    public class DbRepoContainer
    {
        protected readonly DbContext _db;

        public DbRepoContainer(DbContext db)
        {
            _db = db;
        }
    }

    [Flags]
    public enum DbRepoOp
    {
        None = 0,
        Add = 1 << 0,
        Update = 1 << 1,
        Delete = 1 << 2,
        MoveToRecycleBin = 1 << 3,
        RestoreSelfDeleted = 1 << 4,
        UpdateMoveToRecycleBin = Update | MoveToRecycleBin
    }

    public abstract class DbRepoOperationContext
    {
        public abstract DbContext GetDb();
        public abstract DbRepoContainer GetRepos();

        public DbRepoOp OriginOperation { get; set; } = DbRepoOp.None;
        public DbRepoOp Operation { get; set; } = DbRepoOp.None;

        public MojDataGraphMask UpdateMask { get; set; }

        public DateTimeOffset Time { get; set; }
        public object Origin { get; set; }

        public object Item { get; set; }

        public OperationOriginInfo OriginInfo
        {
            get
            {
                if (_originInfo == null)
                {
                    var id = HProp.GetProp<Guid?>(Origin, "Id", null);
                    if (id == null)
                        throw new DbRepositoryException($"The type '{Origin.GetType().Name}' has no ID property.");

                    _originInfo = new OperationOriginInfo
                    {
                        TypeId = Origin.GetType().FindAttr<TypeDescriptorAttribute>().Guid,
                        Id = id.Value
                    };
                }

                return _originInfo;
            }
        }
        OperationOriginInfo _originInfo;

        public void Validate(DbRepoOp op)
        {
            if (!Operation.HasFlag(op))
                throw new DbRepositoryException($"Expected was a repository operation context with an operation of '{op}'.");
        }

        public DbRepoOperationContext SubUpdate(object item, MojDataGraphMask mask = null)
        {
            return CreateSubContext(item, DbRepoOp.Update, mask);
        }

        public DbRepoOperationContext SubAdd(object item, MojDataGraphMask mask = null)
        {
            return CreateSubContext(item, DbRepoOp.Add, mask);
        }

        public DbRepoOperationContext SubDelete(object item = null)
        {
            return CreateSubContext(item, DbRepoOp.Delete);
        }

        public DbRepoOperationContext SubRestoreCascadeDeleted(object item)
        {
            return CreateSubContext(item);
        }

        public DbRepoOperationContext CreateSubContext(object item, DbRepoOp? op = null, MojDataGraphMask mask = null)
        {
            var sub = (DbRepoOperationContext)this.MemberwiseClone();

            sub.Item = item;
            sub.UpdateMask = mask;
            if (op != null)
                sub.Operation = op.Value;

            return sub;
        }
    }

    public abstract class DbRepoOperationContext<TDb, TRepoAggregate> : DbRepoOperationContext
        where TDb : DbContext
        where TRepoAggregate : DbRepoContainer
    {
        public TDb Db { get; set; }
        public TRepoAggregate Repos { get; set; }

        public sealed override DbContext GetDb()
        {
            return Db;
        }

        public sealed override DbRepoContainer GetRepos()
        {
            return Repos;
        }
    }

    public class OperationOriginInfo
    {
        public Guid Id { get; set; }
        public Guid TypeId { get; set; }
    }

    public class DbRepoCurrentUserInfo
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
    }

    public abstract class DbRepositoryCore
    {
        public DateTimeOffset GetTime()
        {
            return DateTimeOffset.UtcNow;
        }

        public virtual object Create<TEntity>(DbContext db)
            where TEntity : class
        {
            Guard.ArgNotNull(db, nameof(db));

            // KABU TODO: Evaluate whether we really want to create entity proxies.
            return db.Set<TEntity>().Create();
        }

        public abstract DbRepoOperationContext CreateOperationContext(object item, DbRepoOp op, DbContext db, MojDataGraphMask mask = null);

        public virtual DbRepoCurrentUserInfo GetCurrentUserInfo()
        {
            throw new NotImplementedException();
        }

        public object UpdateUsingMask(DbRepoOperationContext ctx)
        {
            Guard.ArgNotNull(ctx, nameof(ctx));

            var db = ctx.GetDb();
            var source = ctx.Item;
            var mask = ctx.UpdateMask;
            if (mask == null) throw new ArgumentException("The mask must be assigned.", nameof(ctx));

            var type = source.GetType();

            var key = ((IKeyAccessor)source).GetKeyObject();
            if (key == null)
                throw new DbRepositoryException($"Update error: Item has no key assigned (type: '{type.Name}').");

            var entitySet = db.Set(type);

            var target = entitySet.Find(key);
            if (target == null)
                throw new DbRepositoryException($"Update error: Previous item not found (type: '{type.Name}', ID: '{key}').");

            bool same = target == source;

            var entry = db.Entry(target);

            PropertyInfo prop;
            object newValue;

            // Leaf properties
            foreach (var propName in mask.Properties)
            {
                if (same)
                {
                    entry.Property(propName).IsModified = true;
                }
                else
                {
                    prop = type.GetProperty(propName);
                    newValue = prop.GetValue(source);
                    // Mark as modified and assign if changed.
                    if (!object.Equals(prop.GetValue(target), newValue))
                    {
                        prop.SetValue(target, newValue);
                        entry.Property(propName).IsModified = true;
                    }
                }
            }

            // Reference properties to entity or complex type.
            foreach (var referenceProp in mask.References)
            {
                // KABU TODO: How to differentiate references to entities from references to complex types?

                if (referenceProp.Cardinality.HasFlag(MojCardinality.Many))
                    throw new DbRepositoryException("Update error: Nested references with " +
                        $"cardinality 'Many' are not supported yet (property: '{referenceProp.Name}').");

                if (referenceProp.Binding.HasFlag(MojReferenceBinding.Loose))
                {
                    prop = type.GetProperty(referenceProp.ForeignKey);
                    newValue = prop.GetValue(source);

                    // Loose navigation properties: Update the foreign key value only.
                    // Mark as modified and assign if changed.
                    if (!object.Equals(prop.GetValue(target), newValue))
                    {
                        prop.SetValue(target, newValue);
                        entry.Property(referenceProp.ForeignKey).IsModified = true;
                    }

                    continue;
                }

                // Nested reference

                prop = type.GetProperty(referenceProp.Name);
                newValue = prop.GetValue(source);

                var foreignKeyProp = type.GetProperty(referenceProp.ForeignKey);
                var oldValue = foreignKeyProp.GetValue(target);

                if (newValue == null)
                {
                    // NULL values are only acceptable if the value was also NULL beforehand.
                    if (oldValue != null)
                        throw new DbRepositoryException("Update error: The nested reference " +
                            $"property '{referenceProp.Name}' must not be set to NULL.");
                }
                else if (newValue != null)
                {
                    if (oldValue == null)
                    {
                        // The referenced entity was not added yet.

                        newValue = db.Set(prop.PropertyType).Add(newValue);
                        ApplyTenantKey(newValue);
                        SetIsNested(newValue);
                        OnAdded(ctx.CreateSubContext(newValue, op: DbRepoOp.Add));

                        // Set the reference entitity.
                        prop.SetValue(target, newValue);

                        // Set the foreign key to the referenced entity.
                        foreignKeyProp.SetValue(target, ((IKeyAccessor)newValue).GetKeyObject());
                        entry.Property(referenceProp.ForeignKey).IsModified = true;
                    }
                    else
                    {
                        var newForeignKey = foreignKeyProp.GetValue(source);
                        if (!object.Equals(oldValue, newForeignKey))
                            throw new DbRepositoryException("Update error: Nested reference " +
                                $"property '{referenceProp.Name}': The referenced entity must not be changed once the reference is established.");

                        // Process the nested object.
                        newValue = UpdateUsingMask(ctx.CreateSubContext(newValue, op: DbRepoOp.Update, mask: referenceProp.To));

                        // Assign to target object.
                        // NOTE: Do *not* mark nested navigation properties as modified.
                        prop.SetValue(target, newValue);
                    }
                }
            }

            ctx = ctx.CreateSubContext(target, mask: mask);

            OnUpdated(ctx);

            return target;
        }

        protected void ApplyTenantKey(object entity)
        {
            var multitenant = entity as IMultitenant;
            if (multitenant == null)
                return;

            var tenantId = ServiceLocator.Current.GetInstance<ICurrentTenantProvider>().GetTenantId(required: false);
            if (tenantId == null)
                throw new InvalidOperationException("The CurrentTenantGuid is not assigned.");

            multitenant.SetTenantKey(tenantId.Value);
        }

        protected void SetIsNested(object entity)
        {
            SetProp(entity, CommonDataNames.IsNested, true);
        }

        public virtual void OnLoaded(object item)
        {
            // NOP
        }

        public virtual void OnLoaded(object item, DbContext db)
        {
            OnLoaded(item);
        }

        public virtual void OnAdded(DbRepoOperationContext ctx)
        {
            OnSaving(ctx);
        }

        public virtual void OnUpdated(DbRepoOperationContext ctx)
        {
            OnSaving(ctx);
        }

        public virtual void OnSaving(DbRepoOperationContext ctx)
        {
            // NOP
        }

        public virtual void OnDeleting(DbRepoOperationContext ctx)
        {
            // NOP
        }

        public virtual void RestoreSelfDeleted(DbRepoOperationContext ctx)
        {
            // NOP
        }

        public virtual void RestoreCascadeDeleted(DbRepoOperationContext ctx)
        {
            // NOP
        }

        public virtual void SetAdded(object item, DateTimeOffset? now)
        {
            SetAddedCore(item, now, null, null);
        }

        public void SetAddedCore(object item, DateTimeOffset? now, Guid? userId, string userName)
        {
            Guard.ArgNotNull(item, nameof(item));

            now = now ?? GetTime();

            if (HasProp(item, CommonDataNames.CreatedOn))
            {
                SetProp(item, CommonDataNames.CreatedOn, now);
                SetProp(item, CommonDataNames.CreatedBy, userName);
                SetProp(item, CommonDataNames.CreatedByUserId, userId);
            }

            if (HasProp(item, CommonDataNames.ModifiedOn))
            {
                SetProp(item, CommonDataNames.ModifiedOn, now);
                SetProp(item, CommonDataNames.ModifiedBy, userName);
                SetProp(item, CommonDataNames.ModifiedByUserId, userId);
            }
        }

        public virtual void SetModified(object item, DateTimeOffset? now)
        {
            SetModifiedCore(item, now, null, null);
        }

        public void SetModifiedCore(object item, DateTimeOffset? now, Guid? userId, string userName)
        {
            Guard.ArgNotNull(item, nameof(item));

            if (!HasProp(item, CommonDataNames.ModifiedOn))
                return;

            now = now ?? GetTime();

            SetProp(item, CommonDataNames.ModifiedOn, now);
            SetProp(item, CommonDataNames.ModifiedBy, userName);
            SetProp(item, CommonDataNames.ModifiedByUserId, userId);
        }

        public virtual void SetDeleted(object item, DateTimeOffset? now)
        {
            SetDeletedCore(item, now, null, null);
        }

        public void SetDeletedCore(object item, DateTimeOffset? now, Guid? userId, string userName)
        {
            Guard.ArgNotNull(item, nameof(item));

            if (!HasProp(item, CommonDataNames.IsDeleted))
                return;

            now = now ?? GetTime();

            SetProp(item, CommonDataNames.IsDeleted, true);
            if (GetProp<DateTimeOffset?>(item, CommonDataNames.DeletedOn) == null)
            {
                SetProp(item, CommonDataNames.DeletedOn, now);
                SetProp(item, CommonDataNames.DeletedBy, userName);
                SetProp(item, CommonDataNames.DeletedByUserId, userId);
            }
            //SetProp(item, CommonDataNames.DeletedByDeviceId, ?);

            if (GetProp(item, CommonDataNames.IsSelfDeleted, false) == true &&
                GetProp<DateTimeOffset?>(item, CommonDataNames.SelfDeletedOn) == null)
            {
                SetProp(item, CommonDataNames.SelfDeletedOn, now);
                SetProp(item, CommonDataNames.SelfDeletedBy, userName);
                SetProp(item, CommonDataNames.SelfDeletedByUserId, userId);
                //SetProp(item, CommonDataNames.SelfDeletedByDeviceId, ?);
            }

            if (GetProp(item, CommonDataNames.IsRecyclableDeleted, false) == true &&
                GetProp<DateTimeOffset?>(item, CommonDataNames.RecyclableDeletedOn) == null)
            {
                SetProp(item, CommonDataNames.RecyclableDeletedOn, now);
                SetProp(item, CommonDataNames.RecyclableDeletedBy, userName);
                SetProp(item, CommonDataNames.RecyclableDeletedByUserId, userId);
                //SetProp(item, CommonDataNames.RecyclableDeletedByDeviceId, ?);
            }
        }

        protected void CompleteDeleteInfo(object item, DateTimeOffset? now)
        {
            if (GetProp<DateTimeOffset?>(item, CommonDataNames.DeletedOn, null) == null &&
                (GetProp(item, CommonDataNames.IsDeleted, defaultValue: false) ||
                 GetProp(item, CommonDataNames.IsSelfDeleted, defaultValue: false) ||
                 GetProp(item, CommonDataNames.IsCascadeDeleted, defaultValue: false) ||
                 GetProp(item, CommonDataNames.IsRecyclableDeleted, defaultValue: false)))
            {
                SetDeleted(item, now ?? GetTime());
            }
        }

        protected bool IsCascadeDeletedByOrigin(object item, DbRepoOperationContext ctx)
        {
            if (GetProp(item, CommonDataNames.IsCascadeDeleted, false) == false)
                return false;

            if (GetProp<Guid?>(item, CommonDataNames.CascadeDeletedByOriginId) != ctx.OriginInfo.Id)
                return false;

            return true;
        }

        /// <summary>
        /// Returns true if the item was changed.
        /// </summary>        
        protected bool ProcessCascadeItem(object item, DbRepoOperationContext ctx)
        {
            if (ctx.OriginOperation == DbRepoOp.None)
                throw new DbRepositoryException("Cascade error: The repository operation was not specified.");

            if (item == null)
                return false;

            if (ctx.Origin == item)
                return false;
            // KABU TODO: REMOVE
            //throw new DbRepositoryException("Cascade error: The origin object is equal to the current object.");

            if (GetProp(item, CommonDataNames.IsDeleted, false) == true)
                return false;

            if (ctx.OriginOperation == DbRepoOp.UpdateMoveToRecycleBin)
            {
                return InheritRecyclableDeleted(item, ctx);
            }
            else if (ctx.OriginOperation == DbRepoOp.Update)
            {
                return SetCascadeDeleted(item, ctx);
            }
            else throw new DbRepositoryException($"Cascade error: Unexpected repository operation '{ctx.OriginOperation}'.");
        }

        protected bool SetCascadeDeleted(object item, DbRepoOperationContext ctx)
        {
            var setters = new Func<bool>[]
            {
                () => SetChangedProp(item, CommonDataNames.IsCascadeDeleted, true),

                () => SetChangedProp<DateTimeOffset?>(item, CommonDataNames.CascadeDeletedOn, GetProp<DateTimeOffset?>(ctx.Origin, CommonDataNames.DeletedOn, ctx.Time)),

                () => SetChangedProp<Guid?>(item, CommonDataNames.CascadeDeletedByOriginTypeId, ctx.OriginInfo.TypeId),
                () => SetChangedProp<Guid?>(item, CommonDataNames.CascadeDeletedByOriginId, ctx.OriginInfo.Id),

                () => SetChangedProp(item, CommonDataNames.CascadeDeletedBy, GetProp<string>(ctx.Origin, CommonDataNames.DeletedBy, null)),
                () => SetChangedProp<Guid?>(item, CommonDataNames.CascadeDeletedByUserId, GetProp<Guid?>(ctx.Origin, CommonDataNames.DeletedByUserId, null)),
                () => SetChangedProp<Guid?>(item, CommonDataNames.CascadeDeletedByDeviceId, GetProp<Guid?>(ctx.Origin, CommonDataNames.DeletedByDeviceId, null)),
            };

            bool changed = false;
            foreach (var setter in setters)
                changed = setter() || changed;

            return changed;
        }

        protected bool InheritRecyclableDeleted(object item, DbRepoOperationContext ctx)
        {
            if (ctx.Origin == item)
                return false;

            if (!GetProp(ctx.Origin, CommonDataNames.IsRecyclableDeleted, false))
                return false;

            var setters = new Func<bool>[]
            {
                () => SetChangedProp<bool>(item, CommonDataNames.IsRecyclableDeleted, true),
                () => MapChangedProp<DateTimeOffset?>(ctx.Origin, item, CommonDataNames.RecyclableDeletedOn, ctx.Time),
                () => MapChangedProp<string>(ctx.Origin, item, CommonDataNames.RecyclableDeletedBy),
                () => MapChangedProp<Guid?>(ctx.Origin, item, CommonDataNames.RecyclableDeletedByUserId),
                () => MapChangedProp<Guid?>(ctx.Origin, item, CommonDataNames.RecyclableDeletedByDeviceId)
            };

            bool changed = false;
            foreach (var setter in setters)
                changed = setter() || changed;

            return changed;
        }

        protected void ClearDeleted(object item, DbRepoOperationContext ctx)
        {
            SetProp(item, CommonDataNames.IsDeleted, false);
            SetProp(item, CommonDataNames.DeletedOn, null);
            SetProp(item, CommonDataNames.DeletedBy, null);
            SetProp(item, CommonDataNames.DeletedByUserId, null);
            SetProp(item, CommonDataNames.DeletedByDeviceId, null);

            SetProp(item, CommonDataNames.IsSelfDeleted, false);
            SetProp(item, CommonDataNames.SelfDeletedOn, null);
            SetProp(item, CommonDataNames.SelfDeletedBy, null);
            SetProp(item, CommonDataNames.SelfDeletedByUserId, null);
            SetProp(item, CommonDataNames.SelfDeletedByDeviceId, null);

            SetProp(item, CommonDataNames.IsCascadeDeleted, false);
            SetProp(item, CommonDataNames.CascadeDeletedOn, null);
            SetProp(item, CommonDataNames.CascadeDeletedBy, null);
            SetProp(item, CommonDataNames.CascadeDeletedByUserId, null);
            SetProp(item, CommonDataNames.CascadeDeletedByDeviceId, null);
            SetProp(item, CommonDataNames.CascadeDeletedByOriginTypeId, null);
            SetProp(item, CommonDataNames.CascadeDeletedByOriginId, null);

            SetProp(item, CommonDataNames.IsRecyclableDeleted, false);
            SetProp(item, CommonDataNames.RecyclableDeletedOn, null);
            SetProp(item, CommonDataNames.RecyclableDeletedBy, null);
            SetProp(item, CommonDataNames.RecyclableDeletedByUserId, null);
            SetProp(item, CommonDataNames.RecyclableDeletedByDeviceId, null);
        }

        /// <summary>
        /// Updates the set of DB entities specified by the given @predicate
        /// using the specified collection of entities.
        /// Newly added entities in the collection are added to the DB.
        /// Existing entities in the collection and the DB are updated and saved to DB.
        /// NOTE: Removed entities are deleted physically from the DB.
        /// </summary>        
        public void UpdateNestedCollection<T, TKey>(
            IEnumerable<T> collection,
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TKey>> keySelector,
            IDbRepository db,
            DbRepoOperationContext ctx)
            where T : class
        {
            var keySel = keySelector.Compile();
            var items = new List<T>(collection);
            var entitySet = db.Context.Set<T>();

            // Get keys of existing entities from DB.
            var existingIds = entitySet.Where(predicate).Select(keySelector).Cast<TKey>();

            foreach (var id in existingIds)
            {
                var update = items.FirstOrDefault(x => keySel(x).Equals(id));
                if (update != null)
                {
                    // Modified. This entity exists in the collection and in the DB.
                    // Update the modified nested entity and save to DB.

                    // Check for local duplicate.
                    var local = entitySet.Local.FirstOrDefault(x => keySel(x).Equals(id));
                    if (local != update)
                        throw new DbRepositoryException("An other instance of this entity already exists in the DbContext.");

                    db.UpdateEntity(ctx.CreateSubContext(update, op: DbRepoOp.Update));

                    items.Remove(update);
                }
                else
                {
                    // This DB entity does not exist anymore in the collection.
                    // Delete **physically** from DB.
                    db.DeleteEntityByKey(id, ctx.SubDelete());
                }
            }

            foreach (var item in items)
            {
                // This entity was added to the collection.
                // I.e. it did not exist before in the DB.
                // Add to DB.
                db.AddEntity(ctx.CreateSubContext(item, op: DbRepoOp.Add));
            }
        }

        // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public bool HasProp(object item, string name)
        {
            return HProp.HasProp(item, name);
        }

        public void SetProp(object item, string name, object value)
        {
            HProp.SetProp(item, name, value);
        }

        public bool SetChangedProp<T>(object item, string name, T value)
        {
            return HProp.SetChangedProp<T>(item, name, value);
        }

        public void MapProp<T>(object source, object target, string name, T defaultValue = default(T))
        {
            HProp.MapProp<T>(source, target, name, defaultValue);
        }

        public bool MapChangedProp<T>(object source, object target, string name, T defaultValue = default(T))
        {
            return HProp.MapChangedProp<T>(source, target, name, defaultValue);
        }

        public T GetProp<T>(object item, string name, T defaultValue = default(T))
        {
            return HProp.GetProp(item, name, defaultValue);
        }

        // Error helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        // KABU TODO: LOCALIZE

        public void ThrowUniquePropValueExists<T>(string prop, object value)
        {
            var display = HProp.Display(typeof(T), prop).Text;
            throw new DbRepositoryException($"Der Wert '{value}' für '{display}' ist bereits vergeben.");
        }

        public void ThrowUniquePropValueExistsCustom<T>(string prop, object value, string errorMessage)
        {
            var display = HProp.Display(typeof(T), prop).Text;
            throw new DbRepositoryException(string.Format(errorMessage, value, display));
        }

        public void ThrowUniquePropValueMustNotBeNull<T>(string prop)
        {
            var display = HProp.Display(typeof(T), prop).Text;
            throw new DbRepositoryException($"Ein Wert für '{display}' wird benötigt.");
        }

        public void ThrowUniquePropValueMustNotBeLessThan<T>(string prop, object value)
        {
            var display = HProp.Display(typeof(T), prop).Text;
            throw new DbRepositoryException($"Der Wert für '{display}' darf nicht kleiner als {value} sein.");
        }

        public void ThrowUniquePropValueMustNotBeGreaterThan<T>(string prop, object value)
        {
            var display = HProp.Display(typeof(T), prop).Text;
            throw new DbRepositoryException($"Der Wert für '{display}' darf nicht größer als {value} sein.");
        }

        public void ThrowStaticUserIdPropMustNotBeModified<T>(string prop)
        {
            var display = HProp.Display(typeof(T), prop).Text;
            throw new DbRepositoryException($"Die User-Identifikations-Eigenschaft '{display}' darf nicht mehr verändert werden.");
        }
    }
}