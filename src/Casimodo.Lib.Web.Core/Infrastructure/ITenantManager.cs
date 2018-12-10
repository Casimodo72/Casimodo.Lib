using Microsoft.AspNetCore.Http;
using System;

namespace Casimodo.Lib.Web
{
    public interface ITenantManager
    {
        void SetDefaultTenant(ISession session = null);
        void SetTenant(Guid id, ISession session = null);
        bool TrySetTenant(Guid id, ISession session = null);
    }
}

