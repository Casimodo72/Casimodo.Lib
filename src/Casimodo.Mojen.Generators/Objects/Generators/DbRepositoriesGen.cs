using System.IO;

namespace Casimodo.Mojen
{
    public class DbRepositoriesGen : MojenGenerator
    {
        public DbRepositoriesGen()
        {
            Scope = "Context";
            Lang = "C#";
        }

        protected override void GenerateCore()
        {
            var context = App.Get<DataLayerConfig>();

            if (string.IsNullOrEmpty(context.DbRepositoryDirPath)) return;
            if (string.IsNullOrEmpty(context.DbRepositoryName)) return;

            PerformWrite(Path.Combine(context.DbRepositoryDirPath, "DbRepositories.generated.cs"),
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

            foreach (var type in types)
            {
                var db = App.GetDataLayerConfig(type.DataContextName);
                var name = GetRepositoryName(type);
                OB("public partial class {0} : {1}<{2}, {3}>",
                    name,
                    context.DbRepositoryName,
                    type.ClassName,
                    type.Key.Type.Name);

                O($"public {name}() : base() {{ }}");
                O($"public {name}({db.DbContextName} db) : base(db) {{ }}");

                End();

                O();
            }

            End();
        }
    }
}