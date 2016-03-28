using System;

namespace Casimodo.Lib.Data
{
    public interface ICurrentTenantProvider
    {
        Guid? GetTenantId(bool required = true);
    }
}