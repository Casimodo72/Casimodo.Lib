﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Casimodo.Lib.Data
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojReferenceDataGraphMask
    {
        [DataMember]
        public string Name { get; internal set; }

        [DataMember]
        public string ForeignKey { get; internal set; }

        [DataMember]
        public MojReferenceBinding Binding { get; internal set; }

        [DataMember]
        public MojMultiplicity Multiplicity { get; internal set; }

        [DataMember]
        public MojDataGraphMask To { get; internal set; }

        internal static MojReferenceDataGraphMask Parse(XElement elem)
        {
            var reference = new MojReferenceDataGraphMask();
            reference.Name = (string)elem.Attr("Name");
            reference.Binding = elem.EnumAttr<MojReferenceBinding>("Binding");
            reference.Multiplicity = elem.EnumAttr<MojMultiplicity>("Multiplicity");
            reference.ForeignKey = (string)elem.Attr("ForeignKey", optional: true);
            reference.To = new MojDataGraphMask().Parse(elem.Elem("To"));

            return reference;
        }
    }

#if (DEBUG)
    /// <summary>
    /// Just a playground for the fluent API.
    /// </summary>
    class XFactory
    {
        public MojDataMaskBuilder Create()
        {
            return null;
        }

        public void Test()
        {
            Create()
                .StartReference("ContentData", MojReferenceBinding.OwnedLoose, MojMultiplicity.One)
                .EndReference()
                .Mask();
        }
    }
#endif

    public class MojDataMaskBuilder
    {
        public MojDataMaskBuilder(Type targetType)
        {
            Guard.ArgNotNull(targetType, nameof(targetType));

            _targetType = targetType;

            _mask.TypeName = _targetType.FullName;
        }

        MojDataMaskBuilder _parent;
        MojDataGraphMask _mask = new MojDataGraphMask();
        List<MojDataMaskBuilder> _propTypeBuilders = new List<MojDataMaskBuilder>();
        Type _targetType;

        public MojDataMaskBuilder Prop(string name)
        {
            Guard.ArgNotNull(name, nameof(name));

            if (_targetType.GetProperty(name) == null)
                throw new InvalidOperationException($"The type '{_targetType.Name}' does not contain a property named '{name}'.");

            _mask.Properties.Add(name);
            return this;
        }

        public MojDataMaskBuilder StartReference(string name, MojReferenceBinding binding, MojMultiplicity multiplicity)
        {
            Guard.ArgNotNull(name, nameof(name));

            var reference = new MojReferenceDataGraphMask
            {
                Name = name,
                Binding = binding,
                Multiplicity = multiplicity
            };

            var prop = _targetType.GetProperty(name);
            var foreignKeyAttr = prop
                .GetCustomAttributes(typeof(ForeignKeyAttribute), true)
                .FirstOrDefault() as ForeignKeyAttribute;

            // KABU TODO: REVISIT: We may allow non foreign key references (e.g. collections) in the future.
            if (foreignKeyAttr == null)
                throw new InvalidOperationException($"The property '{name}' of type '{_targetType.Name}' must have a 'ForeignKey' attribute.");

            reference.ForeignKey = foreignKeyAttr.Name;

            _mask.References.Add(reference);

            var builder = new MojDataMaskBuilder(prop.PropertyType);
            builder._parent = this;

            reference.To = builder._mask;

            _propTypeBuilders.Add(builder);

            return builder;
        }

        public MojDataMaskBuilder EndReference()
        {
            return _parent;
        }

        public MojDataGraphMask Mask()
        {
            return _mask;
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojDataGraphMask
    {
        internal MojDataGraphMask()
        {
            Properties = new List<string>();
            References = new List<MojReferenceDataGraphMask>();
        }

        public static MojDataGraphMask CreateFor(Type type, params string[] props)
        {
            var mask = new MojDataGraphMask();
            mask.TypeName = type.FullName;
            mask.Properties.AddRange(props);
            return mask;
        }

        public static MojDataGraphMask ParseXml(string xml)
        {
            return new MojDataGraphMask().Parse(XElement.Parse(xml));
        }

        internal MojDataGraphMask Parse(XElement elem)
        {
            TypeName = (string)elem.Attr("Type");

            foreach (var prop in elem.Elements("Prop"))
                Properties.Add((string)prop.Attr("Name"));

            foreach (var prop in elem.Elements("Ref"))
                References.Add(MojReferenceDataGraphMask.Parse(prop));

            return this;
        }

        [DataMember]
        public string TypeName { get; internal set; }

        [DataMember]
        public List<string> Properties { get; internal set; }

        [DataMember]
        public List<MojReferenceDataGraphMask> References { get; internal set; }
    }
}
