using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    public interface IDbRepository<TEntity> : IDbRepository
    {
        TEntity Add(TEntity entity);
        IQueryable<TEntity> LocalOrQuery(Expression<Func<TEntity, bool>> expression);
        int SaveChanges();
    }

    public interface IDbRepository
    {
        void Use(DbContext context);
        Guid GetGuid(object entity);
        object FindEntity(object key);
        object AddEntity(DbRepoOperationContext ctx);

        object UpdateEntity(DbRepoOperationContext ctx);
        void DeleteEntity(object entity);
        void DeleteEntityByKey(object key);
        DbContext Context { get; set; }
        DbRepositoryCore Core();
    }

    public static class DbRepositoryExtensions
    {
        public static TRepo Use<TRepo>(this TRepo repository, DbContext context)
            where TRepo : IDbRepository
        {
            repository.Use(context);

            return repository;
        }

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

    public class DbRepository<TContext, TEntity, TKey> : DbRepository, IDbRepository, IDisposable
        where TContext : DbContext, new()
        where TEntity : class, IKeyAccessor<TKey>
        where TKey : struct, IComparable<TKey>
    {
        protected readonly static PropertyInfo KeyProp;
        protected readonly static PropertyInfo GuidProp;
        protected readonly static PropertyInfo TenantKeyProp;
        protected readonly static PropertyInfo IsDeletedProp;

        TContext _db;
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

        public DbRepository(DbContext db)
        {
            _db = (TContext)db;
        }

        public TContext Context
        {
            get
            {
                if (_db != null) return _db;

                lock (_lock)
                {
                    if (_db != null) return _db;
                    _db = new TContext();

                    return _db;
                }
            }
        }

        DbContext IDbRepository.Context
        {
            get { return Context; }
            set { _db = (TContext)value; }
        }

        void IDbRepository.Use(DbContext context)
        {
            lock (_lock)
            {
                _db = (TContext)context;
            }
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
            Context.Configuration.LazyLoadingEnabled = enabled;
            //Context.Configuration.ProxyCreationEnabled = enabled;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await Context.SaveChangesAsync();
        }

        // Get: Single ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>
        /// NOTE: Returns also IsDeleted entities.
        /// </summary>
        public TEntity Find(TKey? key, bool required = false)
        {
            TEntity entity = null;
            if (key != null)
                entity = EntitySet.Find(key);
            if (entity == null && required)
                throw NotFound();

            OnLoaded(entity);

            return entity;
        }

        object IDbRepository.FindEntity(object key)
        {
            return Find((TKey)key);
        }

        public async Task<TEntity> FindAsync(TKey key, bool required = false)
        {
            var entity = await EntitySet.FindAsync(key);
            if (entity == null && required)
                throw NotFound();

            OnLoaded(entity);

            return entity;
        }

        public TEntity Get(TKey key)
        {
            return Find(key, required: true);
        }

        public async Task<TEntity> GetAsync(TKey key)
        {
            return await FindAsync(key, required: true);
        }

        public TEntity Get(IQueryable<TEntity> query)
        {
            var entity = query.First();
            OnLoaded(entity);

            return entity;
        }

        public async Task<TEntity> GetAsync(IQueryable<TEntity> query)
        {
            var entity = await query.FirstAsync();
            OnLoaded(entity);

            return entity;
        }

        // Get: Query ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public IQueryable<TEntity> LocalOrQuery(Expression<Func<TEntity, bool>> expression)
        {
            return LocalOrQuery(includeDeleted: false, expression: expression);
        }

        public IQueryable<TEntity> LocalOrQuery(bool includeDeleted, Expression<Func<TEntity, bool>> expression)
        {
            var localResults = FilterByIsDeleted(includeDeleted, EntitySet.Local.AsQueryable().Where(expression));
            if (localResults.Any())
                return localResults;

            return Query(includeDeleted).Where(expression);
        }

        /// <summary>
        /// Returns an IQueryable of TEntity with the following being applied:
        /// 1) Tenant
        /// </summary>
        public IQueryable<TEntity> Query(bool includeDeleted = false)
        {
            return FilterByIsDeleted(includeDeleted, FilterByTenant(EntitySet));
        }

        // CRUD ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public int SaveChanges()
        {
            return Context.SaveChanges();
        }

        protected async Task SaveChangesAsync(TKey key, CancellationToken? cancellationToken = null)
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
            return Add(Core().CreateOperationContext(entity, DbRepoOp.Add, _db));
        }

        public TEntity Add(DbRepoOperationContext ctx)
        {
            Guard.ArgNotNull(ctx, nameof(ctx));
            ctx.Validate(DbRepoOp.Add);

            var entity = (TEntity)ctx.Item;

            ApplyTennant(entity);
            ctx.Item = entity = EntitySet.Add(entity);
            OnAdded(ctx);

            return entity;
        }

        protected DbSet<TEntity> EntitySet
        {
            get { return Context.Set<TEntity>(); }
        }

        public TEntity Update(TEntity entity, MojDataGraphMask mask = null)
        {
            return Update(Core().CreateOperationContext(entity, DbRepoOp.Update, _db, mask));
        }

        public TEntity MoveToRecycleBin(TEntity entity)
        {
            return Update(Core().CreateOperationContext(entity, DbRepoOp.UpdateMoveToRecycleBin, _db));
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

        protected void OnDeleted(TEntity entity)
        {
            Core().OnDeleted(entity, Context);
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

        public async Task<int> GetNextSequenceValueAsync(string sequenceName)
        {
            return await Context.Database.SqlQuery<int>("SELECT NEXT VALUE FOR [dbo].[" + sequenceName + "];").SingleAsync();
        }

        public int GetNextSequenceValue(string sequenceName)
        {
            return Context.Database.SqlQuery<int>("SELECT NEXT VALUE FOR [dbo].[" + sequenceName + "];").Single();
        }

        protected void ApplyTennant(TEntity entity)
        {
            if (TenantKeyProp != null && TenantKeyProp.GetValue(entity) == null)
                TenantKeyProp.SetValue(entity, GetTenantGuid());
        }

        public void Delete(TEntity entity)
        {
            Guard.ArgNotNull(entity, nameof(entity));

            EntitySet.Remove(entity);
            OnDeleted(entity);
        }

        void IDbRepository.DeleteEntity(object entity)
        {
            Delete(entity as TEntity);
        }

        void IDbRepository.DeleteEntityByKey(object key)
        {
            Delete((TKey)key);
        }

        public void Delete(TKey key, bool save = false)
        {
            var entity = EntitySet.Find(key);
            if (entity == null)
                return;

            entity = EntitySet.Remove(entity);
            OnDeleted(entity);

            if (save) Context.SaveChanges();
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected virtual Exception NotFound()
        {
            return new DbRepositoryException("Entity not found.");
        }

        public void Dispose()
        {
            // KABU TODO: REVISIT: If a controller returns an IQueryable then the DbContext
            //  must be kept alive.

            // if (_db != null) _db.Dispose();
            // _db = null;
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
    }
}