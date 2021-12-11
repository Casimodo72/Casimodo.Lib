using System;

namespace Casimodo.Lib.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TenantKeyInfoAttribute : Attribute
    {
        public string PropName { get; set; }
    }
}