using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Casimodo.Lib.Identity
{
    // UserStore: https://github.com/aspnet/AspNetCore/blob/master/src/Identity/src/EF/UserStore.cs
    // UserStoreBase: https://github.com/aspnet/AspNetCore/blob/master/src/Identity/src/Stores/UserStoreBase.cs
    public abstract class AppUserStoreBase<TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>
        : UserStore<TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TContext : DbContext
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>, new()
        where TUserRole : IdentityUserRole<TKey>, new()
        where TUserLogin : IdentityUserLogin<TKey>, new()
        where TUserToken : IdentityUserToken<TKey>, new()
        where TRoleClaim : IdentityRoleClaim<TKey>, new()
    {
        public AppUserStoreBase(TContext context, IdentityErrorDescriber describer = null)
            : base(context, describer)
        { }    

        protected Action<TUser> OnCreatingUser = null;
        protected Action<TUserLogin> OnCreatingUserLogin = null;
        protected Func<TUser, bool> UserFilter = (u) => true;
        protected Func<TUserLogin, bool> UserLoginFilter = (u) => true;

        // The following was copied & modified from UserStoreBase and UserStore:

        private DbSet<TUserClaim> UserClaims { get { return Context.Set<TUserClaim>(); } }
        private DbSet<TUserRole> UserRoles { get { return Context.Set<TUserRole>(); } }
        private DbSet<TUserLogin> UserLogins { get { return Context.Set<TUserLogin>(); } }

        /// <summary>
        /// Creates the specified <paramref name="user"/> in the user store.
        /// </summary>
        /// <param name="user">The user to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the creation operation.</returns>
        public override Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            // NOTE: Modified UserStore implementation due to tenant.

            if (user == null)
                throw new ArgumentNullException("user");

            ThrowIfInvalid();

            OnCreatingUser?.Invoke(user);

            return base.CreateAsync(user, cancellationToken);
        }

        /// <summary>
        /// Called to create a new instance of a <see cref="IdentityUserLogin{TKey}"/>.
        /// </summary>
        /// <param name="user">The associated user.</param>
        /// <param name="login">The sasociated login.</param>
        /// <returns></returns>
        protected override TUserLogin CreateUserLogin(TUser user, UserLoginInfo login)
        {
            // NOTE: Modified UserStoreBase implementation due to tenant.
            // UserStoreBase: https://github.com/aspnet/AspNetCore/blob/master/src/Identity/src/Stores/UserStoreBase.cs
            ThrowIfInvalid();
            var userLogin = new TUserLogin
            {
                UserId = user.Id,
                ProviderKey = login.ProviderKey,
                LoginProvider = login.LoginProvider,
                ProviderDisplayName = login.ProviderDisplayName
            };

            OnCreatingUserLogin?.Invoke(userLogin);

            return userLogin;
        }

        /// <summary>
        /// Finds and returns a user, if any, who has the specified normalized user name.
        /// </summary>
        /// <param name="normalizedUserName">The normalized user name to search for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the user matching the specified <paramref name="normalizedUserName"/> if it exists.
        /// </returns>
        public override Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken))
        {
            // NOTE: Modified UserStore implementation due to tenant.
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfInvalid();
            return Users.FirstOrDefaultAsync(u => UserFilter(u) && u.NormalizedUserName == normalizedUserName, cancellationToken);
        }

        /// <summary>
        /// Return a user login with  provider, providerKey if it exists.
        /// </summary>
        /// <param name="loginProvider">The login provider name.</param>
        /// <param name="providerKey">The key provided by the <paramref name="loginProvider"/> to identify a user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The user login if it exists.</returns>
        protected override Task<TUserLogin> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            // NOTE: Modified UserStore implementation due to tenant.
            ThrowIfInvalid();
            return UserLogins.SingleOrDefaultAsync(userLogin => UserLoginFilter(userLogin) && userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey, cancellationToken);
        }

        /// <summary>
        /// Gets the user, if any, associated with the specified, normalized email address.
        /// </summary>
        /// <param name="normalizedEmail">The normalized email address to return the user for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The task object containing the results of the asynchronous lookup operation, the user if any associated with the specified normalized email address.
        /// </returns>
        public override Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfInvalid();
            return Users.FirstOrDefaultAsync(u => UserFilter(u) && u.NormalizedEmail == normalizedEmail, cancellationToken);
        }

        /// <summary>
        /// Retrieves all users with the specified claim.
        /// </summary>
        /// <param name="claim">The claim whose users should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> contains a list of users, if any, that contain the specified claim. 
        /// </returns>       
        public async override Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            // NOTE: Modified base implementation due to tenant.
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfInvalid();

            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            var query = from userclaims in UserClaims
                        join user in Users on userclaims.UserId equals user.Id
                        where userclaims.ClaimValue == claim.Value
                            && userclaims.ClaimType == claim.Type
                            && UserFilter(user) // NOTE: Modified base implementation due to tenant.
                        select user;

            return await query.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves all users in the specified role.
        /// </summary>
        /// <param name="normalizedRoleName">The role whose users should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> contains a list of users, if any, that are in the specified role. 
        /// </returns>
        public async override Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            // NOTE: Modified base implementation due to tenant.
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfInvalid();
            if (string.IsNullOrEmpty(normalizedRoleName))
            {
                throw new ArgumentNullException(nameof(normalizedRoleName));
            }

            var role = await FindRoleAsync(normalizedRoleName, cancellationToken);

            if (role != null)
            {
                var query = from userrole in UserRoles
                            join user in Users on userrole.UserId equals user.Id
                            where userrole.RoleId.Equals(role.Id)
                             && UserFilter(user) // NOTE: Modified base implementation due to tenant.
                            select user;

                return await query.ToListAsync(cancellationToken);
            }
            return new List<TUser>();
        }

        protected virtual void ThrowIfInvalid()
        {
            ThrowIfDisposed();
        }
    }
}