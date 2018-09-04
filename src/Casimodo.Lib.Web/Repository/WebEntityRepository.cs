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
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Extensions;

namespace Casimodo.Lib.Web
{
    /// <summary>
    /// WebEntityRepository
    /// </summary>
    /// <remarks>
    /// http://www.asp.net/web-api/overview/odata-support-in-aspnet-web-api/supporting-odata-query-options
    /// http://www.asp.net/web-api/overview/odata-support-in-aspnet-web-api/odata-v4/create-an-odata-v4-endpoint
    /// </remarks>
    public class WebEntityRepository<TContext, TEntity, TKey> :
        WebRepositoryBase<TContext, TEntity, TKey>
        where TContext : DbContext, new()
        where TEntity : class, IKeyAccessor<TKey>
        where TKey : struct, IComparable<TKey>
    {
        // Get: Collection ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>
        /// Selects a collection of TEntity with the following being applied:
        /// 1) options
        /// 2) settings
        /// 3) Tenant
        /// </summary>
        public async Task<IEnumerable<TEntity>> SelectAsync(ODataQueryOptions<TEntity> query, ODataQuerySettings settings = null)
        {
            return await SelectCoreAsync(query, settings);
        }

        // NOTE: Not used anywhere. Keep though.
        async Task<PageResult<TEntity>> GetPageAsync(ODataQueryOptions<TEntity> query)
        {
            var settings = new ODataQuerySettings()
            {
                PageSize = 20
            };

            var items = await SelectCoreAsync(query, settings);

            return new PageResult<TEntity>(
                items,
                GetRequest().ODataProperties().NextLink,
                GetRequest().ODataProperties().TotalCount);
        }

        /// <summary>
        /// Selects a collection of TEntity with the following being applied:
        /// 1) options
        /// 2) settings
        /// 3) Tenant
        /// </summary>
        async Task<IEnumerable<TEntity>> SelectCoreAsync(ODataQueryOptions<TEntity> options, ODataQuerySettings settings = null)
        {
            var items = await BuildEffectiveQuery(options, settings)
                .ToListAsync();

#if (false)
            // Set the SelectExpandClause on the request to hint the odata formatter to
            // select/expand only the fields mentioned in the SelectExpandClause.
            if (options.SelectExpand != null)
            {
                GetRequest().ODataProperties().SelectExpandClause = options.SelectExpand.SelectExpandClause;
            }
#endif

            return items.OfType<TEntity>();
        }

        // Get: Queryable ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~                

        /// <summary>
        /// Returns the query with the following being applied:
        /// 1) given options
        /// 2) given settings
        /// 3) Tenant
        /// </summary>
        IQueryable BuildEffectiveQuery(ODataQueryOptions options, ODataQuerySettings settings)
        {
            if (options == null)
                return Query();

            return (settings != null)
                ? options.ApplyTo(Query(), settings)
                : options.ApplyTo(Query());
        }

        // NOTE: Not used anywhere. Keep though.
        //public IQueryable GetAny(ODataQueryOptions<TEntity> query = null, ODataQuerySettings settings = null)
        //{
        //    return (IQueryable)BuildEffectiveQuery(query, settings);
        //}

        // NOTE: Not used anywhere. Keep though.
        //public IQueryable<TEntity> Query(ODataQueryOptions<TEntity> query = null, ODataQuerySettings settings = null)
        //{
        //    return (IQueryable<TEntity>)BuildEffectiveQuery(query, settings);
        //}

#if (DEBUG)
        /// <summary>
        /// KABU TODO: REVISIT: Just an example validation of odata query options.
        /// </summary>
        /// <param name="options"></param>
        void DevExampleValidate(ODataQueryOptions<TEntity> options)
        {
            options.Validate(new ODataValidationSettings()
            {
                AllowedFunctions = AllowedFunctions.AllMathFunctions
            });
        }
#endif

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

        // NOTE: Not used anywhere. Keep though.
        async Task<TEntity> PatchAsync(TKey key, Delta<TEntity> delta, bool save = false, CancellationToken? cancellationToken = null)
        {
            // KABU TODO: Not tenant safe.
            var entity = await EntitySet.FindAsync(key);
            if (entity == null)
                throw NotFound();

            delta.Patch(entity);
            ApplyTennant(entity);

            CheckEqualKey(entity, key);

            entity = Update(entity);

            if (save) await SaveChangesAsync(key, cancellationToken);

            return entity;
        }
    }
}