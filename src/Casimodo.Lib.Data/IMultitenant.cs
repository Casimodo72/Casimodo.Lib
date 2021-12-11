namespace Casimodo.Lib.Data
{
    public interface IMultitenant
    {
        object GetTenantKey();
        void SetTenantKey(object tenantKey);
    }
}