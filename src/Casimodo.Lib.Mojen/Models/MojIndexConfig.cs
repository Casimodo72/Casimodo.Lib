using System.Collections.Generic;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojIndexParticipantConfig : MojBase
    {
        [DataMember]
        public MojIndexPropKind Kind { get; set; }

        [DataMember]
        public MojProp Prop { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojIndexConfig : MojBase
    {
        public static readonly MojIndexConfig None = new MojIndexConfig { Is = false };

        [DataMember]
        public bool Is { get; set; }

        [DataMember]
        public bool IsUnique { get; set; }

        [DataMember]
        public readonly List<MojIndexParticipantConfig> Participants = new List<MojIndexParticipantConfig>();

        //public MojIndexConfig Clone()
        //{
        //    var clone = (MojIndexConfig)MemberwiseClone();
        //    return clone;
        //}

        public override string ToString()
        {
            return $"I: {Is}, U: {IsUnique}";
        }
    }
}