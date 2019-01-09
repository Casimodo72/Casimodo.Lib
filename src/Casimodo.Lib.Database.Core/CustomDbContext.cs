using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using JetBrains.Annotations;
using System.Linq;

namespace Casimodo.Lib.Data
{
    public abstract class CustomDbContext : DbContext
    {
        private DbConnection _connection;

        protected CustomDbContext()
        { }

        public CustomDbContext(DbConnection connection)
        {
            Guard.ArgNotNull(connection, nameof(connection));

            _connection = connection;
        }

        public CustomDbContext(DbContextOptions options)
            : base(options)
        { }

        //public CustomDbContext(DbConnection existingConnection, bool contextOwnsConnection)
        //    : base(existingConnection, contextOwnsConnection)
        //{ }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_connection != null)
            {
                OnConfiguringWithConnection(optionsBuilder, _connection);
                _connection = null;
            }
        }

        abstract protected void OnConfiguringWithConnection(DbContextOptionsBuilder optionsBuilder, DbConnection connection);

        // Transactions: https://docs.microsoft.com/en-us/ef/core/saving/transactions

        public void PerformTransaction(Action<DbTransactionContext> action)
        {
            // KABU TODO: EF Core's BeginTransaction does 
            //   not have an isolation level parameter (i.e. IsolationLevel.ReadCommitted).
            //   Which isolation level does it use?
            using (var trans = Database.BeginTransaction())
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
            using (var trans = Database.BeginTransaction())
            {
                try
                {
                    await action(new DbTransactionContext(this, trans));

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw ex;
                }
            }
        }

        /// <summary>
        /// TODO: IMPORTANT: REVISIT: EF Core conventions not ready yet?
        ///     See https://github.com/aspnet/EntityFrameworkCore/issues/13954
        /// </summary>
        protected void OnModelCreatingApplyDecimalPrecision(ModelBuilder builder)
        {
            foreach (var decimalProperty in builder.Model
                .GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Where(p => p.PropertyInfo != null
                            && (p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))))
            {
                var attribute = (PrecisionAttribute)decimalProperty.PropertyInfo
                    .GetCustomAttributes(typeof(PrecisionAttribute), true)
                    .FirstOrDefault();

                if (attribute != null)
                    decimalProperty.Relational().ColumnType = $"decimal({attribute.Precision},{attribute.Scale})";
            }
        }
    }
}
