using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class CoreDbContextGen : DataLayerGenerator
    {
        public CoreDbContextGen()
        {
            Scope = "App";
        }

        protected override void GenerateCore()
        {
            foreach (var item in App.GetItems<DataLayerConfig>())
                Generate(item);
        }

        public void Generate(DataLayerConfig config)
        {
            if ((config.DbContextDirPath == null) != (config.DbContextName == null))
                throw new MojenException("Either the DbContext name or the DbContext dir path is missing.");

            if (config.DbContextName == null)
                return;

            string outputFilePath = Path.Combine(config.DbContextDirPath, config.DbContextName + ".generated.cs");
            if (config.ExistsAlready)
            {
                if (File.Exists(outputFilePath))
                    File.Delete(outputFilePath);
                return;
            }

            PerformWrite(outputFilePath, () =>
            {
                OUsing("System", "System.Data.Common", "Microsoft.EntityFrameworkCore", "Casimodo.Lib.Data");

                ONamespace(config.DataNamespace);

                OB($"public partial class {config.DbContextName}: CustomDbContext");

                // Constructor
                if (!config.NoConstructor)
                {
                    O();
                    OB($"public {config.DbContextName}()");
                    O("OnCreatingMain();");
                    End();

                    O();
                    O($"public {config.DbContextName}(DbContextOptions options)");
                    O("    : base(options)");
                    Begin();
                    O("OnCreatingMain();");
                    End();
#if (false)
                    O($"public {config.DbContextName}(string nameOrConnectionString)");
                    O("    : base(nameOrConnectionString)");
                    Begin();
                    O("OnCreatingMain();");
                    End();

                    O();
                    O($"public {config.DbContextName}(DbConnection existingConnection, bool contextOwnsConnection)");
                    O("    : base(existingConnection, contextOwnsConnection)");
                    Begin();
                    O("OnCreatingMain();");
                    End();
#endif

                    O();
                    OB("void OnCreatingMain()");
                    // EF Core: Validation does not exist in EF Core, so no need to change that one.
                    // KABU TODO: REMOVE:
                    //   EF Old: See http://stackoverflow.com/questions/8099949/entity-framework-mvc3-temporarily-disable-validation
                    //   O("// Configuration.ValidateOnSaveEnabled = false;");                   
                    //   O("// EF Old: See http://stackoverflow.com/questions/5917478/what-causes-attach-to-be-slow-in-ef4/5921259#5921259");
                    //   O("// Configuration.AutoDetectChangesEnabled = false;");
                    O("// ChangeTracker.AutoDetectChangesEnabled = false;");
                    O("OnCreating();");
                    End();

                    O();
                    O("partial void OnCreating();");
                    O();
                }

                foreach (MojType entity in App.AllConcreteEntities
                    .Where(x => x.DataContextName == config.Name)
                    .Where(x => !x.WasGenerated))
                {
                    O("public DbSet<{0}> {1} {{ get; set; }}", entity.ClassName, entity.TableName);
                }

                End();
                End();
            });
        }
    }
}