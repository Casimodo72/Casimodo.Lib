using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Casimodo.Lib.Mojen
{
    public class MojenBuildContext : MojenGeneratorBase
    {
        public MojenBuildContext()
        {
            Items = new List<MojBase>();
        }

        public void Initialize(MojenApp app)
        {
            App = app;
        }

        public MojenApp App { get; set; }

        public void Add(MojBase item)
        {
            Items.Add(item);
        }

        public List<MojBase> Items { get; private set; }

        public DataLayerConfig CurrentDataContext { get; set; }

        public ViewModelLayerConfig CurrentModelContext { get; set; }

        public MojModelBuilder AddModel(string name, string pluralName = null)
        {
            var context = GetDataContext();
            var type = MojType.CreateModel(name, pluralName);
            type.ClassName = type.Name; // TOOD: REMOVE: + "Model";
            type.DataContextName = context.Name;
            Items.Add(type);

            var builder = MojTypeBuilder.Create<MojModelBuilder>(App, type);
            builder.Namespace(context.ModelNamespace);

            return builder;
        }

        public MojModelBuilder BuildModel(MojType type)
        {
            var builder = MojTypeBuilder.Create<MojModelBuilder>(App, type);
            var context = GetDataContext();
            builder.Namespace(context.ModelNamespace);

            return builder;
        }

        public MojEntityBuilder AddEntity(string name)
        {
            var context = GetDataContext();
            var type = MojType.CreateEntity(name);
            type.DataContextName = context.Name;
            Items.Add(type);

            var builder = MojTypeBuilder.Create<MojEntityBuilder>(App, type);
            builder.NoTracking().NoValidation();
            builder.Namespace(context.DataNamespace);

            return builder;
        }

        public MojEntityBuilder BuildEntity(MojType type)
        {
            var context = GetDataContext();
            type.DataContextName = context.Name;
            if (!Items.Contains(type))
                Items.Add(type);

            var builder = MojTypeBuilder.Create<MojEntityBuilder>(App, type)
                .NoTracking()
                .NoValidation()
                .Namespace(context.DataNamespace);

            return builder;
        }

        public MojComplexTypeBuilder AddComplex(string name)
        {
            var context = GetDataContext();
            var type = MojType.CreateComplexType(name);
            Items.Add(type);

            var builder = MojTypeBuilder.Create<MojComplexTypeBuilder>(App, type)
                .NoTracking()
                .NoValidation()
                .Namespace(context.DataNamespace);

            return builder;
        }

        public DataLayerConfig GetDataContext()
        {
            var item = Items.OfType<DataLayerConfig>().FirstOrDefault();
            if (item == null)
                throw new MojenException("Data context not found.");

            return item;
        }

        public MojValueSetContainerBuilder AddItemsOfType(MojType type)
        {
            var effectiveTargetType = type.StoreOrSelf;

            var context = GetDataContext();
            var container = new MojValueSetContainer(type.Name);
            container.DataContextName = context.Name;
            Items.Add(container);

            var builder = new MojValueSetContainerBuilder(App, container);
            builder.Namespace(effectiveTargetType.Namespace);

            builder.Config.TypeConfig = effectiveTargetType;

            // Set value type and key prop name.
            var key = effectiveTargetType.Key;
            builder.Config.ValueType = key.Type.TypeNormalized;
            builder.Config.ValuePropName = key.Name;

            type.Seedings.Add(builder.Config);

            return builder;
        }

        public MojEnumBuilder AddEnum(string name)
        {
            var context = GetDataContext();
            var model = MojType.CreateEnum(name);
            model.DataContextName = context.Name;
            model.DataSetSize = MojDataSetSizeKind.ExtraSmall;
            Items.Add(model);

            var builder = new MojEnumBuilder(model);
            builder.Namespace(context.DataNamespace);

            return builder;
        }

        public MojInterfaceBuilder AddInterface(string name)
        {
            var context = GetDataContext();
            var type = MojType.CreateInterface(name);
            type.DataContextName = context.Name;
            Items.Add(type);

            var builder = new MojInterfaceBuilder(type);
            builder.Namespace(context.DataNamespace);

            return builder;
        }

        public MojAnyKeysBuilder AddKeys(string className, Type valueType)
        {
            var config = new MojAnyKeysConfig();
            config.DataContextName = GetDataContext().Name;
            config.ClassName = className;
            config.ValueType = valueType;
            Items.Add(config);

            var builder = new MojAnyKeysBuilder();
            builder.Config = config;

            return builder;
        }

        public T Get<T>()
        {
            if (typeof(T) == typeof(DataLayerConfig) && CurrentDataContext != null)
                return (T)(object)CurrentDataContext;

            if (typeof(T) == typeof(ViewModelLayerConfig) && CurrentModelContext != null)
                return (T)(object)CurrentModelContext;

            return GetItems<T>().First();
        }

        public IEnumerable<T> GetItems<T>()
        {
            return Items.OfType<T>();
        }

        public string SourceDataFilesDirPath { get; set; }

        public string MapFilePath(string virtualPath)
        {
            var absolutePath = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            var directoryName = Path.GetDirectoryName(absolutePath);
            var path = Path.Combine(directoryName, ".." + virtualPath.TrimStart('~').Replace('/', '\\'));

            return path;
        }
    }
}