using System;

namespace Casimodo.Lib.Data
{
    public class TypeDescriptorAttribute : Attribute
    {
        public TypeDescriptorAttribute(string guid)
        {
            Guid = new Guid(guid);
        }

        public Guid Guid { get; set; }
    }
}