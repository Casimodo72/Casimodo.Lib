using Casimodo.Lib.Data;
using System.Runtime.Serialization;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojAuthPermission
    {
        public string Roles { get; set; }

        public string Permit { get; set; }
        public string Deny { get; set; }
    }
}