using System.Runtime.Serialization;
using Casimodo.Lib.Data;
using System.Collections.Generic;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojAssignFromCollectionConfig
    {
        public static readonly MojAssignFromCollectionConfig None = new MojAssignFromCollectionConfig(false);

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
        public List<MojNamedAssignFromConfig> Items { get; set; } = new List<MojNamedAssignFromConfig>();
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojNamedAssignFromConfig
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<string> Properties { get; set; } = new List<string>();
    }
}