using System.Runtime.Serialization;
using Casimodo.Lib.Data;
using System;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojBinaryConfig : MojBase
    {
        public static readonly MojBinaryConfig None = new MojBinaryConfig();

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