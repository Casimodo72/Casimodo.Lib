using Casimodo.Lib.Data;
using Ga.Web;
using System;

namespace Casimodo.Lib.Web
{
    // KABU TODO: REMOVE? Tenant mechanism has changed.
    public class WebServiceTenantProvider : ICurrentTenantProvider
    {
        public Guid? TennantId { get; set; }

        public Guid? GetTenantId(bool failWhenMissing = true)
        {            
            if (TennantId != null && TennantId.Value != Guid.Empty)
                return TennantId.Value;

            if (failWhenMissing)
                throw new ApplicationException("There is no current tenant registered.");

            return null;
        }
    }
}