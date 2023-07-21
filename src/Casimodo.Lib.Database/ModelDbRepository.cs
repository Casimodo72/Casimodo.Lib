using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    // Intended for projecting entities directly to models.
    public abstract class ModelDbRepository<TModel, TEntityRepo, TContext, TEntity, TKey> :
        DbRepository<TContext, TEntity, TKey>
        where TEntityRepo : DbRepository<TContext, TEntity, TKey>, new()
        where TContext : DbContext, new()
        where TEntity : class, IKeyAccessor<TKey>, new()
        where TModel : class, IKeyAccessor<TKey>
        where TKey : struct, IComparable<TKey>
    {
        public ModelDbRepository()
        {
            Entities = new TEntityRepo().Use(this);
        }

        public TEntityRepo Entities { get; private set; }

        public void OnLoaded(TModel model)
        {
            Core().OnLoaded(model, Context);
        }

        // Get: Single ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public TModel Find(TKey key, bool required = false)
        {
            // KABU TODO: Not tenant safe.
            var entity = EntitySet.Find(key);
            if (entity == null && required)
                throw NotFound();

            var model = ConvertToModel(entity);
            OnLoaded(model);

            return model;
        }

        // Get: Queryable ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>
        /// Returns an IQueryable of TModel with the following being applied:
        /// 1) Tenant
        /// 2) projection to TModel
        /// </summary>
        public new IQueryable<TModel> Query(bool includeDeleted = false, bool trackable = true)
        {
            var query = base.Query(includeDeleted, trackable);

            // Project to TModel using AutoMapper.
            return query.ProjectTo<TModel>(GetAutoMapper().ConfigurationProvider);
        }

        // CRUD ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public TModel Add(TModel model)
        {
            var entity = Add(ConvertToEntity(model, CreateEntity()));

            return ConvertToModel(entity, model);
        }

        // NOTE: Not used anywhere. Keep though.
        async Task<TModel> AddAsync(TModel model, bool save = false)
        {
            var entity = Add(ConvertToEntity(model, CreateEntity()));

            if (save) await SaveChangesAsync(model.GetKey());

            return ConvertToModel(entity, model);
        }

        public async Task<TModel> UpdateAsync(TKey key, TModel model, MojDataGraphMask mask = null, bool save = false, CancellationToken? cancellationToken = null)
        {
            CheckEqualKey(model, key);

            return await UpdateAsync(model, mask, save, cancellationToken);
        }

        public async Task<TModel> UpdateAsync(TModel model, MojDataGraphMask mask = null, bool save = false, CancellationToken? cancellationToken = null)
        {
            var entity = Update(ConvertToEntity(model, CreateEntity()), mask);

            if (save) await SaveChangesAsync(GetKey(entity), cancellationToken);

            ConvertToModel(entity, model);

            // NOTE: We call OnLoaded because we want computed properties to be updated.
            // KABU TODO: REVISIT: Should we better implement a dedicated hook for this? E.g. OnCompute?
            OnLoaded(model);

            return model;
        }

        public TModel Update(TKey key, TModel model, MojDataGraphMask mask = null)
        {
            CheckEqualKey(model, key);

            return Update(model, mask);
        }

        public TModel Update(TModel model, MojDataGraphMask mask = null)
        {
            var entity = Update(ConvertToEntity(model, CreateEntity()), mask);

            ConvertToModel(entity, model);

            // NOTE: We call OnLoaded because we want computed properties to be updated.
            // KABU TODO: REVISIT: Should we better implement a dedicated hook for this? E.g. OnCompute?
            OnLoaded(model);

            return model;
        }

        // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public TModel ConvertToModel(TEntity entity)
        {
            return GetAutoMapper().Map<TModel>(entity);
        }

        public TModel ConvertToModel(TEntity entity, TModel model)
        {
            return GetAutoMapper().Map(entity, model);
        }

        public TEntity ConvertToEntity(TModel model, TEntity entity)
        {
            return GetAutoMapper().Map(model, entity);
        }

        public void CheckEqualKey(TModel model, TKey key)
        {
            if (!KeyEquals(model, key))
                throw new DbRepositoryException(DbRepositoryErrorKind.InvalidOperation,
                    "Changin the entity key property is not allowed.");
        }
    }
}