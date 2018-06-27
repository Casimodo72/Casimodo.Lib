using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Microsoft.Practices.ServiceLocation;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Identity
{
    // Multitenant:
    // https://www.scottbrady91.com/ASPNET-Identity/Quick-and-Easy-ASPNET-Identity-Multitenancy
    // https://github.com/JSkimming/AspNet.Identity.EntityFramework.Multitenant/blob/master/src/AspNet.Identity.EntityFramework.Multitenant/MultitenantUserStore.Generic.cs
    public class AppUserStore : UserStore<User, Role, Guid, UserLogin, UserRole, UserClaim>
    {
        public AppUserStore(DbContext context)
            : base(context)
        { }

        public AppUserStore(DbContext context, Guid? tenantId)
            : base(context)
        {
            TenantId = tenantId;
        }

        public Guid? TenantId { get; set; }

        DbSet<UserLogin> Logins
        {
            get { return _logins ?? (_logins = Context.Set<UserLogin>()); }
        }
        DbSet<UserLogin> _logins;

        public override Task CreateAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            ThrowIfInvalid();

            user.TenantId = TenantId.Value;

            return base.CreateAsync(user);
        }

        public override Task AddLoginAsync(User user, UserLoginInfo login)
        {
            if (user == null)
                throw new ArgumentNullException("user");
            if (login == null)
                throw new ArgumentNullException("login");

            ThrowIfInvalid();

            var userLogin = new UserLogin
            {
                TenantId = TenantId.Value,
                UserId = user.Id,
                ProviderKey = login.ProviderKey,
                LoginProvider = login.LoginProvider,
            };

            user.Logins.Add(userLogin);
            return Task.FromResult(0);
        }

        public override Task<User> FindByNameAsync(string userName)
        {
            ThrowIfInvalid();
            var user = GetUserAggregateAsync(u =>
                !u.IsDeleted &&
                u.TenantId.Value.Equals(TenantId.Value) &&
                u.UserName == userName);

            return user;
        }

        /// <summary>
        /// Find a user by email
        /// </summary>
        /// <param name="email">The Email address of a <typeparamref name="TUser"/>.</param>
        /// <returns>The <typeparamref name="TUser"/> if found; otherwise <c>null</c>.</returns>
        public override Task<User> FindByEmailAsync(string email)
        {
            ThrowIfInvalid();
            return GetUserAggregateAsync(u =>
                !u.IsDeleted &&
                u.TenantId.Value.Equals(TenantId.Value) &&
                u.Email.ToLower() == email.ToLower());
        }

        /// <summary>
        /// Find a user based on the specified external login.
        /// </summary>
        /// <param name="login">The login info.</param>
        /// <returns>The user if found, otherwise <c>null</c>.</returns>
        public override async Task<User> FindAsync(UserLoginInfo login)
        {
            if (login == null)
                throw new ArgumentNullException("login");

            ThrowIfInvalid();

            Guid? userId = await
                (from l in Logins
                 where l.LoginProvider == login.LoginProvider
                       && l.ProviderKey == login.ProviderKey
                       && l.TenantId.Equals(TenantId)
                 select l.UserId)
                    .SingleOrDefaultAsync()
                    .ConfigureAwait(false);

            if (EqualityComparer<Guid?>.Default.Equals(userId, default(Guid?)))
                return null;

            return await GetUserAggregateAsync(u => u.Id.Equals(userId) && !u.IsDeleted);
        }

        void ThrowIfInvalid()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (!IsTenantAssigned())
            {
                if (!TryAssignTenant())
                    throw new InvalidOperationException("The Tenant has not been assigned.");
            }
        }

        bool TryAssignTenant()
        {
            if (!IsTenantAssigned())
                TenantId = ServiceLocator.Current.GetInstance<ICurrentTenantProvider>().GetTenantId(required: false);

            return IsTenantAssigned();
        }

        bool IsTenantAssigned()
        {
            return !EqualityComparer<Guid?>.Default.Equals(TenantId, default(Guid?));
        }

        bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (!disposing || _disposed)
                return;

            _disposed = true;
            _logins = null;
        }
    }
}