using Casimodo.Lib.Data;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using Microsoft.AspNet.OData;

namespace Casimodo.Lib.Web
{
    [System.Web.Http.Authorize]
    [TenantScopeApiFilter]
    public class ODataControllerBase : ODataController
    {
        protected Guid GetTenantId()
        {
            return ServiceLocator.Current.GetInstance<ICurrentTenantProvider>().GetTenantId(required: true).Value;
        }

        // KABU TODO: REVISIT: Since we want to use extension methods, but
        //   most of the methods of ApiController are protected (for whatever reason),
        //   we need to expose anything we need in extension methods.

        public new OkNegotiatedContentResult<T> Ok<T>(T content)
        {
            return base.Ok(content);
        }

        public System.Web.Http.Results.StatusCodeResult NoContent()
        {
            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        [System.Diagnostics.DebuggerHidden]
        public void ThrowBadRequest(string content = null, string reasonPhrase = null)
        {
            var respMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
            if (!string.IsNullOrEmpty(content))
                respMessage.Content = new StringContent(content);
            if (!string.IsNullOrEmpty(reasonPhrase))
                respMessage.ReasonPhrase = reasonPhrase;

            throw new HttpResponseException(respMessage);
        }

        [System.Diagnostics.DebuggerHidden]
        public void ThrowNotFound(string content = null)
        {
            var respMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
            if (!string.IsNullOrEmpty(content))
                respMessage.Content = new StringContent(content);

            throw new HttpResponseException(respMessage);
        }
    }
}