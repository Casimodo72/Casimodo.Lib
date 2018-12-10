using System.Data.Entity;

namespace Casimodo.Lib.Data
{
    public abstract class DbRepoOperationContext<TDb, TRepoAggregate> : DbRepoOperationContext
        where TDb : DbContext
        where TRepoAggregate : DbRepoContainer
    {
        public TDb Db { get; set; }
        public TRepoAggregate Repos { get; set; }

        public sealed override DbContext GetDb()
        {
            return Db;
        }

        public sealed override DbRepoContainer GetRepos()
        {
            return Repos;
        }
    }
}