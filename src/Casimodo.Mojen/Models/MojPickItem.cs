using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojPickConfig : MojBase
    {
        public MojPickConfig()
        { }

        [DataMember]
        public string KeyProp { get; set; }

        [DataMember]
        public string DisplayProp { get; set; }
    }
}