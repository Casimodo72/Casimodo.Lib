using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Reflection;

namespace Casimodo.Lib.Data
{
    public abstract class CustomDbContext<TDbContext> : DbContext
        where TDbContext : CustomDbContext<TDbContext>
    {
        protected DbConnection _connection;

        protected CustomDbContext()
        { }

        public CustomDbContext(DbConnection connection)
        {
            Guard.ArgNotNull(connection);

            _connection = connection;
        }

        public CustomDbContext(DbContextOptions options)
            : base(options)
        { }

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

        public void PerformTransaction(Action<DbTransactionContext<TDbContext>> action)
        {
            // KABU TODO: EF Core's BeginTransaction does 
            //   not have an isolation level parameter (i.e. IsolationLevel.ReadCommitted).
            //   Which isolation level does it use?
            using var trans = Database.BeginTransaction();
            try
            {
                action(new DbTransactionContext<TDbContext>((TDbContext)this, trans));

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public async Task PerformTransactionAsync(Func<DbTransactionContext<TDbContext>, Task> action)
        {
            using var trans = Database.BeginTransaction();
            try
            {
                await action(new DbTransactionContext<TDbContext>((TDbContext)this, trans));

                await trans.CommitAsync();
            }
            catch (Exception)
            {
                await trans.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// TODO: REVISIT: EF Core conventions not ready yet?
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
                var attribute = decimalProperty.PropertyInfo
                    .GetCustomAttribute<PrecisionAttribute>(true);

                if (attribute != null)
                    decimalProperty.SetColumnType($"decimal({attribute.Precision},{attribute.Scale})");
            }
        }
    }
}
