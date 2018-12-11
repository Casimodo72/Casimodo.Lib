using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    class MyImportPropMapping
    {
        public string PropName { get; set; }
        public string ImportPropName { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojValueSetContainer : MojPartBase
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

        /// <summary>
        /// Used at built-time only.
        /// </summary>
        public MojType TargetType { get; set; }

        /// <summary>
        /// Used at build-time only.
        /// </summary>
        public List<string> SeedMappings { get; private set; } = new List<string>();

        // KABU TODO: ELIMINATE
        [Obsolete("Will be removed. Not used.")]
        [DataMember]
        public List<string> IgnoredSeedMappings { get; private set; } = new List<string>();

        [DataMember]
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

        public IEnumerable<MojProp> GetProps(bool defaults = true)
        {
            var propNames = new List<string>();
            foreach (var item in Items)
            {
                foreach (var val in item.Values)
                {
                    if (!AllPropNames.Contains(val.Name))
                        throw new MojenException($"Property '{val.Name}' was not registered in the value set container.");
                }
            }

            propNames = AllPropNames;

            if (!defaults)
                propNames = propNames.Except(DefaultPropNames).ToList();

            return propNames.Select(x => TargetType.GetProp(x)).ToArray();
        }

        public object ConvertFromLiteral(string name, object value)
        {
            if (value == null || TargetType == null)
                return value;

            var targetProp = TargetType.FindProp(name);

            if (targetProp == null)
                throw new MojenException($"Seed error (prop '{name}'): Property not found on target type '{TargetType.ClassName}'.");

            if (targetProp.IsNavigation)
                throw new MojenException($"Seed error (prop '{name}'): Seeding of navigation properties is not supported.");

            if (targetProp != null)
            {
                if (targetProp.Type.TypeNormalized == typeof(Guid) && value is string)
                {
                    value = new Guid((string)value);
                }
                else if (targetProp.Type.TypeNormalized == typeof(DateTimeOffset) && value is string)
                {
                    value = DateTimeOffset.Parse((string)value);
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
            _valueType = ValueType != null ? ValueType.FullName : null;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            if (_valueType != null)
                ValueType = Type.GetType(_valueType);
        }
    }
}