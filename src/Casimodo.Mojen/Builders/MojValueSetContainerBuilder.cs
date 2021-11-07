using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class MojValueSetContainerBuilder : MojenBuilder<MojValueSetContainerBuilder>
    {
        public MojValueSetContainerBuilder(MojenApp app, MojValueSetContainer config)
        {
            Guard.ArgNotNull(app, nameof(app));
            Guard.ArgNotNull(config, nameof(config));

            App = app;
            Config = config;
        }

        public MojValueSetContainer Config { get; private set; }

        internal List<MojValueSetProp> Defaults { get; private set; } = new List<MojValueSetProp>();

        public List<MojValueSetAggregateBuilder> Aggregates { get; private set; } = new List<MojValueSetAggregateBuilder>();

        Action<MojValueSetBuilder> _onAdded;

        Func<string, string> _mapPropName;
        internal Func<string, object, object> _mapPropValue;

        public MojValueSetContainerBuilder OnAdded(Action<MojValueSetBuilder> action)
        {
            _onAdded = action;
            return this;
        }

        public MojValueSetContainerBuilder MapName(Func<string, string> func)
        {
            _mapPropName = func;
            return this;
        }

        public MojValueSetContainerBuilder MapValue(Func<string, object, object> func)
        {
            _mapPropValue = func;
            return this;
        }

        public MojValueSetContainerBuilder Namespace(string ns)
        {
            Config.Namespace = ns;
            return this;
        }

        public MojValueSetContainerBuilder To(MojType type)
        {
            Config.TypeConfig = type;

            // Set value type and key prop name.
            var key = Config.TypeConfig.Key;
            Config.ValueType = key.Type.TypeNormalized;
            Config.ValuePropName = key.Name;

            type.Seedings.Add(Config);

            return this;
        }

        public MojValueSetContainerBuilder ProducePrimitiveKeys()
        {
            Config.ProducesPrimitiveKeys = true;
            return this;
        }

        // KABU TODO: ELIMINATE
        [Obsolete("Will be removed")]
        MojValueSetContainerBuilder Ignore(params string[] propertyNames)
        {
            foreach (var prop in propertyNames)
            {
                if (!Config.IgnoredSeedMappings.Contains(prop))
                    Config.IgnoredSeedMappings.Add(prop);
            }

            return this;
        }

        public MojValueSetContainerBuilder ValueType(Type type)
        {
            Config.ValueType = type;
            return this;
        }

        public MojValueSetContainerBuilder DbSeedEnabled(bool enabled)
        {
            Config.IsDbSeedEnabled = enabled;
            return this;
        }

        public MojValueSetContainerBuilder SeedAllProps()
        {
            ClearSeedProps();
            //Config.AllPropNames.Clear();
            return Seed(Config.TypeConfig.GetDatabaseProps()
                .Reverse()
                .Select(x => x.Name)
                .ToArray());
        }

        public MojValueSetContainerBuilder Seed(params string[] propertyNames)
        {
            var effectiveNames = propertyNames.ToList();

            if (_mapPropName != null)
                for (int i = 0; i < effectiveNames.Count; i++)
                    effectiveNames[i] = _mapPropName(effectiveNames[i]);

            foreach (var prop in effectiveNames)
            {
                if (Defaults.Any(x => x.Name == prop))
                    throw new MojenException($"The property '{prop}' was configured as a property with a default value.");
            }
            Config.IsIndexMapping = true;
            Config.SeedMappings.AddRange(effectiveNames);
            foreach (var propName in effectiveNames)
                Config.UseProp(propName);

            return this;
        }

        public MojValueSetContainerBuilder Value(string valueName)
        {
            Config.ValuePropName = valueName;

            Config.UseProp(valueName);

            if (Config.TypeConfig != null)
            {
                // Set value type of key.
                var prop = Config.TypeConfig.GetProp(valueName);
                Config.ValueType = prop.Type.TypeNormalized;
            }

            return this;
        }

        public MojValueSetContainerBuilder Name(string name)
        {
            Config.NamePropName = name;

            Config.UseProp(name);

            return this;
        }

        public MojValueSetContainerBuilder UseAsync()
        {
            Config.IsAsync = true;

            return this;
        }

        public MojValueSetContainerBuilder MapImportProp(string propName, string importPropName)
        {
            Config.MapImportProp(propName, importPropName);

            return this;
        }

        public MojValueSetContainerBuilder RemoveProp(string propName)
        {
            Config.AllPropNames.Remove(propName);
            Config.SeedMappings.Remove(propName);

            return this;
        }

        public MojValueSetContainerBuilder UseDefault(string name, object value)
        {
            var item = Defaults.FirstOrDefault(x => x.Name == name);
            if (item == null)
            {
                item = new MojValueSetProp { Name = name };
                Defaults.Add(item);
                Config.UseDefaultProp(name);
                Config.UseProp(name);
            }

            item.Value = Config.ConvertFromLiteral(name, value);

            return this;
        }

        public string IndexName { get; set; }

        public MojValueSetContainerBuilder ResetIndex()
        {
            var item = Defaults.First(x => x.Name == IndexName);
            item.Value = 0;
            return this;
        }

        public MojValueSetContainerBuilder ClearExistingData()
        {
            Config.ClearExistingData = true;
            return this;
        }

        /// <summary>
        /// NOTE: Clears also default values.
        /// </summary>
        /// <returns></returns>
        public MojValueSetContainerBuilder ClearSeedProps()
        {
            //foreach (var prop in Config.AllPropNames.ToArray())
            //{
            //if (prop == IndexName ||
            //    prop == Config.NamePropName ||
            //    prop == Config.ValuePropName)
            //    continue;

            //if (Config.DefaultPropNames.Contains(prop))
            //    continue;

            //Config.AllPropNames.Remove(prop);
            //}
            Config.AllPropNames.Clear();
            Config.DefaultPropNames.Clear();
            Config.SeedMappings.Clear();

            return this;
        }

        public MojValueSetContainerBuilder UseIndex(string name = "Index")
        {
            IndexName = name;
            Defaults.Add(new MojValueSetProp
            {
                Name = IndexName,
                Value = 0
            });
            Config.UseProp(name);
            Config.UseDefaultProp(name);

            return this;
        }

        internal protected override List<MojUsingGeneratorConfig> UsingGenerators
        {
            get { return Config.UsingGenerators; }
        }

        public MojValueSetBuilder Add()
        {
            return new MojValueSetBuilder(this).Add();
        }

        /// <summary>
        /// Sets the value of an index mapped property.
        /// </summary>
        public MojValueSetBuilder Add(params object[] values)
        {
            var builder = new MojValueSetBuilder(this).Add().O(values);

            _onAdded?.Invoke(builder);

            return builder;
        }

        public MojValueSetContainerBuilder With(params object[] values)
        {
            var builder = new MojValueSetBuilder(this).Add().O(values);
            _onAdded?.Invoke(builder);

            return this;
        }

        public MojValueSetContainerBuilder With(object[] values, Action<MojValueSetBuilder> buildValueSet = null)
        {
            var builder = new MojValueSetBuilder(this).Add().O(values);
            _onAdded?.Invoke(builder);
            buildValueSet?.Invoke(builder);

            return this;
        }

        public MojValueSetBuilder AddDummy()
        {
            return new MojValueSetBuilder(this).AddDummy();
        }

        public MojValueSetContainerBuilder Mapping(string from, string to)
        {
            var map = new MojValueSetMapping { To = to };
            map.From.Add(from);
            Config.Mappings.Add(map);

            return this;
        }

        public MojValueSetContainerBuilder Mapping(string[] from, string to)
        {
            var map = new MojValueSetMapping { To = to };
            map.From.AddRange(from);
            Config.Mappings.Add(map);

            return this;
        }

        public MojValueSetAggregateBuilder Aggregate(string name, params string[] valueNames)
        {
            return Add(new MojValueSetAggregateBuilder(name)).Add(valueNames);
        }

        MojValueSetAggregateBuilder Add(MojValueSetAggregateBuilder builder)
        {
            Aggregates.Add(builder);
            Config.Aggregates.Add(builder.Aggregate);
            return builder;
        }

        public MojValueSetContainer Build()
        {
            if (Config.Items.Any() &&
                Config.Items.First().Has("Name") &&
                Config.Items.All(x => x.Get("Name").Value != null))
            {
                Aggregate("All", Config.Items.Select(x => x.Get("Name").Value.ToString()).ToArray());
            }

            Validate();

            return Config;
        }

        void Validate()
        {
            if (Config.Items.Any())
            {
                if (Config.Items.Where(x => x.IsNull).Count() > 1)
                    throw new MojenException("More than one NULL value item defined.");

                if (Config.Items.Where(x => x.IsDefault).Count() > 1)
                    throw new MojenException("More than one default value item defined.");

                //var defaultDupls = Items.GroupBy(x => x.IsDefault).Where(g => g.Count() > 1).ToList();
                //if (defaultDupls.Count != 0) // ValueConfig
                //    throw new CodeGenException(
                //        string.Format("More than one default items ({0}).", defaultDupls.First().Select(x => x.Name).Aggregate((acc, n) => acc + ", " + n)));

                if (Config.NamePropName != null && Config.Items.First().Has(Config.NamePropName))
                {
                    var valueNameDupls = Config.Items.GroupBy(x => x.Get(Config.NamePropName).Value).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
                    if (valueNameDupls.Count != 0) throw new MojenException("Duplicate value names.");
                }

                if (Config.ValuePropName != null && Config.Items.First().Has(Config.ValuePropName))
                {
                    var valueValueDupls = Config.Items.GroupBy(x => x.Get(Config.ValuePropName).Value).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
                    if (valueValueDupls.Count != 0) throw new MojenException("Duplicate values.");
                }
            }

            var aggDupls = Aggregates.GroupBy(x => x.Aggregate.Name).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
            if (aggDupls.Count != 0) throw new MojenException("Duplicate aggregate names.");

            foreach (var agg in Aggregates)
                agg.Validate();
        }
    }
}