using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojIndexConfig : MojBase
    {
        public static readonly MojIndexConfig None = new MojIndexConfig { Is = false };

        [DataMember]
        public bool Is { get; set; }

        public MojIndexConfig Clone()
        {
            var clone = (MojIndexConfig)MemberwiseClone();
            return clone;
        }
    }
}