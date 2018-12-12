using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class DbSeedItemRunner : MojenGenerator
    {
        public DbSeedItemRunner()
        {
            Scope = "App";
        }

        protected override void GenerateCore()
        {
            //foreach (var item in App.GetItems<MojSeedItem>())
            //    item.Execute();
        }
    }
}