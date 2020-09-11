using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojAnyKeyItemConfig
    {
        [DataMember]
        public string Key { get; set; }

        [DataMember]
        public object Value { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojAnyKeysConfig : MojBase
    {
        [DataMember]
        public string DataContextName { get; set; }

        [DataMember]
        public string ClassName { get; set; }

        public Type ValueType { get; set; }

        [DataMember]
        string _valueTypeFullName { get; set; }

        [DataMember]
        public bool UseInstance { get; set; }

        [DataMember]
        public List<MojAnyKeyItemConfig> Items { get; private set; } = new List<MojAnyKeyItemConfig>();

        [OnSerializing]
        void OnSerializing(StreamingContext context)
        {
            _valueTypeFullName = ValueType != null ? ValueType.FullName : null;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            if (_valueTypeFullName != null)
                ValueType = Type.GetType(_valueTypeFullName);
        }
    }
}
