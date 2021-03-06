﻿using Casimodo.Lib.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    // EF Core: DbQuery<T> docs: https://www.learnentityframeworkcore.com/query-types

    public interface IDbRepository<TEntity> : IDbRepository
        where TEntity : new()
    {
        TEntity GetById(object key);
        TEntity Add(TEntity entity);
        TEntity Update(TEntity entity, MojDataGraphMask mask = null);
        void Delete(TEntity entity, DbRepoOperationContext ctx = null, bool? isPhysicalDeletionAuthorized = null);
        IQueryable<TEntity> LocalAndDbQuery(Expression<Func<TEntity, bool>> expression);
        IQueryable<TEntity> Query(bool includeDeleted = false, bool trackable = true);
        int SaveChanges();
        Task<int> SaveChangesAsync();
    }

    public interface IDbRepository
    {
        void Use(DbContext context);
        Guid GetGuid(object entity);
        object FindEntity(object key);
        object AddEntity(DbRepoOperationContext ctx);

        object UpdateEntity(DbRepoOperationContext ctx);
        void DeleteEntity(object entity);
        void DeleteEntityByKey(object key, DbRepoOperationContext ctx);
        void DeleteEntityByKey(object key);
        DbContext Context { get; set; }
        DbRepositoryCore Core();
    }

    public class DbTransactionContext<TDbContext> : DbTransactionContext
        where TDbContext : DbContext
    {
        public DbTransactionContext(TDbContext db, IDbContextTransaction trans)
            : base(db, trans)
        { }

        public TDbContext Context
        {
            get { return (TDbContext)_db; }
        }
    }

    public class DbTransactionContext
    {
        protected DbContext _db;

        public DbTransactionContext(DbContext db, IDbContextTransaction trans)
        {
            Guard.ArgNotNull(db, nameof(db));
            Guard.ArgNotNull(trans, nameof(trans));

            _db = db;
            Transaction = trans;
        }

        public IDbContextTransaction Transaction { get; private set; }

        public DbContext BaseContext
        {
            get { return _db; }
        }

        public void EnlistTransaction(DbContext dbcontext)
        {
            Guard.ArgNotNull(dbcontext, nameof(dbcontext));
            dbcontext.Database.UseTransaction(Transaction.GetDbTransaction());
        }
    }

    public static class DbRepositoryExtensions
    {
        // TODO: REMOVE?
        //public static TRepo Use<TRepo>(this TRepo repository, DbContext context)
        //    where TRepo : IDbRepository
        //{
        //    repository.Use(context);

        //    return repository;
        //}

        public static TRepo Use<TRepo>(this TRepo repository, IDbRepository other)
            where TRepo : IDbRepository
        {
            repository.Use(other.Context);

            return repository;
        }
    }

    // TransactionScope: https://msdn.microsoft.com/en-us/library/system.transactions.transactionscope%28v=vs.110%29.aspx  

    public class DbRepository
    { }

    public class DbRepository<TContext, TEntity, TKey> : DbRepository, IDbRepository<TEntity>, IDbRepository, IDisposable
        where TContext : DbContext, new()
        where TEntity : class, IKeyAccessor<TKey>, new()
        where TKey : struct, IComparable<TKey>
    {
        protected readonly static PropertyInfo KeyProp;
        protected readonly static PropertyInfo GuidProp;
        protected readonly static PropertyInfo TenantKeyProp;
        protected readonly static PropertyInfo IsDeletedProp;

        DbRepositoryCore _core;
        Guid? _tenantGuid;
        object _lock = new object();

        static DbRepository()
        {
            // Cache property infos.
            KeyProp = GetKeyProperty();
            GuidProp = (KeyProp.PropertyType == typeof(Guid)) ? KeyProp : FindProperty("Guid");
            TenantKeyProp = FindTenantKeyProperty();
            IsDeletedProp = FindProperty("IsDeleted");
        }

        public DbRepository()
        { }

        public DbRepository(TContext db)
        {
            Guard.ArgNotNull(db, nameof(db));

            Context = db;
        }

        public TContext Context { get; private set; }

        DbContext IDbRepository.Context
        {
            get { return Context; }
            set
            {
                Guard.ArgNotNull(value, nameof(value));
                Context = (TContext)value;
            }
        }

        void IDbRepository.Use(DbContext context)
        {
            Context = (TContext)context;
        }

        public DbRepositoryCore Core()
        {
            if (_core != null) return _core;
            lock (_lock)
            {
                if (_core != null) return _core;
                _core = ServiceLocator.Current.GetInstance<DbRepositoryCoreProvider>().GetCoreFor<TContext>();
                return _core;
            }
        }

        public void PerformTransaction(Action<DbTransactionContext<TContext>> action)
        {
            using (var trans = Context.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    action(new DbTransactionContext<TContext>(Context, trans));

                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        public async Task PerformTransactionAsync(Func<DbTransactionContext<TContext>, Task> action)
        {
            using (var trans = Context.Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    await action(new DbTransactionContext<TContext>(Context, trans));

                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        // KABU TODO: REMOVE? Tenant mechanism has changed.
        protected Guid GetTenantGuid()
        {
            if (_tenantGuid == null)
                _tenantGuid = ServiceLocator.Current.GetInstance<ICurrentTenantProvider>().GetTenantId(required: false);

            if (_tenantGuid == null)
                throw new InvalidOperationException("The CurrentTenantGuid is not assigned.");

            return _tenantGuid.Value;
        }

        public void ReferenceLoading(bool enabled)
        {
            // KABU TODO: IMPORTANT: EF Core: disabled by default.
            //Context.Configuration.LazyLoadingEnabled = enabled;
            //Context.Configuration.ProxyCreationEnabled = enabled;
        }

        // Get: Single ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        TEntity IDbRepository<TEntity>.GetById(object key)
        {
            return Get((TKey?)key, required: true);
        }

        object IDbRepository.FindEntity(object key)
        {
            return Get((TKey)key, required: false);
        }

        /// <summary>
        /// NOTE: Returns also IsDeleted entities.
        /// </summary>
        public TEntity Find(TKey? key, bool required = false)
        {
            return Get(key, required: required);
        }

        /// <summary>
        /// KABU TODO: Not tenant safe.
        /// </summary> 
        public TEntity Get(TKey? key, bool required = true)
        {
            TEntity entity = null;
            if (key != null)
                // KABU TODO: Not tenant safe.
                entity = EntitySet.Find(key);

            if (entity == null && required)
                throw NotFound();

            OnLoaded(entity);

            return entity;
        }

        /// <summary>
        /// NOTE: Not tenant safe. Returns also IsDeleted entities.
        /// </summary> 
        public async Task<TEntity> GetAsync(TKey key, bool required = true)
        {
            // KABU TODO: Not tenant safe.
            var entity = await EntitySet.FindAsync(key);
            if (entity == null && required)
                throw NotFound();

            OnLoaded(entity);

            return entity;
        }

        public async Task<TEntity> GetAsync(IQueryable<TEntity> query)
        {
            var entity = await query.FirstAsync();
            OnLoaded(entity);

            return entity;
        }

        public TEntity Get(IQueryable<TEntity> query)
        {
            var entity = query.First();
            OnLoaded(entity);

            return entity;
        }

        public IQueryable<TEntity> LocalQuery(bool includeDeleted = false)
        {
            return ApplyDefaultFilters(includeDeleted, EntitySet.Local.AsQueryable());
        }

        public IQueryable<TEntity> LocalAndDbQuery(Expression<Func<TEntity, bool>> predicate)
        {
            return LocalAndDbQuery(includeDeleted: false, predicate: predicate);
        }

        public IQueryable<TEntity> LocalAndDbQuery(bool includeDeleted, Expression<Func<TEntity, bool>> predicate)
        {
            // TODO: EF Core's Local is slow because it is a view over the state manager.
            //   See https://github.com/aspnet/EntityFrameworkCore/issues/14231
            //   Do we want to use ToObservableCollection here?
            var localQuery = EntitySet.Local.AsQueryable().Where(predicate);
            var localItems = ApplyDefaultFilters(includeDeleted, localQuery);

            if (!localItems.Any())
                return Query(includeDeleted).Where(predicate);

            var keys = localItems.Select(x => GetKey(x)).ToArray();

            // Return local items + queried items from db.
            // TODO: VERY IMPORTANT: This could return duplicates.
            //   TODO: Is this still an issue? We exclude the local items, don't we?
            return localItems.ToArray().Concat(
                Query(includeDeleted)
                    .Where(predicate)
                    // Exclude local items.
                    .Where(keys.GetContainsPredicate<TEntity, TKey>(KeyProp).Not())
                    .ToArray())
                .AsQueryable();
        }

        public IQueryable<TEntity> QuerySingle(TKey id, bool includeDeleted = false)
        {
            return FilterById(id, Query(includeDeleted: includeDeleted, trackable: false));
        }

        /// <summary>
        /// Returns an IQueryable of TEntity with the following being applied:
        /// 1) Tenant
        /// </summary>
        public IQueryable<TEntity> Query(bool includeDeleted = false, bool trackable = true)
        {
            IQueryable<TEntity> query = EntitySet;

            if (!trackable)
                query = query.AsNoTracking();

            query = ApplyDefaultFilters(includeDeleted, query);

            return query;
        }

        public IQueryable<TEntity> QueryDistinct(string on, bool includeDeleted = false, bool trackable = true)
        {
            Guard.ArgNotEmpty(on, nameof(on));

            return Query(includeDeleted: includeDeleted, trackable: trackable)
                .GroupBy(ExpressionHelper.GetGroupKey<TEntity>(on.Trim('\'')))
                .Select(g => g.FirstOrDefault());
        }

        // CRUD ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public int SaveChanges()
        {
            return Context.SaveChanges();
        }


        public async Task<int> SaveChangesAsync()
        {
            return await Context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync(TKey key, CancellationToken? cancellationToken = null)
        {
            try
            {
                if (cancellationToken != null)
                    await Context.SaveChangesAsync(cancellationToken.Value);
                else
                    await Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Exists(key))
                    throw NotFound();

                // TODO: REVISIT: Should we throw with HttpStatusCode.Conflict instead?
                throw;
            }
        }

        object IDbRepository.AddEntity(DbRepoOperationContext ctx)
        {
            return Add(ctx);
        }

        public TEntity Add(TEntity entity)
        {
            return Add(Core().CreateOperationContext(entity, DbRepoOp.Add, Context));
        }

        public TEntity Add(DbRepoOperationContext ctx)
        {
            Guard.ArgNotNull(ctx, nameof(ctx));
            ctx.Validate(DbRepoOp.Add);

            var entity = (TEntity)ctx.Item;

            ApplyTennant(entity);
            ctx.Item = entity = EntitySet.Add(entity).Entity;
            OnAdded(ctx);

            return entity;
        }

        public DbSet<TEntity> EntitySet
        {
            get { return Context.Set<TEntity>(); }
        }

        public TEntity Update(TEntity entity, MojDataGraphMask mask = null)
        {
            return Update(Core().CreateOperationContext(entity, DbRepoOp.Update, Context, mask));
        }

        public TEntity MoveToRecycleBin(TEntity entity)
        {
            return Update(Core().CreateOperationContext(entity, DbRepoOp.UpdateMoveToRecycleBin, Context));
        }

        public TEntity Update(DbRepoOperationContext ctx)
        {
            Guard.ArgNotNull(ctx, nameof(ctx));
            ctx.Validate(DbRepoOp.Update);

            var entity = (TEntity)ctx.Item;

            ApplyTennant(entity);
            if (ctx.UpdateMask != null)
            {
                return (TEntity)Core().UpdateUsingMask(ctx);
            }
            else
            {
                var localEntity = FindLocal(GetKey(entity));
                if (localEntity != null && localEntity != entity)
                {
                    ctx.Item = entity = AutoMapper.Mapper.Map(entity, localEntity);
                }
                //if (Db.Entry(entity).State != EntityState.Added)
                Context.Entry(entity).State = EntityState.Modified;

                OnUpdated(ctx);
            }

            return entity;
        }

        public TEntity RestoreSelfDeleted(TKey key)
        {
            var entity = Get(key, required: true);
            Core().RestoreSelfDeleted(Core().CreateOperationContext(entity, DbRepoOp.RestoreSelfDeleted, Context));

            return entity;
        }

        public T GetProp<T>(object item, string name, T defaultValue = default(T))
        {
            return HProp.GetProp(item, name, defaultValue);
        }

        object IDbRepository.UpdateEntity(DbRepoOperationContext ctx)
        {
            return Update(ctx);
        }

        protected TEntity CreateEntity()
        {
            return (TEntity)Core().Create<TEntity>(Context);
        }

        protected void OnAdded(DbRepoOperationContext ctx)
        {
            Core().OnAdded(ctx);
        }

        protected void OnLoaded(TEntity entity)
        {
            Core().OnLoaded(entity, Context);
        }

        protected void OnUpdated(DbRepoOperationContext ctx)
        {
            Core().OnUpdated(ctx);
        }

        protected void OnDeleting(DbRepoOperationContext ctx)
        {
            Core().OnDeleting(ctx);
        }

        protected IQueryable<TEntity> ApplyDefaultFilters(bool includeDeleted, IQueryable<TEntity> query)
        {
            return FilterByIsDeleted(includeDeleted, FilterByTenant(query));
        }

        protected IQueryable<TEntity> FilterByTenant(IQueryable<TEntity> query)
        {
            if (TenantKeyProp == null)
                return query;

            // Filter by the CurrentTenantGuid.
            return query.Where(GetIsTenantKeyEqual(GetTenantGuid()));
        }

        protected IQueryable<TEntity> FilterByIsDeleted(bool includeDeleted, IQueryable<TEntity> query)
        {
            if (includeDeleted || IsDeletedProp == null)
                return query;

            return query.Where(GetIsNotDeleted());
        }

        protected IQueryable<TEntity> FilterById(TKey id, IQueryable<TEntity> query)
        {
            return query.Where(GetIsKeyEqual(id));
        }

        public async Task<int> GetNextSequenceValueAsync(string sequenceName)
        {
            return (await SqlQueryValueListAsync<int>("SELECT NEXT VALUE FOR [dbo].[" + sequenceName + "];")).Single();
        }

        public int GetNextSequenceValue(string sequenceName)
        {
            return SqlQueryValueList<int>("SELECT NEXT VALUE FOR [dbo].[" + sequenceName + "];").Single();
        }

        IEnumerable<TRowValue> SqlQueryValueList<TRowValue>(string query)
        {
            // Source: https://stackoverflow.com/questions/50402015/how-to-execute-sqlquery-with-entity-framework-core-2-1
            var rows = new List<TRowValue>();
            var conn = Context.Database.GetDbConnection();
            conn.Open();
            var command = conn.CreateCommand();
            command.CommandText = query;
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(reader.GetFieldValue<TRowValue>(0));
            }

            return rows;
        }

        async Task<IEnumerable<TRowValue>> SqlQueryValueListAsync<TRowValue>(string query)
        {
            // Source: https://stackoverflow.com/questions/50402015/how-to-execute-sqlquery-with-entity-framework-core-2-1
            var rows = new List<TRowValue>();
            var conn = Context.Database.GetDbConnection();
            await conn.OpenAsync();
            var command = conn.CreateCommand();
            command.CommandText = query;
            var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(reader.GetFieldValue<TRowValue>(0));
            }

            return rows;
        }

        public void ApplyTennant(TEntity entity)
        {
            if (TenantKeyProp != null && TenantKeyProp.GetValue(entity) == null)
                TenantKeyProp.SetValue(entity, GetTenantGuid());
        }

        void IDbRepository.DeleteEntity(object entity)
        {
            Delete((TEntity)entity);
        }

        void IDbRepository.DeleteEntityByKey(object key, DbRepoOperationContext ctx)
        {
            Delete((TKey)key, ctx);
        }

        void IDbRepository.DeleteEntityByKey(object key)
        {
            Delete((TKey)key);
        }

        public void Delete(TKey key, DbRepoOperationContext ctx = null, bool save = false)
        {
            // KABU TODO: Not tenant safe.
            var entity = EntitySet.Find(key);
            if (entity == null)
                return;

            Delete(entity, ctx);

            if (save) Context.SaveChanges();
        }

        public IDbRepository<TEntity> DeletePhysicallyById(TKey id)
        {
            Delete(EntitySet.Find(id), isPhysicalDeletionAuthorized: true);

            return this;
        }

        public void Delete(TEntity entity, DbRepoOperationContext ctx = null, bool? isPhysicalDeletionAuthorized = null)
        {
            Guard.ArgNotNull(entity, nameof(entity));

            if (ctx == null)
                ctx = Core().CreateOperationContext(entity, DbRepoOp.Delete, Context);
            else if (ctx.Item == null)
                ctx.Item = entity;

            if (isPhysicalDeletionAuthorized != null)
                ctx.IsPhysicalDeletionAuthorized = isPhysicalDeletionAuthorized.Value;

            // NOTE: We will process the entity *before* EF's Remove() method,
            //   because this object will have some foreign keys nullified by that method.
            //   Thus we would loose information needed in the OnDeleting handlers.
            OnDeleting(ctx);

            EntitySet.Remove(entity);
        }

        // Add ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~        

        // NOTE: Not used anywhere. Keep though.
        async Task<TEntity> AddAsync(TEntity entity, bool save = false)
        {
            entity = Add(entity);

            if (save) await SaveChangesAsync(entity.GetKey());

            return entity;
        }

        // Update ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public TEntity Update(TKey key, TEntity entity, MojDataGraphMask mask = null)
        {
            CheckEqualKey(entity, key);

            return Update(entity, mask);
        }

        // NOTE: Not used anywhere. Keep though.
        async Task<TEntity> UpdateAsync(TKey key, TEntity entity, MojDataGraphMask mask = null, bool save = false, CancellationToken? cancellationToken = null)
        {
            CheckEqualKey(entity, key);

            return await UpdateAsync(entity, mask, save, cancellationToken);
        }

        // NOTE: Not used anywhere. Keep though.
        async Task<TEntity> UpdateAsync(TEntity entity, MojDataGraphMask mask = null, bool save = false, CancellationToken? cancellationToken = null)
        {
            entity = Update(entity, mask);

            if (save) await SaveChangesAsync(GetKey(entity), cancellationToken);

            return entity;
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        // KABU TODO: Rename to EntityNotFound
        public DbRepositoryException NotFound()
        {
            return new DbRepositoryException(DbRepositoryErrorKind.NotFound, "Entity not found.");
        }

        public void Dispose()
        {
            // We can't dispose the DbContext because it might be shared.
            Context = null;
        }

        // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public bool Exists(TKey key)
        {
            if (object.Equals(key, default(TKey)))
                return false;

            // KABU TODO: Check the SQL this generates.
            return EntitySet.Any(GetIsKeyEqual(key));
        }

        public bool KeyEquals(object item, TKey key)
        {
            return EqualityComparer<TKey>.Default.Equals(((IKeyAccessor<TKey>)item).GetKey(), key);
        }

        public TKey GetKey(object item)
        {
            return ((IKeyAccessor<TKey>)item).GetKey();
        }

        public Guid GetGuid(object item)
        {
            return (Guid)GuidProp.GetValue(item);
        }

        protected TEntity FindLocal(TKey key)
        {
            // KABU TODO: Not tenant safe.
            return EntitySet.Local.AsQueryable().FirstOrDefault(GetIsKeyEqual(key));
        }

        static PropertyInfo GetKeyProperty()
        {
            var attr = typeof(TEntity).FindAttr<KeyInfoAttribute>();
            if (attr == null)
                throw new DbRepositoryException(string.Format("The type '{0}' is missing the '{1}'.",
                    typeof(TEntity).Name, typeof(KeyInfoAttribute).Name));

            return GetProperty(attr.PropName);
        }

        static PropertyInfo FindTenantKeyProperty()
        {
            var attr = typeof(TEntity).FindAttr<TenantKeyInfoAttribute>();
            if (attr == null)
                return null;

            return GetProperty(attr.PropName);
        }

        static PropertyInfo GetProperty(string name)
        {
            var prop = FindProperty(name);
            if (prop == null)
                throw new DbRepositoryException(string.Format("The specified key property '{0}' does not exist on type '{1}'.",
                    name, typeof(TEntity).Name));

            return prop;
        }

        static PropertyInfo FindProperty(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");

            var prop = typeof(TEntity).GetProperty(name);
            if (prop == null)
                return null;

            return prop;
        }

        protected Expression<Func<TEntity, bool>> GetIsNotDeleted()
        {
            return ExpressionHelper.GetEqualityPredicate<TEntity, bool>(IsDeletedProp, false);
        }

        protected Expression<Func<TEntity, bool>> GetIsKeyEqual(TKey key)
        {
            return ExpressionHelper.GetEqualityPredicate<TEntity, TKey>(KeyProp, key);
        }

        protected Expression<Func<TEntity, bool>> GetIsGuidKeyEqual(Guid key)
        {
            return ExpressionHelper.GetEqualityPredicate<TEntity, Guid>(KeyProp, key);
        }

        protected Expression<Func<TEntity, bool>> GetIsTenantKeyEqual(Guid key)
        {
            return ExpressionHelper.GetEqualityPredicate<TEntity, Guid>(TenantKeyProp, key);
        }

        public void CheckEqualKey(TEntity entity, TKey key)
        {
            if (!KeyEquals(entity, key))
                throw new DbRepositoryException(DbRepositoryErrorKind.InvalidOperation,
                    "Changin the entity key property is not allowed.");
        }
    }
}