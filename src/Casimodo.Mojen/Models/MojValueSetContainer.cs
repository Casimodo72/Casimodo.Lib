using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Casimodo.Lib.Mojen
{
    class MyImportPropMapping
    {
        public string PropName { get; set; }
        public string ImportPropName { get; set; }
    }

    public class MojValueSetContainer : MojPartBase, IMojValidatable
    {
        public MojValueSetContainer(string name)
        {
            Name = name;
            PluralName = MojType.Pluralize(name);
            ValueType = typeof(string);
            MetaContainerName = Name + "Values";
            KeysContainerName = Name + "Keys";
        }

        [DataMember]
        public bool IsDbSeedEnabled { get; set; } = true;

        [DataMember]
        public bool ProducesPrimitiveKeys { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string PluralName { get; set; }

        [DataMember]
        public string DataContextName { get; set; }

        [DataMember]
        public bool ClearExistingData { get; set; }

        [DataMember]
        public string MetaContainerName { get; set; }

        [DataMember]
        public string KeysContainerName { get; set; }

        public Type ValueType { get; set; }

        [DataMember]
        string _valueType;

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Namespace { get; set; }

        [DataMember]
        public string ValuePropName { get; set; }

        [DataMember]
        public string NamePropName { get; set; }

        [DataMember]
        public bool IsIndexMapping { get; set; }


        [DataMember]
        public bool IsAsync { get; set; }

        public MojType TypeConfig { get; set; }

        /// <summary>
        /// Used at build-time only.
        /// </summary>
        public List<string> SeedMappings { get; private set; } = new List<string>();

        // TODO: REMOVE: [DataMember]
        public List<MojValueSet> Items { get; private set; } = new List<MojValueSet>();

        [DataMember]
        public List<MojValueSetAggregate> Aggregates { get; private set; } = new List<MojValueSetAggregate>();

        [DataMember]
        public List<MojValueSetMapping> Mappings { get; set; } = new List<MojValueSetMapping>();

        [DataMember]
        internal List<string> AllPropNames { get; set; } = new List<string>();

        [DataMember]
        internal List<string> DefaultPropNames { get; set; } = new List<string>();

        internal List<MyImportPropMapping> ImportPropMappings { get; set; }

        public string GetImportPropName(string propName)
        {
            if (ImportPropMappings == null)
                return propName;

            var mapping = ImportPropMappings.FirstOrDefault(x => x.PropName == propName);
            if (mapping != null)
                return mapping.ImportPropName;

            return propName;
        }

        public void MapImportProp(string propName, string importPropName)
        {
            if (ImportPropMappings == null)
                ImportPropMappings = new List<MyImportPropMapping>();
            ImportPropMappings.Add(new MyImportPropMapping { PropName = propName, ImportPropName = importPropName });
        }

        internal void UseProp(string propName)
        {
            if (!AllPropNames.Contains(propName))
                AllPropNames.Add(propName);
        }

        internal void UseDefaultProp(string propName)
        {
            if (!DefaultPropNames.Contains(propName))
                DefaultPropNames.Add(propName);
        }

        //public IEnumerable<MojProp> GetProps(bool defaults = true)
        //{

        //}

        public IEnumerable<MojProp> GetSeedableProps()
        {
            var databasePropNames = TypeConfig.GetDatabaseProps().Select(x => x.Name).ToList();

            return GetProps(defaults: true)
                .Where(x => databasePropNames.Contains(x.Name));
        }

        public IEnumerable<MojProp> GetProps(bool defaults = true)
        {
            var propNames = new List<string>();
            // KABU TODO: Do we need this validation here? Should go into Build() -> Validate().
            // Validate: Check that all exiting props of all rows were registered.
            foreach (var item in Items)
            {
                foreach (var val in item.Values)
                    if (!AllPropNames.Contains(val.Name))
                        throw new MojenException($"Property '{val.Name}' was not registered in the value set container.");
            }

            propNames = AllPropNames;

            if (!defaults)
                propNames = propNames.Except(DefaultPropNames).ToList();

            return propNames.Select(x => TypeConfig.GetProp(x)).ToArray();
        }

        public object ConvertFromLiteral(string name, object value)
        {
            if (value == null || TypeConfig == null)
                return value;

            var targetProp = TypeConfig.FindProp(name);

            if (targetProp == null)
                throw new MojenException($"Seed error (prop '{name}'): Property not found on target type '{TypeConfig.ClassName}'.");

            if (targetProp.IsNavigation)
                throw new MojenException($"Seed error (prop '{name}'): Seeding of navigation properties is not supported.");

            if (targetProp != null)
            {
                if (targetProp.Type.TypeNormalized == typeof(Guid) && value is string guidString)
                {
                    value = new Guid(guidString);
                }
                else if (targetProp.Type.TypeNormalized == typeof(DateTimeOffset) && value is string dateTimeString)
                {
                    value = DateTimeOffset.Parse(dateTimeString);
                }

                CheckValidTypes(targetProp, value);
            }

            return value;
        }

        static void CheckValidTypes(MojProp targetProp, object value)
        {
            if (targetProp.Type.Type.IsAssignableFrom(value.GetType()))
                return;

            // Allow numbers.
            Type pt = targetProp.Type.Type;
            Type vt = value.GetType();
            if (pt.IsNumber() && vt.IsNumber())
                return;

            throw new MojenException(string.Format("Value of type '{0}' not assignable to property of type '{1}'.",
                value.GetType().Name, targetProp.Type.Type.Name));
        }

        public MojValueSet Get(int id)
        {
            return Items.First(x => x.SetId == id);
        }

        [OnSerializing]
        void OnSerializing(StreamingContext context)
        {
            _valueType = ValueType?.FullName;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            if (_valueType != null)
                ValueType = Type.GetType(_valueType);
        }

        public bool WasBuild { get; set; }

        void IMojValidatable.Validate()
        {
            if (!WasBuild)
            {
                throw new MojenException($"Seed container for type '{TypeConfig.Name}' was not build.");
            }
        }
    }
}