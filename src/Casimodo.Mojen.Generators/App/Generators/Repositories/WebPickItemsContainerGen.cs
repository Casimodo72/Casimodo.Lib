using System.IO;

namespace Casimodo.Lib.Mojen
{
    public class WebPickItemsContainerGen : WebPartGenerator
    {
        public WebPickItemsContainerGen()
        {
            Scope = "App";
        }

        protected override void GenerateCore()
        {
            PerformWrite(
                Path.Combine(App.Get<WebAppBuildConfig>().WebPickItemsDirPath, "PickItemsContainer.generated.cs"),
                GeneratePickItems);
        }

        void GeneratePickItems()
        {
            OUsing("System",
                "System.Linq",
                "System.Collections.Generic",
                "System.Web.Mvc",
                "Casimodo.Lib.Web",
                GetAllDataNamespaces());

            ONamespace(App.Get<WebAppBuildConfig>().WebNamespace);

            // Select MojType pick items.
            var types = App.GetRepositoryableEntities()
                //.GetTopTypes(MojTypeKind.Model, MojTypeKind.Entity).Where(x => !x.IsAbstract && x.FindKey() != null)// Use the entity if applicable.
                //.Select(x => x.Store ?? x)
                .ToArray();

            O("// NOTE: Only repositories with size 'extra small' are generated here.");
            O("public static partial class PickItemsContainer");
            Begin();

            var items = new List<string>();
            // Models/Entities as pick items.
            foreach (MojType type in types)
            {
                if (items.Contains(type.PluralName))
                    // Skip duplicates (e.g. for "TextItems").
                    continue;

                // NOTE: We now only handle extra small data sets.
                if (type.DataSetSize != MojDataSetSizeKind.ExtraSmall)
                    continue;

                items.Add(type.PluralName);

                var core = string.Format("new {0}().Query(){1}{2}, \"{3}\", display, nullable, id);",
                    GetWebRepositoryName(type),
                    new Mex().BuildLinqWhereClause(type.Conditions),
                    BuildLinqOrderBy(type.GetOrderBy()),
                    type.Key.Name);

                O($"public static MvcHtmlString Get{type.PluralName}AsJsArray(string display, bool nullable = false, bool id = true) {{ return PickItemsHelper.ToJsArray({core} }}");

                O($"public static IEnumerable<object> Get{type.PluralName}(string display, bool nullable = false, bool id = true) {{ return PickItemsHelper.ToSelectItems({core} }}");

                O();
                //public static MvcHtmlString GetCompaniesAsJsArray(string display, bool nullable = false) { return PickItemsHelper.ToJsArray(new CompaniesWebRepository().Query().OrderBy(x => x.NameShort), "Id", display, nullable); }

                // KABU TODO: REMOVE?
                if (false)
                {
#pragma warning disable CS0162 // Unreachable code detected
                    var pick = type.FindPick();
#pragma warning restore CS0162 // Unreachable code detected
                    if (pick != null)
                    {
                        OFormat("public static IEnumerable<object> Get{0}(bool nullable = false) {{ return PickItemsHelper.ToSelectItems(new {1}().Query(){2}{3}, \"{4}\", \"{5}\", nullable); }}",
                            type.PluralName,
                            GetWebRepositoryName(type),
                            new Mex().BuildLinqWhereClause(type.Conditions),
                            BuildLinqOrderBy(type.GetOrderBy()),
                            pick.KeyProp,
                            pick.DisplayProp);
                        O();
                    }
                }
            }

            // KABU TODO: REVISIT: Not needed yet.
            //// Value collections as pick items.
            // Select only value collections which are not already represented by MojTypes as pick items.
            // var pickItemCollections = App.AllValueCollections.Where(x => !pickItemTypes.Any(t => t.Name == x.Name));
            //foreach (MojValueSetContainer collection in pickItemCollections)
            //{
            //    OSummary(2, collection.Description);
            //    O("public static SelectList {0} {{ get; private set; }}", collection.PluralName);
            //}

            End();
            End();
        }
    }
}