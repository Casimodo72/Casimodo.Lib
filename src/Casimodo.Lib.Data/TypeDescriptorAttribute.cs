using System;

namespace Casimodo.Lib.Data
{
    public class TypeIdentityAttribute : Attribute
    {
        public TypeIdentityAttribute(string guid)
        {
            Guid = new Guid(guid);
        }

        public Guid Guid { get; set; }
    }
}