﻿using Casimodo.Lib.Data;
using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNet.OData;
using Casimodo.Lib.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Casimodo.Lib.Web
{
    [Serializable]
    public class ServerException : Exception
    {
        public ServerException() { }
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
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.InternalServerError;
    }

    public abstract class StandardODataControllerBase<TDbRepository, TDbContext, TEntity, TKey> : ODataControllerBase
        where TDbRepository : DbRepository<TDbContext, TEntity, TKey>
        where TDbContext : DbContext, new()
        where TEntity : class, IKeyAccessor<TKey>, new()
        where TKey : struct, IComparable<TKey>
    {
        protected readonly TDbContext _dbcontext;
        protected readonly TDbRepository _db;

        public StandardODataControllerBase(TDbContext dbcontext, TDbRepository dbrepo)
        {
            Guard.ArgNotNull(dbcontext, nameof(dbcontext));
            Guard.ArgNotNull(dbrepo, nameof(dbrepo));

            _dbcontext = dbcontext;
            _db = dbrepo;
        }

        protected Func<IQueryable<TEntity>, IQueryable<TEntity>> CustomFilter { get; set; } = (query) => query;

        protected async Task<IActionResult> CreateCore(TEntity model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _db.ReferenceLoading(false);

            if (OnCreatingExtended != null) await OnCreatingExtended(model);
            var item = _db.Add(model);

            await _db.SaveChangesAsync();

            return Created(item);
        }

        protected Func<TEntity, Task> OnCreatingExtended = null;

        protected async Task<IActionResult> UpdateCore(TKey id, TEntity model, MojDataGraphMask mask, string group = null)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _db.ReferenceLoading(false);
            var item = _db.Update(id, model, mask);
            if (OnUpdatedExtended != null) await OnUpdatedExtended(item, group);
            await _db.SaveChangesAsync();

            return Updated(item);
        }

        protected Func<TEntity, string, Task> OnUpdatedExtended = null;
    }

    [Authorize]
    public class ODataControllerBase : ODataController
    {
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