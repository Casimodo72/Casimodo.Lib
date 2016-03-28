using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public enum MojDefaultValueCommon
    {
        [EnumMember]
        None = 0,

        [EnumMember]
        CurrentYear
    }
}
