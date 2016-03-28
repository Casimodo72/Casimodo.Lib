using System;
using System.Collections.Generic;
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
        public string Name { get; private set; }

        [DataMember]
        public string ForeignKey { get; private set; }

        [DataMember]
        public MojReferenceBinding Binding { get; private set; }

        [DataMember]
        public MojCardinality Cardinality { get; private set; }

        [DataMember]
        public MojDataGraphMask To { get; private set; }

        internal static MojReferenceDataGraphMask Parse(XElement elem)
        {
            var reference = new MojReferenceDataGraphMask();
            reference.Name = (string)elem.Attr("Name");
            reference.Binding = elem.EnumAttr<MojReferenceBinding>("Binding");
            reference.Cardinality = elem.EnumAttr<MojCardinality>("Cardinality");
            reference.ForeignKey = (string)elem.Attr("ForeignKey");
            reference.To = new MojDataGraphMask().Parse(elem.Elem("To"));

            return reference;
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
        public string TypeName { get; private set; }

        [DataMember]
        public List<string> Properties { get; private set; }

        [DataMember]
        public List<MojReferenceDataGraphMask> References { get; private set; }
    }
}
