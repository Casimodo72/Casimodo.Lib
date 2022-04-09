namespace Casimodo.Mojen
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

        // TODO: REMOVE? Not used
        //public MojValueSetContainerBuilder To(MojType type)
        //{
        //    Config.TypeConfig = type;

        //    // Set value type and key prop name.
        //    var key = Config.TypeConfig.Key;
        //    Config.ValueType = key.Type.TypeNormalized;
        //    Config.ValuePropName = key.Name;

        //    type.Seedings.Add(Config);

        //    return this;
        //}

        public MojValueSetContainerBuilder ProducePrimitiveKeys()
        {
            Config.ProducesPrimitiveKeys = true;
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

        /// <summary>
        /// Sets the value of mapped property "Description".
        /// </summary>
        public MojValueSetContainerBuilder SetDescription(string description)
        {
            Values.Description(description);

            return this;
        }

        /// <summary>
        /// Sets the value of the given mapped property.
        /// </summary>
        public MojValueSetContainerBuilder Set(string name, object value)
        {
            Values.Set(name, value);

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

        public MojValueSetContainerBuilder UseDefault(string name, Func<MojValueSet, object> provider)
        {
            var item = Defaults.FirstOrDefault(x => x.Name == name);
            if (item == null)
            {
                item = CreateUseDefaultProp(name);
            }

            item.ProvideValue = provider;

            return this;
        }

        public MojValueSetContainerBuilder UseDefault(string name, object value)
        {
            var item = Defaults.FirstOrDefault(x => x.Name == name);
            if (item == null)
            {
                item = CreateUseDefaultProp(name);
            }

            item.Value = Config.ConvertFromLiteral(name, value);

            return this;
        }

        MojValueSetProp CreateUseDefaultProp(string name)
        {
            var item = new MojValueSetProp { Name = name };
            Defaults.Add(item);
            Config.UseDefaultProp(name);
            Config.UseProp(name);

            return item;
        }

        public string IndexName { get; set; }

        internal MojValueSetProp GetIndexProp(bool required = true)
        {
            var indexProp = Defaults.FirstOrDefault(x => x.Name == IndexName);
            if (indexProp == null && required)
            {
                throw new MojenException($"Not index prop defined on type '{Config.TypeConfig.Name}'.");
            }

            return indexProp;
        }

        public MojValueSetContainerBuilder ResetIndex()
        {
            GetIndexProp().Value = 0;
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
            _currentValueSetBuilder = new MojValueSetBuilder(this).Add();

            return _currentValueSetBuilder;
        }

        MojValueSetBuilder _currentValueSetBuilder;

        public MojValueSetBuilder Values => _currentValueSetBuilder;

        /// <summary>
        /// Sets the value of an index mapped property.
        /// </summary>
        public MojValueSetContainerBuilder Add(params object[] values)
        {
            var builder = Add().O(values);

            var indexProp = GetIndexProp(required: false);
            if (indexProp != null)
            {
                indexProp.Value = ((int)indexProp.Value) + 1;
            }

            _onAdded?.Invoke(builder);

            return this;
        }

        // TODO: REMOVE
        //public MojValueSetContainerBuilder With(object[] values, Action<MojValueSetBuilder> buildValueSet = null)
        //{
        //    var builder = Add().O(values);
        //    _onAdded?.Invoke(builder);
        //    buildValueSet?.Invoke(builder);
        //    return this;
        //}

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

        public bool IsAutoAggregateByNameDisabled { get; set; }

        public MojValueSetContainerBuilder DisableAutoAggregateByName()
        {
            IsAutoAggregateByNameDisabled = true;

            return this;
        }

        public MojValueSetContainer Build()
        {
            if (Config.Items.Any())
            {
                foreach (var valueSet in Config.Items)
                {
                    valueSet.Build();
                }

                if (!IsAutoAggregateByNameDisabled)
                {
                    var namePropName = Config.NamePropName ?? "Name";

                    // TODO: Get rid of auto "All" aggretage generation.
                    if (Config.Items.First().Has(namePropName) &&
                        Config.Items.All(valueSet => valueSet.Get(namePropName).Value != null))
                    {
                        Aggregate("All", Config.Items.Select(valueSet => valueSet.Get(namePropName).Value.ToString()).ToArray());
                    }
                }
            }

            ValidateCore();

            Config.WasBuild = true;

            return Config;
        }

        void ValidateCore()
        {
            if (Config.Items.Any())
            {
                if (Config.Items.Where(x => x.IsNull).Count() > 1)
                    throw new MojenException("More than one NULL value item defined.");

                if (Config.Items.Where(x => x.IsDefault).Count() > 1)
                    throw new MojenException("More than one default value item defined.");

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