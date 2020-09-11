using System;
using System.Collections.Generic;
using Casimodo.Lib.ComponentModel;
using Casimodo.Lib.Data;
using Microsoft.AspNetCore.Identity;

namespace Casimodo.Lib.Identity
{
    /// <summary>
    /// Multitenant ASP.NET Core identity user store.
    /// </summary>
    public class AppUserStore : AppUserStoreBase<User, Role, UserDbContext, Guid, UserClaim, UserRole, UserLogin, UserToken, RoleClaim>
    {
        public AppUserStore(UserDbContext context, IdentityErrorDescriber describer = null)
            : base(context, describer)
        {
            ConfigureForTenant();
        }

        //public AppUserStore(UserDbContext context, Guid? tenantId)
        //    : this(context)
        //{
        //    TenantId = tenantId;
        //}

        void ConfigureForTenant()
        {
            OnCreatingUser = (u) => u.TenantId = TenantId;
            UserFilter = (u) => u.TenantId == TenantId;

            OnCreatingUserLogin = (ul) => ul.TenantId = TenantId;
            UserLoginFilter = (ul) => ul.TenantId == TenantId;
        }

        public Guid? TenantId { get; set; }

        protected override void ThrowIfInvalid()
        {
            base.ThrowIfInvalid();

            if (!IsTenantAssigned() && !TryAssignTenant())
                throw new InvalidOperationException("The Tenant has not been assigned.");
        }

        bool TryAssignTenant()
        {
            TenantId = ServiceLocator.Current.GetInstance<ICurrentTenantProvider>().GetTenantId(required: false);

            return IsTenantAssigned();
        }

        bool IsTenantAssigned()
        {
            return TenantId != null && TenantId != Guid.Empty;
        }
    }
}