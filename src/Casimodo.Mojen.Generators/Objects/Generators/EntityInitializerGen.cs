using System.IO;

namespace Casimodo.Mojen
{
    public class DbDataHelperGen : DataLayerGenerator
    {
        public DbDataHelperGen()
        {
            Lang = "C#";
            Scope = "Context";
        }

        protected override void GenerateCore()
        {
            if (DataConfig.IsOutputDisabled)
                return;

            if (string.IsNullOrEmpty(DataConfig.EntityDirPath))
                return;

            var types = App.GetTypes(MojTypeKind.Entity)
                .Where(x => !x.IsAbstract && !x.IsTenant)
                .ToArray();

            var name = (DataConfig.TypePrefix ?? "") + "DbDataHelper";

            PerformWrite(Path.Combine(DataConfig.EntityDirPath, name + ".generated.cs"), () =>
             {
                 ONamespace(DataConfig.DataNamespace);

                 OB($"public static class {name}");
                 foreach (var type in types)
                 {
                     Generate(type);
                 }
                 End();

                 End();
             });
        }

        public void Generate(MojType type)
        {
            OB($"public static void ClearLooseReferences({type.Name} item)");

            var referenceProps = type.GetProps().Where(x => x.IsNavigation);

            foreach (var prop in referenceProps)
            {
                if (prop.IsHiddenCollectionNavigationProp)
                    continue;

                if (prop.Reference.IsToMany)
                {
                    // TODO: Currently we only support saving of independent collection props.
                    //   Neither nested or loose collections are saved currently.
                    // TODO: EF Core: we don't use intependent collections in EF Core
                    //   because they are not supported (yet?).
                    if (!prop.Reference.Independent)
                    {
                        // We null the property in this case.
                        O($"item.{prop.Name} = null;");
                    }
                }
                else if (prop.Reference.IsToOne)
                {
                    if (prop.Reference.IsLoose)
                    {
                        // Set all non-nested referenced entities to null,
                        //   because we don't want EF to add those referenced entities.
                        //   We need the foreign keys only.

                        O($"item.{prop.Name} = null;");
                    }
                    else if (prop.Reference.IsNested)
                    {
                        // Initialize the nested object.
                        O("// Preserve nested object.");
                        O($"if (item.{prop.Name} != null) InitForSaving(item.{prop.Name});");
                    }
                }
            }

            End();
            O();
        }
    }
}