using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen.Meta
{
    public class MojenAppMetadataConfig : MojBase
    {
        public string MetadataOutputDirPath { get; set; }

        public bool IsForDataLayer { get; set; }

        public string Namespace { get; set; } = "Casimodo.Lib.MojenAppMeta";

        public List<Type> Types { get; set; } = new List<Type>();
    }

    public class MojenAppMetadataGen : MojenGenerator
    {
        public MojenAppMetadataGen()
        {
            Scope = "Meta";
        }

        string OutputDirPath { get; set; }

        string ContainerOutputFilePath { get; set; }

        string DataOutputFilePath { get; set; }

        public MojenAppMetadataConfig Config { get; set; }

        class MetaItem
        {
            public string Name { get; set; }
            public object Data { get; set; }
        }

        protected override void GenerateCore()
        {
            // Check all entity types first.
            foreach (var type in App.AllEntities)
                CheckEntityGraph(type);

            Config = App.Get<MojenAppMetadataConfig>();

            OutputDirPath = Config.MetadataOutputDirPath;

            if (string.IsNullOrWhiteSpace(OutputDirPath))
                return;

            var container = new MojenMetaContainer();
            var items = new List<MetaItem>();

            // Generate misc configs
            foreach (var t in Config.Types)
            {
                if (t == typeof(MojType))
                    continue;

                foreach (var item in App.GetItems(t).ToArray())
                {
                    container.Add(item);
                    GenerateMeta(item as DataLayerConfig);
                    // Disabled for now.
                    //GenerateMeta(item as MojValueSetContainer);
                }
            }

            // Generate MojTypes
            if (Config.Types.Contains(typeof(MojType)))
            {
                IEnumerable<MojType> types = null;
                if (Config.IsForDataLayer)
                {
                    types = App.GetTypesExcept(MojTypeKind.Model);
                }
                else
                    types = App.GetTopTypes();

                types = types.DistinctBy(x => x).OrderBy(x => x.ClassName);

                var processedTypes = new List<MojType>();

                foreach (var type in types)
                {
                    CheckType(type);
                    container.Add(type);
                    items.Add(GenerateMeta(type, processedTypes));
                }
            }

            // Container file            
            ContainerOutputFilePath = Path.Combine(OutputDirPath, "_MetaContainer.generated.cs");
            DataOutputFilePath = Path.Combine(OutputDirPath, "_MetaData.generated.data");

            GenerateContainerFile(items);

            // Serialize data            
            MojenMetaSerializer.Serialize(container.GetType(), container, DataOutputFilePath);
        }

        bool FilterOutModels(MojBase item)
        {
            return (!(item is MojType) || (item as MojType).Kind != MojTypeKind.Model);
        }

        void GenerateContainerFile(List<MetaItem> items)
        {
            PerformWrite(ContainerOutputFilePath, () =>
            {
                OUsing("System", "Casimodo.Lib", "Casimodo.Lib.Mojen", "System.Collections.Generic");

                ONamespace(Config.Namespace);

                O("public static class _MetaContainer");
                Begin();

                O("public static readonly MojenMetaContainer Data = MojenMetaSerializer.Deserialize<{0}>(@\"{1}\");",
                    typeof(MojenMetaContainer).Name, DataOutputFilePath);

                O("public static readonly List<MojBase> Items = new List<MojBase>();");
                O();
                O("public static void Init()");
                Begin();

                // Types                
                foreach (var item in items)
                    O("Items.Add({0}.Class);", item.Name);

                // Disabled for now.
#if (false)
                // ValueCollections
                foreach (var container in App.AllValueCollections)
                    if (container.Items.Any() && container.Items.First().Has(container.NamePropName))
                        O("Items.Add({0}.Class);", container.MetaContainerName);
#endif

                // Build layer configs
                foreach (var item in App.GetItems<DataLayerConfig>().Where(x => x.DbContextName != null))
                    O("Items.Add({0}.Class);", (item.MetaName ?? item.DbContextName));

                End();
                End();
                End();
            });
        }

        bool IsNavigateable(MojProp prop)
        {
            return prop.Reference.IsNavigation && !prop.Reference.IsToMany;
        }

        MetaItem GenerateMeta(MojType type, List<MojType> types)
        {
            var result = new MetaItem();
            result.Data = type;

            string typeName = type.Name;
            if (types.Any(x => x.Name == typeName))
            {
                if (type.Kind != MojTypeKind.Model || type.ClassName == type.Name)
                    throw new MojenException($"Duplicate type name '{typeName}'.");

                typeName = type.ClassName;
            }

            types.Add(type);
            result.Name = typeName;

            PerformWrite(Path.Combine(Config.MetadataOutputDirPath, typeName + ".generated.cs"), () =>
            {
                ONamespace(Config.Namespace);
                OUsing("System", "Casimodo.Lib", "Casimodo.Lib.Mojen");

                var staticFormedType = typeName;
                var formedTypeName = "Formed" + typeName;

                var props = type.GetProps().ToArray();

                MojProp pickDisplayProp = null;
                var pick = type.FindPick();
                if (pick != null)
                    pickDisplayProp = type.GetProp(pick.DisplayProp);

                // Type/property accessor class ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                O($"public class {formedTypeName} : MojFormedType");
                Begin();
                // Constructor
                O($"public {formedTypeName}()");
                Begin();
                O($"Add({staticFormedType}._Container);");
                End();
                O();
                // Properties
                var index = 0;
                foreach (var prop in props)
                {
                    GenerateProp(type, prop, index, true, pickDisplayProp);
                    index++;
                }
                End();

                // Static type/property accessor class ~~~~~~~~~~~~~~~~~~~~~~~~

                O($"public static class {staticFormedType}");
                Begin();

                // Container
                O("public static readonly MojFormedTypeContainer _Container = new MojFormedTypeContainer(_MetaContainer.Data.Get<{0}>(\"{1}\"));",
                    type.GetType().Name, type.MetadataId.ToString());

                // Class
                O("public static readonly {0} Class = _Container.Type;", type.GetType().Name);
                O();

                // Properties
                index = 0;
                foreach (var prop in props)
                {
                    GenerateProp(type, prop, index, false, pickDisplayProp);
                    index++;
                }

                O();
                O($"static {typeof(MojProp).Name} Get(int index)");
                Begin();
                O("return _Container.Get(index);");
                End();

                End(); // Static class
                End(); // Namespace

                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                O("/*");

                foreach (var prop in props)
                    O("v.Prop({0}.{1});", typeName, prop.Name);
                O();

                O();
                foreach (var prop in props)
                    O(".Map({0}.{1})", typeName, prop.Name);
                O();

                O($"All props: {props.OrderByDerivedFirst().Where(x => x.Type.Type != null).Select(x => "\"" + x.Name + "\"").Join(", ")}");

                O("*/");
            });

            return result;
        }

        void GenerateProp(MojType type, MojProp prop, int index, bool instance, MojProp pickDisplayProp)
        {
            if (prop.DeclaringType.Name != type.Name)
                O("// Inherited from {0}", prop.DeclaringType.ClassName);

            if (prop.IsOverride)
                O("// Override");

            if (prop.Reference.Is &&
                (prop.Reference.IsToOne ||
                 // KABU TODO: IMPORTANT: Dunno yet what to do with lists, but we need a formed navigation right now.
                 prop.Reference.IsToMany))
            {
                var toType = GetTypeName(prop.Reference.ToType);

                MojProp formedNavigationProp = null;

                if (prop.Reference.IsNavigation)
                {
                    formedNavigationProp = prop;
                }
                else if (!prop.AutoRelatedProps.Any(x => x.Reference.IsNavigation))
                {
                    // Add formed navigation path property if there is only a foreign key without a navigation property.
                    formedNavigationProp = prop;
                }

                if (formedNavigationProp != null)
                {
                    if (instance)
                        O($"public Formed{toType} {prop.Alias} {{ get {{ return new Formed{toType}().Via(FormedNavigationFrom).Via(_Type, Get({index})); }} }}");
                    else
                        O($"public static Formed{toType} {prop.Alias} {{ get {{ return new Formed{toType}().Via(Class, Get({index})); }} }}");
                }

                if (prop.Reference.IsNavigation)
                    return;
            }

            O($"public {(instance ? "" : "static ")}{prop.GetType().Name} {prop.Name} {{ get {{ return Get({index}); }} }}");

            if (prop == pickDisplayProp)
            {
                O($"public {(instance ? "" : "static ")}{prop.GetType().Name} _PickDisplay {{ get {{ return Get({index}); }} }}");
            }
        }

        void GenerateMeta(DataLayerConfig item)
        {
            if (item == null)
                return;

            PerformWrite(Path.Combine(Config.MetadataOutputDirPath, item.DbContextName + ".generated.cs"), () =>
            {
                ONamespace(Config.Namespace);
                OUsing("System", "System.Collections.Generic", "Casimodo.Lib", "Casimodo.Lib.Mojen");

                O("public static class {0}", (item.MetaName ?? item.DbContextName));
                Begin();

                // Class
                O("public static readonly {0} Class = _MetaContainer.Data.Get<{0}>(\"{1}\");",
                    item.GetType().Name, item.MetadataId.ToString());
                O();

                // Properties
                O("public static bool NoConstructor { get { return Class.NoConstructor; } }");
                O("public static bool ExistsAlready { get { return Class.ExistsAlready; } }");

                End();
                End();
            });
        }

        void GenerateMeta(MojValueSetContainer container)
        {
            if (container == null)
                return;

            string filePath = Path.Combine(Config.MetadataOutputDirPath, container.MetaContainerName + ".generated.cs");

            if (!container.Items.Any())
            {
                DeleteFile(filePath);
                return;
            }

            var first = container.Items.First();

            if (!first.Has(container.NamePropName))
            {
                DeleteFile(filePath);
                return;
            }

            PerformWrite(filePath, () =>
            {
                ONamespace(Config.Namespace);
                OUsing("System", "Casimodo.Lib", "Casimodo.Lib.Mojen");

                O("public static class {0}", container.MetaContainerName);
                Begin();

                // Class
                O("public static readonly {0} Class = _MetaContainer.Data.Get<{0}>(\"{1}\");",
                    container.GetType().Name, container.MetadataId.ToString());
                O();

                // Properties
                foreach (MojValueSet set in container.Items)
                {
                    O("public static {0} {1} {{ get {{ return Class.Get({2}); }} }}", typeof(MojValueSet).Name, set.Get(container.NamePropName).Value, set.SetId);
                }

                End();
                End();
            });
        }

        void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        void CheckType(MojType type)
        {
            if (type.Id == null) throw new MojenException($"ID missing on type '{type.ClassName}'.");
            if (type.Store != null && type.Store.Id == null) throw new MojenException($"ID missing on type '{type.Store.ClassName}'.");
        }

        void CheckEntityGraph(MojType type)
        {
            if (type.IsModel())
                ThrowModelInEntityGraph();

            if (type.BaseClass.IsModel())
                ThrowModelInEntityGraph();

            if (type.Store != null)
                throw new MojenException("Entity types must not have a store assigned.");

            foreach (var prop in type.GetProps(overriden: true))
            {
                CheckEntityProp(prop);
            }
        }

        void CheckEntityProp(MojProp prop)
        {
            if (prop.IsModel())
                ThrowModelInEntityGraph();

            if (prop.Store != null)
                throw new MojenException("Entity properties must not have a store assigned.");

            if (prop.Type.TypeConfig.IsModel())
                ThrowModelInEntityGraph();

            if (prop.Type.GenericTypeArguments.Any(x => x.TypeConfig.IsModel()))
                ThrowModelInEntityGraph();

            if (prop.Reference.ToType?.IsModel() == true)
                ThrowModelInEntityGraph();

            if (prop.Reference.ToTypeKey?.IsModel() == true)
                ThrowModelInEntityGraph();

            if (prop.Reference.ForeignKey?.IsModel() == true)
                ThrowModelInEntityGraph();

            if (prop.Reference.NavigationProp?.IsModel() == true)
                ThrowModelInEntityGraph();

            // KABU TODO: REMOVE
            // if (prop.Reference.ParentToChildReferenceProp?.ContainingType?.Kind == MojTypeKind.Model) ThrowModelInEntityGraph();

            if (prop.Reference.ChildToParentProp?.DeclaringType?.IsModel() == true)
                ThrowModelInEntityGraph();

            foreach (var aprop in prop.AutoRelatedProps)
                CheckEntityProp(aprop);

            foreach (var aprop in prop.CascadeFromProps)
                CheckEntityProp(aprop);
        }

        string GetTypeName(MojType type)
        {
            return type.Name;
        }

        void ThrowModelInEntityGraph()
        {
            throw new MojenException("There is a model type in the entity graph.");
        }
    }
}