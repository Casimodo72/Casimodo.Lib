using Casimodo.Lib.Data;
using Ga.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;

namespace Casimodo.Lib.Web
{
    // KABU TODO: REMOVE? Tenant mechanism has changed.
    public class WebSessionTenantProvider : ICurrentTenantProvider
    {
        IHttpContextAccessor _httpContextAccessor;

        public WebSessionTenantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? GetTenantId(bool failWhenMissing = true)
        {
            var tenantGuid = _httpContextAccessor.HttpContext.Session.GetString(SessionVarName.TenantGuid);
            if (tenantGuid != null)
                return new Guid(tenantGuid);

            if (failWhenMissing)
                throw new ApplicationException("No tenant registered in current session.");

            return null;
        }
    }
}