using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    public static class WebDbRepositoryExtensions
    {
        // NOTE: Not used anywhere. Keep though.
        public static async Task<TEntity> PatchAsync<TContext, TEntity, TKey>(
            this DbRepository<TContext, TEntity, TKey> repo,
            TKey key,
            Delta<TEntity> delta,
            bool save = false,
            CancellationToken cancellationToken = default)
            where TContext : DbContext, new()
            where TEntity : class, IKeyAccessor<TKey>, new()
            where TKey : struct, IComparable<TKey>
        {
            var entity = await repo.EntitySet.FindAsync(key);
            if (entity == null)
                throw repo.NotFound();

            delta.Patch(entity);
            repo.ApplyTennant(entity);

            repo.CheckEqualKey(entity, key);

            entity = repo.Update(entity);

            if (save) await repo.SaveChangesAsync(key, cancellationToken);

            return entity;
        }

        // KABU TODO: Not used anywhere. Keep though.
        public static async Task<TModel> PatchAsync<TModel, TEntityRepo, TContext, TEntity, TKey>(
            this ModelDbRepository<TModel, TEntityRepo, TContext, TEntity, TKey> repo,
            TKey key,
            Delta<TModel> delta,
            bool save = false,
            CancellationToken cancellationToken = default)
            where TEntityRepo : DbRepository<TContext, TEntity, TKey>, new()
            where TContext : DbContext, new()
            where TEntity : class, IKeyAccessor<TKey>, new()
            where TModel : class, IKeyAccessor<TKey>
            where TKey : struct, IComparable<TKey>
        {
            // Get entity from store.
            var entity = await repo.EntitySet.FindAsync(key);
            if (entity == null)
                throw repo.NotFound();

            var model = repo.ConvertToModel(entity);

            // Apply changes to the model.
            delta.Patch(model);

            repo.CheckEqualKey(model, key);

            // Update entity
            entity = repo.Update(repo.ConvertToEntity(model, entity));

            if (save) await repo.SaveChangesAsync(key, cancellationToken);

            repo.ConvertToModel(entity, model);

            // NOTE: We call OnLoaded because we want computed properties to be updated.
            repo.OnLoaded(model);

            return model;
        }
    }
}