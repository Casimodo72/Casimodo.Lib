using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
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

            O($"sealed partial class {name} : DbRepoContainer");
            Begin();
            //O($"readonly {context.DbContextName} _db;");
            //O();
            O($"public {name}({dataConfig.DbContextName} db)");
            O("    : base(db)");
            O("{ }");
            //Begin();
            //O("_db = db;");
            //End();

            O();
            O($"public {dataConfig.DbContextName} Db {{ get {{ return ({dataConfig.DbContextName})_db; }} }}");

            foreach (var type in App.GetRepositoryableEntities())
            {
                var @class = repoNameGenerator(type);
                var prop = type.PluralName;
                var field = "_" + FirstCharToLower(prop);

                O();
                O("public {0} {1}", @class, prop);
                Begin();
                O("get {{ return {0} ?? ({0} = new {1}().Use(Db)); }}", field, @class);
                End();
                O("{0} {1};", @class, field);
            }

            End();

        }
    }
}
