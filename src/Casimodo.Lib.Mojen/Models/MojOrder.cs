using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojOrderConfig
    {
        public static readonly MojOrderConfig None = new MojOrderConfig(false);

        public MojOrderConfig()
            : this(true)
        { }

        MojOrderConfig(bool @is)
        {
            Is = @is;
        }

        [DataMember]
        public bool Is { get; private set; }

        [DataMember]
        public int Index { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public MojOrderDirection Direction { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public enum MojOrderDirection
    {
        [EnumMember]
        Ascending = 0,

        [EnumMember]
        Descending = 1
    }
}