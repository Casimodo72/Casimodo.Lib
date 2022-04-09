using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojTypeComparison : MojBase
    {
        public MojTypeComparison()
        {
            Mode = "none";
            IncludedProps = new List<string>();
            ExcludedProps = new List<string>();
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public MojType Type { get; set; }

        [DataMember]
        public string Mode { get; set; }

        [DataMember]
        public bool UseNavitationProps { get; set; }

        [DataMember]
        public bool UseListProps { get; set; }

        [DataMember]
        public bool UseNonStoredProps { get; set; }

        [DataMember]
        public List<string> IncludedProps { get; private set; }

        [DataMember]
        public List<string> ExcludedProps { get; private set; }
    }
}