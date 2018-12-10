using Casimodo.Lib.Data;
using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNet.OData;
using Casimodo.Lib.ComponentModel;
using Microsoft.AspNetCore.Authorization;

namespace Casimodo.Lib.Web
{
    [Authorize]
    //[TenantScopeApiFilter] // KABU TODO: Tenant filter on OData
    public class ODataControllerBase : ODataController
    {
        protected Guid GetTenantId()
        {                 
            return ServiceLocator.Current.GetInstance<ICurrentTenantProvider>().GetTenantId(required: true).Value;
        }

        // KABU TODO: REVISIT: Since we want to use extension methods, but
        //   most of the methods of ApiController are protected (for whatever reason),
        //   we need to expose anything we need in extension methods.
        //public new OkNegotiatedContentResult<T> Ok<T>(T content)
        //{
        //    return base.Ok(content);
        //}

        //public System.Web.Http.Results.StatusCodeResult NoContent()
        //{
        //    return StatusCode(System.Net.HttpStatusCode.NoContent);
        //}

        // KABU TODO: REMOVE
        //[System.Diagnostics.DebuggerHidden]
        //public void ThrowBadRequest(string content = null, string reasonPhrase = null)
        //{
        //    var respMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
        //    if (!string.IsNullOrEmpty(content))
        //        respMessage.Content = new StringContent(content);
        //    if (!string.IsNullOrEmpty(reasonPhrase))
        //        respMessage.ReasonPhrase = reasonPhrase;
        //    throw new HttpResponseException(respMessage);
        //}

        // KABU TODO: REMOVE
        //[System.Diagnostics.DebuggerHidden]
        //public void ThrowNotFound(string content = null)
        //{            
        //    var respMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
        //    if (!string.IsNullOrEmpty(content))
        //        respMessage.Content = new StringContent(content);
        //    throw Exception(respMessage);
        //}
    }
}