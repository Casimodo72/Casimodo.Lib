using Casimodo.Lib;
using Casimodo.Lib.Data;
using Casimodo.Lib.Web;
using System;
using System.Data.Entity;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Casimodo.Lib.Web
{
    public static class WebRepositoryExtensions
    {
        public static TRepo Use<TRepo>(this TRepo repository, IWebRepository source)
            where TRepo : IWebRepository
        {
            repository.Use(source.Context);
            repository.Request = source.Request;

            return repository;
        }        
    }

    public interface IWebRepository : IDbRepository
    {
        HttpRequestMessage Request { get; set; }
    }

    public class WebRepositoryBase<TContext, TEntity, TKey> :
        DbRepository<TContext, TEntity, TKey>,
        IDbRepository<TEntity>,
        IWebRepository
        where TContext : DbContext, new()
        where TEntity : class, IKeyAccessor<TKey>
        where TKey : struct, IComparable<TKey>
    {
        public HttpRequestMessage Request { get; set; }

        protected HttpRequestMessage GetRequest(HttpRequestMessage request = null)
        {
            if (request == null)
                return Request;

            Request = request;
            return request;
        }

        protected override Exception NotFound()
        {
            var request = GetRequest();
            if (request != null)
                return new HttpResponseException(request.CreateResponse(HttpStatusCode.NotFound));

            return base.NotFound();
        }

        // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        protected void CheckEqualKey(TEntity entity, TKey key)
        {
            if (!KeyEquals(entity, key))
            {
                throw new HttpResponseException(GetRequest().CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    "Changin the entity key property is not allowed."));
            }
        }
    }
}