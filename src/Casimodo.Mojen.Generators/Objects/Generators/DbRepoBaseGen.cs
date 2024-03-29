﻿using System.IO;

namespace Casimodo.Mojen
{
    public class DbRepoBaseGen : MojenGenerator
    {
        public DbRepoBaseGen()
        {
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            var context = App.Get<DataLayerConfig>();

            if (string.IsNullOrEmpty(context.DbRepositoryDirPath)) return;
            if (string.IsNullOrEmpty(context.DbRepositoryName)) return;
            if (string.IsNullOrEmpty(context.DbContextName)) return;

            PerformWrite(Path.Combine(context.DbRepositoryDirPath, context.DbRepositoryName + ".generated.cs"), () =>
            {
                GenerateBaseRepository(context);
            });
        }

        void GenerateBaseRepository(DataLayerConfig context)
        {
            OUsing("System", "Casimodo.Lib", "Casimodo.Lib.Data");

            ONamespace(context.DataNamespace);

            string name = context.DbRepositoryName;

            O($"public partial interface I{name} : IDbRepository");
            O("{ }");

            O();
            OFormat("public partial class {0}<TEntity, TKey> : DbRepository<{1}, TEntity, TKey>, I{0}",
                name,
                context.DbContextName);

            O("    where TEntity : class, IKeyAccessor<TKey>, new()");
            O("    where TKey : struct, IComparable<TKey>");
            Begin();
            O($"public {name}() {{ }}");
            O($"public {name}({context.DbContextName} db) : base(db) {{ }}");
            End();

            End();
        }
    }
}
