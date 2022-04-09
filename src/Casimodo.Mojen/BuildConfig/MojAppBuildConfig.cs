using Casimodo.Lib.Data;
using System.Runtime.Serialization;

namespace Casimodo.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class AppBuildConfig : MojenBuildConfig
    {
        public AppBuildConfig()
        { }

        public AppBuildConfig(string metadataId)
        {
            Guard.ArgNotNull(metadataId, nameof(metadataId));

            MetadataId = metadataId;
        }

        [DataMember]
        public string AppNamePrefix { get; set; }
    }
}
