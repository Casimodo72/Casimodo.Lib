using Casimodo.Lib.Data;
using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNet.OData;
using Casimodo.Lib.ComponentModel;
using Microsoft.AspNetCore.Authorization;

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

    [Authorize]
    //[TenantScopeApiFilter] // KABU TODO: Tenant filter on OData
    public class ODataControllerBase : ODataController
    {
        // KABU TODO: REMOVE? Tenant mechanism has changed.
        protected Guid GetTenantId()
        {
            return ServiceLocator.Current.GetInstance<ICurrentTenantProvider>().GetTenantId(required: true).Value;
        }

        [System.Diagnostics.DebuggerHidden]
        public void ThrowNotFound(string message = null)
        {
            new ServerException(HttpStatusCode.BadRequest, message);
        }

        [System.Diagnostics.DebuggerHidden]
        public void ThrowBadRequest(string message = null)
        {
            new ServerException(HttpStatusCode.BadRequest, message);
        }
    }
}