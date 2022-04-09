using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojSequenceConfig : MojBase
    {
        public static readonly MojSequenceConfig None = new() { Is = false };

        [DataMember]
        public bool Is { get; set; }

        [DataMember]
        public bool IsDbSequence { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int? Start { get; set; }

        [DataMember]
        public MojFormedNavigationPath StartSelector { get; set; }

        [DataMember]
        public MojFormedNavigationPath EndSelector { get; set; }

        [DataMember]
        public int Increment { get; set; }

        [DataMember]
        public int Min { get; set; }

        [DataMember]
        public int Max { get; set; }
    }
}