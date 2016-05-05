using Casimodo.Lib.Data;
using Ga.Web;
using System;

namespace Casimodo.Lib.Web
{
    public class WebSessionTenantProvider : ICurrentTenantProvider
    {
        public Guid? GetTenantId(bool failWhenMissing = true)
        {
            var tennantId = System.Web.HttpContext.Current.Session.Get<Guid?>(SessionVarName.TenantGuid);
            if (tennantId != null && tennantId.Value != Guid.Empty)
                return tennantId.Value;

            if (failWhenMissing)
                throw new ApplicationException("There is no current tenant registered.");

            return null;
        }
    }
}