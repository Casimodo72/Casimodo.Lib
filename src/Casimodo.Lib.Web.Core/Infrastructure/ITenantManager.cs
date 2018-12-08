using System;

namespace Casimodo.Lib.Web
{
    public interface ITenantManager
    {
        void SetDefaultTenant();
        void SetTenant(Guid id);
        bool TrySetTenant(Guid id);
    }
}

