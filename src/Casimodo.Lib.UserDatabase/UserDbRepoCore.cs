using Casimodo.Lib.Data;
using System;
using System.Data.Entity;

namespace Casimodo.Lib.Identity
{
    // KABU TODO: ELIMINATE?
    public sealed class UserDbRepoCore : DbRepositoryCore
    {
        public sealed override DbRepoOperationContext CreateOperationContext(object item, DbRepoOp op, DbContext db, MojDataGraphMask mask = null)
        {
            throw new NotSupportedException("Modification operations via the DbRepositoryCore are not supported by the user DB.");
        }

        public static void OnSaving(object entity)
        {
            // KABU TODO: IMPL?
        }
    }
}