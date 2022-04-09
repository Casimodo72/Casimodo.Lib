using System.IO;

namespace Casimodo.Mojen
{
    public class DbRepoContainerGen : MojenGenerator
    {
        public DbRepoContainerGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            var context = App.Get<DataLayerConfig>();

            if (string.IsNullOrEmpty(context.DbRepositoryDirPath)) return;
            if (string.IsNullOrEmpty(context.DbContextName)) return;
            if (string.IsNullOrEmpty(context.DbRepoContainerName)) return;

            PerformWrite(Path.Combine(context.DbRepositoryDirPath, context.DbRepoContainerName + ".generated.cs"), () =>
            {
                GenerateRepoContainer(context);
            });
        }

        void GenerateRepoContainer(DataLayerConfig dataConfig)
        {
            OUsing("System", "Casimodo.Lib", "Casimodo.Lib.Data");

            ONamespace(dataConfig.DataNamespace);

            GenerateRepoContainerType(dataConfig, GetRepositoryName);

            End();
        }

        public void GenerateRepoContainerType(DataLayerConfig dataConfig, Func<MojType, string> repoNameGenerator)
        {
            string name = dataConfig.DbRepoContainerName;

            O($"public sealed partial class {name} : DbRepoContainer");
            Begin();

            O($"public {name}()");
            O($"    : base(new {dataConfig.DbContextName}())");
            O("{ }");

            O();
            O($"public {name}({dataConfig.DbContextName} db)");
            O("    : base(db)");
            O("{ }");

            O();
            O($"public {dataConfig.DbContextName} Db {{ get {{ return ({dataConfig.DbContextName})_db; }} }}");

            foreach (var type in App.GetRepositoryableEntities())
            {
                var @class = repoNameGenerator(type);
                var prop = type.PluralName;
                var field = "_" + FirstCharToLower(prop);

                O();
                O($"public {@class} {prop}");
                Begin();
                OFormat("get {{ return {0} ?? ({0} = new {1}(Db)); }}", field, @class);
                End();
                O($"{@class} {field};");
            }

            End();

        }
    }
}
