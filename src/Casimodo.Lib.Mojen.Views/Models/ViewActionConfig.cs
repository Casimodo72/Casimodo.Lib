using System;

namespace Casimodo.Lib.Mojen
{
    [Flags]
    public enum MojViewRole
    {
        None = 0,
        Index = 1 << 0,
        List = 1 << 1,
        Lookup = 1 << 2,
        Details = 1 << 3,
        Editor = 1 << 4,
        // KABU TODO: REMOVE? ForUpdate and ForCreate and never assigned.
        ForUpdate = 1 << 5,
        ForCreate = 1 << 6,
        Delete = 1 << 7
    }

    public class MojViewKindConfig
    {
        public MojViewRole Roles { get; set; }

        public string RoleName { get; set; }

        public MojViewMode Mode { get; set; }

        public string RawAction { get; set; }        
    }
}