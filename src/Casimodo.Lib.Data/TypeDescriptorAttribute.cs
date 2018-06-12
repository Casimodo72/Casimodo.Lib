using System;
using System.Linq;

namespace Casimodo.Lib.Data
{
    public static class TypeIdentityHelper
    {
        public static Guid? GetTypeGuid(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(TypeIdentityAttribute), false);
            if (attrs.Length == 0)
                return null;

            return ((TypeIdentityAttribute)attrs[0]).Guid;
        }
    }

    public class TypeIdentityAttribute : Attribute
    {
        public TypeIdentityAttribute(string guid)
        {
            Guid = new Guid(guid);
        }

        public Guid Guid { get; set; }
    }
}