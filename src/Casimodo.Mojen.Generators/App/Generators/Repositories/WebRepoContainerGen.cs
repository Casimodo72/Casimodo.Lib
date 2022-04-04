using System.IO;

namespace Casimodo.Lib.Mojen
{
    public class WebRepoContainerGen : WebPartGenerator
    {
        public WebRepoContainerGen()
        {
            Scope = "App";
        }

        public override MojenGenerator Initialize(MojenApp app)
        {
            base.Initialize(app);
            RepoContainerGen.SetParent(this);

            return this;
        }

        public DbRepoContainerGen RepoContainerGen { get; set; } = new DbRepoContainerGen();

        protected override void GenerateCore()
        {
            if (string.IsNullOrEmpty(WebConfig.WebRepositoriesDirPath)) return;

            PerformWrite(Path.Combine(WebConfig.WebRepositoriesDirPath, "DbRepoContainer.generated.cs"),
                () => GenerateRepoContainer());
        }

        void GenerateRepoContainer()
        {
            OUsing(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "Casimodo.Lib",
                "Casimodo.Lib.Data",
                "Casimodo.Lib.Web",
                GetAllDataNamespaces()
            );

            ONamespace(App.Get<WebAppBuildConfig>().WebNamespace);

            var types = App.GetRepositoryableTypes().ToArray();
            GenerateRepoTypes(types);

            GenerateRepoContainerTypes();

            End();
        }

        public void GenerateRepoContainerTypes()
        {
            foreach (var dataConfig in App.GetItems<DataLayerConfig>())
            {
                if (string.IsNullOrWhiteSpace(dataConfig.DbRepoContainerName))
                    continue;

                RepoContainerGen.GenerateRepoContainerType(dataConfig, GetWebRepositoryName);
            }
        }

        public void GenerateRepoTypes(IEnumerable<MojType> types, string accessModifier = "")
        {
            accessModifier = !string.IsNullOrWhiteSpace(accessModifier) ? accessModifier + " " : "";

            foreach (var type in types)
            {
                string repositoryName = GetWebRepositoryName(type);

                var context = App.GetDataLayerConfig(type.DataContextName);

                if (type.Kind == MojTypeKind.Model)
                {
                    // KABU TODO: REVISIT: We don't use model repositories anymore (or currently).
                    if (false)
                    {
#pragma warning disable CS0162 // Unreachable code detected
                        O(accessModifier + "public class {0} : WebModelRepository<{1}, {2}, {3}, {4}, {5}>{6}",
#pragma warning restore CS0162 // Unreachable code detected
                            repositoryName,
                            type.ClassName,
                            GetWebRepositoryName(type.Store),
                            context.DbContextName,
                            type.Store.ClassName,
                            type.Key.Type.Name,
                            // Repository base interface
                            (context.DbRepositoryName != null ? ", I" + context.DbRepositoryName : ""));

                        O("{ }");
                    }

                }
                else if (type.Kind == MojTypeKind.Entity)
                {
                    O(accessModifier + "public class {0} : WebEntityRepository<{1}, {2}, {3}>{4}",
                        repositoryName,
                        context.DbContextName,
                        type.ClassName,
                        type.Key.Type.Name,
                        // Repository base interface
                        (context.DbRepositoryName != null ? ", I" + context.DbRepositoryName : ""));

                    O("{ }");
                    O();
                }
            }
        }
    }
}