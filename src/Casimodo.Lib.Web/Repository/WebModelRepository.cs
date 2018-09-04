using AutoMapper;
using AutoMapper.QueryableExtensions;
using Casimodo.Lib;
using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Web.Http;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;

namespace Casimodo.Lib.Web
{
    // NOTE: Not used anymore. Keep though.
    abstract class WebModelRepository<TModel, TEntityRepo, TContext, TEntity, TKey> :
        WebRepositoryBase<TContext, TEntity, TKey>
        where TEntityRepo : WebEntityRepository<TContext, TEntity, TKey>, new()
        where TContext : DbContext, new()
        where TEntity : class, IKeyAccessor<TKey>
        where TModel : class, IKeyAccessor<TKey>
        where TKey : struct, IComparable<TKey>
    {
        public WebModelRepository()
        {
            Entities = new TEntityRepo().Use(this);
        }

        public TEntityRepo Entities { get; private set; }

        void OnLoaded(TModel model)
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

        public new TModel Get(TKey key)
        {
            return Find(key, required: true);
        }

        // Get: Collection ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~        

        /// <summary>
        /// Selects a collection of TModel with the following being applied:
        /// 1) options
        /// 2) settings
        /// 3) Tenant
        /// 4) projection to TModel
        /// 5) OnLoaded(TModel)
        /// </summary>
        public async Task<IEnumerable<TModel>> SelectAsync(ODataQueryOptions<TModel> options)
        {
            return await SelectCoreAsync(options);
        }

        public async Task<PageResult<TModel>> GetPageAsync(ODataQueryOptions<TModel> options)
        {
            var items = await SelectCoreAsync(options, settings: new ODataQuerySettings
            {
                PageSize = 20
            });

            return new PageResult<TModel>(
                items,
                GetRequest().ODataProperties().NextLink,
                GetRequest().ODataProperties().TotalCount);
        }

        /// <summary>
        /// Selects a collection of TModel with the following being applied:
        /// 1) options
        /// 2) settings
        /// 3) Tenant
        /// 4) projection to TModel
        /// 5) OnLoaded(TModel)
        /// </summary>
        async Task<IEnumerable<TModel>> SelectCoreAsync(ODataQueryOptions<TModel> options, ODataQuerySettings settings = null)
        {
            var items = (await BuildEffectiveQuery(options, settings)
                .ToListAsync())
                .Cast<TModel>();

            // Process models.            
            foreach (var item in items)
                OnLoaded(item);

            // Return models.
            return items;
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
            return query.ProjectTo<TModel>();
        }

        /// <summary>
        /// Returns the query with the following being applied:
        /// 1) given options
        /// 2) given settings
        /// 3) Tenant
        /// 4) projection to TModel
        /// </summary>
        IQueryable BuildEffectiveQuery(ODataQueryOptions options, ODataQuerySettings settings)
        {
            if (options == null)
                return Query();

            return (settings != null)
                ? options.ApplyTo(Query(), settings)
                : options.ApplyTo(Query());
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

        public async Task<TModel> PatchAsync(TKey key, Delta<TModel> delta, bool save = false, CancellationToken? cancellationToken = null)
        {
            // Get entity from store.
            // KABU TODO: Not tenant safe.
            var entity = await EntitySet.FindAsync(key);
            if (entity == null)
                throw NotFound();

            var model = ConvertToModel(entity);

            // Apply changes to the model.
            delta.Patch(model);

            CheckEqualKey(model, key);

            // Update entity
            entity = Update(ConvertToEntity(model, entity));

            if (save) await SaveChangesAsync(key, cancellationToken);

            ConvertToModel(entity, model);

            // NOTE: We call OnLoaded because we want computed properties to be updated.
            OnLoaded(model);

            return model;
        }

        // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        TModel ConvertToModel(TEntity entity)
        {
            return Mapper.Map<TModel>(entity);
        }

        TModel ConvertToModel(TEntity entity, TModel model)
        {
            return Mapper.Map(entity, model);
        }

        TEntity ConvertToEntity(TModel model, TEntity entity)
        {
            return Mapper.Map(model, entity);
        }

        void CheckEqualKey(TModel model, TKey key)
        {
            if (!KeyEquals(model, key))
            {
                throw new HttpResponseException(GetRequest().CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    "Changin the entity key property is not allowed."));
            }
        }
    }
}