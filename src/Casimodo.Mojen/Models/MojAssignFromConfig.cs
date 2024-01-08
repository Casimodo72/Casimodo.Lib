using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojAssignFromCollectionConfig
    {
        public static readonly MojAssignFromCollectionConfig None = new(false);

        public MojAssignFromCollectionConfig()
            : this(true)
        { }

        MojAssignFromCollectionConfig(bool @is)
        {
            Is = @is;
        }

        [DataMember]
        public bool Is { get; private set; }

        [DataMember]
        public List<MojNamedAssignFromConfig> Items { get; set; } = [];
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojNamedAssignFromConfig
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<string> Properties { get; set; } = [];
    }
}