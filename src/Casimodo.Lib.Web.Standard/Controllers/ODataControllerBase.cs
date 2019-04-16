using Casimodo.Lib.Data;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Casimodo.Lib.Web
{
    [Serializable]
    public class ServerException : Exception
    {
        public ServerException()
        { }

        public ServerException(HttpStatusCode code, string message)
            : base(message)
        {
            StatusCode = code;
        }

        public ServerException(HttpStatusCode code, string message, Exception inner)
            : base(message, inner)
        {
            StatusCode = code;
        }

        protected ServerException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }

        public HttpStatusCode StatusCode { get; private set; } = HttpStatusCode.InternalServerError;
    }

    public abstract class StandardODataControllerBase<TDbRepository, TDbContext, TEntity, TKey> : ODataControllerBase
        where TDbRepository : DbRepository<TDbContext, TEntity, TKey>
        where TDbContext : DbContext, new()
        where TEntity : class, IKeyAccessor<TKey>, new()
        where TKey : struct, IComparable<TKey>
    {
        protected readonly TDbContext _db;
        protected readonly TDbRepository _repo;

        public StandardODataControllerBase(TDbContext db, TDbRepository repo)
        {
            Guard.ArgNotNull(db, nameof(db));
            Guard.ArgNotNull(repo, nameof(repo));

            _db = db;
            _repo = repo;
        }

        protected Func<IQueryable<TEntity>, IQueryable<TEntity>> CustomFilter { get; set; } = (query) => query;

        protected async Task<IActionResult> CreateCore(TEntity model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // TODO: REMOVE for NET Core
            _repo.ReferenceLoading(false);

            if (OnCreatingExtended != null)
                await OnCreatingExtended(model);

            var item = _repo.Add(model);

            await _repo.SaveChangesAsync();

            return Created(item);
        }

        protected Func<TEntity, Task> OnCreatingExtended = null;

        protected async Task<IActionResult> UpdateCore(TKey id, TEntity model, MojDataGraphMask mask, string group = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // TODO: REMOVE for NET Core
            _repo.ReferenceLoading(false);

            var item = _repo.Update(id, model, mask);

            if (OnUpdatedExtended != null)
                await OnUpdatedExtended(item, group);

            await _repo.SaveChangesAsync();

            return Updated(item);
        }

        protected Func<TEntity, string, Task> OnUpdatedExtended = null;
    }

    [Authorize]
    public class ODataControllerBase : ODataController
    {
        [NonAction]
        public BadRequestObjectResult BadRequest(string errorMessage)
        {
            return base.BadRequest(new ODataError { ErrorCode = "400", Message = errorMessage });
        }

        [System.Diagnostics.DebuggerHidden]
        public void ThrowNotFound(string message = null)
        {
            new ServerException(HttpStatusCode.NotFound, message);
        }

        [System.Diagnostics.DebuggerHidden]
        public void ThrowBadRequest(string message = null)
        {
            new ServerException(HttpStatusCode.BadRequest, message);
        }
    }
}