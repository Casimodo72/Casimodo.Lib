using System;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    public class CustomDbContext : DbContext
    {
        protected CustomDbContext()
        { }

        public CustomDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        { }

        public CustomDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        { }

        public void PerformTransaction(Action<DbTransactionContext> action)
        {
            using (var trans = Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    action(new DbTransactionContext(this, trans));

                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }


        public async Task PerformTransactionAsync(Func<DbTransactionContext, Task> action)
        {
            using (var trans = Database.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    await action(new DbTransactionContext(this, trans));

                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }
    }
}
