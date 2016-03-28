using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class DbDataLayerRepositoriesGen : MojenGenerator
    {
        public DbDataLayerRepositoriesGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            var context = App.Get<DataLayerConfig>();

            if (string.IsNullOrEmpty(context.DbRepositoryDirPath)) return;
            if (string.IsNullOrEmpty(context.DbRepositoryName)) return;

            PerformWrite(Path.Combine(context.DbRepositoryDirPath, "DbRepository.Cores.generated.cs"),
                () => GenerateRepositories(context));
        }

        void GenerateRepositories(DataLayerConfig context)
        {
            var types = App.GetRepositoryableEntities().ToArray();

            OUsing(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "Casimodo.Lib",
                "Casimodo.Lib.Data");

            ONamespace(App.Get<DataLayerConfig>().DataNamespace);

            // NOTE: The repositories will be internal.
            string accessModifier = "";

            foreach (var type in types)
            {
                var db = App.GetDataLayerConfig(type.DataContextName);
                var name = GetRepositoryName(type);
                O(accessModifier + "class {0} : {1}<{2}, {3}>",
                    name,
                    context.DbRepositoryName,
                    type.ClassName,
                    type.Key.Type.Name);

                Begin();
                O("public {0}() {{ }}", name);
                O("public {0}({1} db) : base(db) {{ }}", name, db.DbContextName);
                End();

                O();
            }

            End();
        }
    }
}