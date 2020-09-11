using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojValueSetProp : MojBase
    {
        [DataMember]
        public int? MappingIndex { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public object Value { get; set; }

        /// <summary>
        /// Only used for file paths.
        /// </summary>
        [DataMember]
        public string Kind { get; set; }

        public string ValueToString()
        {
            return Value != null ? Value.ToString() : null;
        }
    }
}