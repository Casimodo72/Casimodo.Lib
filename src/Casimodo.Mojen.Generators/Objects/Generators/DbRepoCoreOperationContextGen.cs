using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public class DbRepoOperationContextGen : MojenGenerator
    {
        public DbRepoOperationContextGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            var context = App.Get<DataLayerConfig>();

            if (string.IsNullOrEmpty(context.DbRepositoryDirPath)) return;
            if (string.IsNullOrEmpty(context.DbContextName)) return;
            if (string.IsNullOrEmpty(context.DbRepoOperationContextName)) return;

            PerformWrite(Path.Combine(context.DbRepositoryDirPath, context.DbRepoOperationContextName + ".generated.cs"), () =>
            {
                Generate(context);
            });
        }

        void Generate(DataLayerConfig context)
        {
            OUsing("System", "Casimodo.Lib", "Casimodo.Lib.Data");
            ONamespace(context.DataNamespace);

            // NOTE: The class is internal
            O($"sealed partial class {context.DbRepoOperationContextName} : DbRepoOperationContext<{context.DbContextName}, {context.DbRepoContainerName}>");
            O("{ }");

            End();
        }
    }
}
