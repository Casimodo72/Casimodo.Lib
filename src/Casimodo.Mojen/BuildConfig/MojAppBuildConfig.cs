using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
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
