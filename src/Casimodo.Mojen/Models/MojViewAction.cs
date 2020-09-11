using System.Runtime.Serialization;
using Casimodo.Lib.Data;
using System;

namespace Casimodo.Lib.Mojen
{
    public enum MojViewActionKind
    {
        Toggle
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojViewActionConfig : MojBase
    {
        public MojViewActionConfig()
        { }

        [DataMember]
        public MojViewActionKind Kind { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public object DefaultValue { get; set; }

        [DataMember]
        public bool IsVisible { get; set; } = true;
    }
}