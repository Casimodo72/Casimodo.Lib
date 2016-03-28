using System.Runtime.Serialization;
using Casimodo.Lib.Data;

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

        [DataMember]
        public bool IsUploadable { get; set; }

        [DataMember]
        public bool IsImage { get; set; }
    }
}