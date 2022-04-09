using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Mojen
{
    // TODO: RENAME to MojBinaryDataConfig or MojBlobConfig.
    [DataContract(Namespace = MojContract.Ns)]
    public class MojBinaryConfig : MojBase
    {
        public static readonly MojBinaryConfig None = new();

        public MojBinaryConfig()
        { }

        [DataMember]
        public bool Is { get; set; }

        // KABU TODO: ELIMINATE: @IsUploadable, since UI specific.
        [DataMember]
        [Obsolete("Will be removed, since UI specific.")]
        public bool IsUploadable { get; set; }

        [DataMember]
        public bool IsImage { get; set; }
    }
}