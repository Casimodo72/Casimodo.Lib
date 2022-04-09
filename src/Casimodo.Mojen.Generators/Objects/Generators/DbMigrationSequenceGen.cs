using System.IO;

namespace Casimodo.Mojen
{
    public class DbMigrationSequenceGen : ClassGen
    {
        public DbMigrationSequenceGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            var config = App.Get<DataLayerConfig>();

            string outputDirPath = config.DbMigrationDirPath;
            if (string.IsNullOrWhiteSpace(outputDirPath))
                return;

            PerformWrite(Path.Combine(outputDirPath, "Sequences.generated.cs"), () =>
            {
                OUsing(
                    "System",
                    "System.Data.Entity.Migrations",
                    "Casimodo.Lib.Data.Migrations");

                ONamespace(config.DataNamespace + ".Migrations");

                O("static partial class DbMigrationContainer");
                Begin();

                var props = App.AllEntities.SelectMany(x => x.GetProps().Where(p => p.DbAnno.Sequence.IsDbSequence)).ToArray();

                O("public static void CreateSequences(DbMigration m)");
                Begin();
                foreach (var prop in props)
                {
                    O($"Create{prop.DbAnno.Sequence.Name}(m);");
                }
                End();

                O();
                O("public static void DropSequences(DbMigration m)");
                Begin();
                foreach (var prop in props)
                {
                    O($"Drop{prop.DbAnno.Sequence.Name}(m);");
                }
                End();

                foreach (var prop in props)
                {
                    var seq = prop.DbAnno.Sequence;
                    O();
                    O($"public static void Create{seq.Name}(DbMigration m)");
                    Begin();
                    O($"m.CreateSequence(\"{seq.Name}\", {seq.Start}, {seq.Increment}, {seq.Min}, {seq.Max});");
                    End();
                }

                foreach (var prop in props)
                {
                    O();
                    O($"public static void Drop{prop.DbAnno.Sequence.Name}(DbMigration m)");
                    Begin();
                    O($"m.DropSequence(\"{prop.DbAnno.Sequence.Name}\");");
                    End();
                }

                End();
                End();
            });
        }
    }
}