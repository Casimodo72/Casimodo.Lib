namespace Casimodo.Mojen
{
    public class MojValueSetBuilder
    {
        public MojValueSetBuilder(MojValueSetContainerBuilder containerBuilder)
        {
            ContainerBuilder = containerBuilder;
            Container = ContainerBuilder.Config;
            TypeProps.AddRange(Container.TypeConfig.GetProps().Select(x => x.Name));
        }

        public MojValueSetContainerBuilder ContainerBuilder { get; set; }

        public MojValueSetContainer Container { get; set; }

        public MojValueSet ValuesConfig { get; set; }

        public List<string> TypeProps { get; set; } = new List<string>();

        int MappingIndex { get; set; }

        /// <summary>
        /// Adds a new value set.
        /// </summary>
        public MojValueSetBuilder Add()
        {
            var valueSet = new MojValueSet
            {
                SetId = Container.Items.Count
            };
            Container.Items.Add(valueSet);

            // Increase "Index" value if applicable.
           
            //if (ContainerBuilder.IndexName != null && Container.Items.Count > 1)
            //{
            //    var index = (int)ContainerBuilder.IndexProp.Value;
            //    ContainerBuilder.IndexProp.Value = index + 1;
            //}

            // Set default values specified on the container builder.
            foreach (var defaultValue in ContainerBuilder.Defaults)
            {
                valueSet.Get(defaultValue.Name, create: true)
                    .AssignValueFrom(defaultValue);
            }

            // TODO: REMOVE
            //if (ContainerBuilder.IndexName != null && Container.Items.Count > 1)
            //{
            //    var prevSet = Container.Items[^2];
            //    var index = (int)prevSet.Get(ContainerBuilder.IndexName).GetValue(prevSet);
            //    index++;

            //    valueSet.Get(ContainerBuilder.IndexName, create: true).Value = index;
            //}

            ValuesConfig = valueSet;
            MappingIndex = 0;

            return this;
        }

        /// <summary>
        /// Sets the value of an index mapped property.
        /// </summary>
        public MojValueSetBuilder O(params object[] values)
        {
            foreach (var value in values)
                Set(value);

            return this;
        }

        ///// <summary>
        ///// Sets the value of an index mapped property.
        ///// </summary>
        //public MojValueSetBuilder O(string value)
        //{
        //    return Set(value);
        //}

        /// <summary>
        /// Sets the value of mapped property "Id".
        /// </summary>
        public MojValueSetBuilder Id(int id)
        {
            return Set("Id", id);
        }

        /// <summary>
        /// Sets the value of mapped property "Value".
        /// </summary>
        public MojValueSetBuilder Key(object value)
        {
            return Set("Value", value);
        }

        /// <summary>
        /// Sets the value of mapped property "Name".
        /// </summary>
        public MojValueSetBuilder Name(string name)
        {
            return Set("Name", name);
        }

        /// <summary>
        /// Sets the value of mapped property "DisplayValue".
        /// </summary>
        public MojValueSetBuilder Display(string displayValue)
        {
            return Set("DisplayValue", displayValue);
        }

        /// <summary>
        /// Sets the value of mapped property "Guid".
        /// </summary>
        public MojValueSetBuilder Id(string guid)
        {
            return Set("Id", new Guid(guid));
        }

        /// <summary>
        /// Sets the value of mapped property "Description".
        /// </summary>
        public MojValueSetBuilder Description(string description)
        {
            ValuesConfig.Description = ValuesConfig.Description != null
                ? ValuesConfig.Description + " " + description
                : description;

            if (TypeProps.Contains("Description"))
                ValuesConfig.Get("Description", create: true).Value = ValuesConfig.Description;

            return this;
        }

        /// <summary>
        /// Marks the current value set as the defaull set.
        /// </summary>
        public MojValueSetBuilder Default()
        {
            ValuesConfig.IsDefault = true;
            return this;
        }

        /// <summary>
        /// Marks the current value set as the null object set.
        /// </summary>
        public MojValueSetBuilder Null()
        {
            ValuesConfig.IsNull = true;
            return this;
        }

        MojValueSetBuilder Set(object value)
        {
            if (!Container.IsIndexMapping || Container.SeedMappings.Count <= MappingIndex)
                throw new MojenException("No matching value -> property mapping found.");

            string name = Container.SeedMappings[MappingIndex];

            var prop = SetCore(name, value);
            prop.MappingIndex = MappingIndex;

            MappingIndex++;

            return this;
        }

        /// <summary>
        /// Sets the value of the given mapped property.
        /// </summary>
        public MojValueSetBuilder Set(string name, object value)
        {
            SetCore(name, value);
            return this;
        }

        MojValueSetProp SetCore(string name, object value)
        {
            if (!TypeProps.Contains(name))
                throw new MojenException($"Property '{name}' not found in type '{Container.TypeConfig.ClassName}'.");

            MojValueSetProp prop = ValuesConfig.Get(name, create: true);

            if (ContainerBuilder._mapPropValue != null)
                value = ContainerBuilder._mapPropValue(name, value);

            prop.Value = Container.ConvertFromLiteral(name, value);

            return prop;
        }

        /// <summary>
        /// Sets the value of the given mapped file property.
        /// Use only with Blobs in order to load the file from the filesystem during seed.
        /// </summary>
        public MojValueSetBuilder SetSeedFileName(string name, string fileName)
        {
            var item = ValuesConfig.Get(name, create: true);
            var targetProp = Container.TypeConfig.GetProp(name);
            // Check valid type.
            if (!targetProp.Type.IsByteArray)
                throw new MojenException($"Property '{targetProp.Name}' of type '{targetProp.Type.NameNormalized}' cannot hold binary data.");

            item.Value = fileName;
            item.Kind = "FileName";

            return this;
        }

        public MojValueSetBuilder AuthRoles(string roles)
        {
            if (roles == null) return this;

            var items = roles.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length == 0)
                return this;

            foreach (var role in items)
            {
                if (!ValuesConfig.AuthRoles.Contains(role))
                    ValuesConfig.AuthRoles.Add(role);
            }

            return this;
        }

        public MojValueSetBuilder Pw(string pw)
        {
            ValuesConfig.Pw = pw;

            return this;
        }
    }
}