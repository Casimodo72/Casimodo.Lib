﻿using Casimodo.Lib.Data;
using System.Runtime.Serialization;

namespace Casimodo.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojAuthPermission
    {
        public string Role { get; set; }

        public string Permit { get; set; }
        public string Deny { get; set; }
    }
}