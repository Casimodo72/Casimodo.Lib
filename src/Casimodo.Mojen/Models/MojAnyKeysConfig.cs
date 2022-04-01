using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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
#pragma warning disable IDE1006 // Naming Styles
        string _valueTypeFullName { get; set; }
#pragma warning restore IDE1006 // Naming Styles

        [DataMember]
        public bool UseInstance { get; set; }

        [DataMember]
        public List<MojAnyKeyItemConfig> Items { get; private set; } = new List<MojAnyKeyItemConfig>();

        [OnSerializing]
        void OnSerializing(StreamingContext context)
        {
            _valueTypeFullName = ValueType?.FullName;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            if (_valueTypeFullName != null)
                ValueType = Type.GetType(_valueTypeFullName);
        }
    }
}
