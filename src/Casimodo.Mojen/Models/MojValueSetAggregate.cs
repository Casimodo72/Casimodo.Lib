using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Mojen
{
    [CollectionDataContract(Namespace = MojContract.Ns)]
    //[Serializable]
    public class MojValueSetAggregate : List<string>
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojValueSetMapping
    {
        [DataMember]
        public List<string> From { get; set; } = new List<string>();

        [DataMember]
        public string To { get; set; }
    }
}