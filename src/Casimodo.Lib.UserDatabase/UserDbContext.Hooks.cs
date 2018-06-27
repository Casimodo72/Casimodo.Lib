// KABU TODO: REMOVE? We don't use hooks anymore.
#if (false)
using Casimodo.Lib.Data;
using System;
using System.Data.Entity;
using System.Data.Entity.Hooks;
using System.Data.Entity.Hooks.Fluent;

namespace Casimodo.Lib.Identity
{
    // KABU TODO: ELIMINATE
    public partial class UserDbContext
    {
        class SaveHook : IDbHook
        {
            readonly DbContext _db;

            public SaveHook(DbContext db)
            {
                _db = db;
            }

            public void HookEntry(IDbEntityEntry entry)
            {
                var entity = entry.Entity;

                // KABU TODO: IMPORTANT: CreatedBy, CreatedByUserId, etc.

                if (entry.State == EntityState.Added)
                {
                    // Set CreatedOn and ModifiedOn
                    var createdOn = entity.GetTypeProperty(CommonDataNames.CreatedOn);
                    if (createdOn != null)
                    {
                        var now = DateTimeOffset.UtcNow;
                        createdOn.SetValue(entity, now);
                        var modifiedOn = entity.GetTypeProperty(CommonDataNames.ModifiedOn);
                        if (modifiedOn != null)
                            modifiedOn.SetValue(entity, now);
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    // Set ModifiedOn
                    var modifiedOn = entity.GetTypeProperty(CommonDataNames.ModifiedOn);
                    if (modifiedOn != null)
                        modifiedOn.SetValue(entity, DateTimeOffset.UtcNow);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    // Set DeletedOn
                    var deletedOn = entity.GetTypeProperty(CommonDataNames.DeletedOn);
                    if (deletedOn != null)
                        deletedOn.SetValue(entity, DateTimeOffset.UtcNow);
                }

                if (entry.State == EntityState.Modified || entry.State == EntityState.Added)
                {
                    // Call any saving handlers.
                    UserDbRepoCore.OnSaving(entity);
                }
            }
        }

        object _lock = new object();
        bool _isHooked;

        void EnsureHooked()
        {
            if (_isHooked)
                return;

            lock (_lock)
            {
                if (_isHooked)
                    return;

                this.OnSave().Attach(new SaveHook(this));
#if (false)
                this.OnLoad().Attach(new LoadHook(this));
#endif

                _isHooked = true;
            }
        }
    }
}
#endif