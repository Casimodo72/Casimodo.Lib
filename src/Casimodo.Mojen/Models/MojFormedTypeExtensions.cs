using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Casimodo.Lib.Mojen
{
    public static class MojFormedTypeExtensions
    {
        public static T Via<T>(this T descriptor, MojType type, MojProp prop) where T : MojFormedType
        {
            descriptor.Via(type, prop);
            return descriptor;
        }

        public static T Via<T>(this T descriptor, MojFormedNavigationPath path) where T : MojFormedType
        {
            descriptor.Via(path);
            return descriptor;
        }
    }
}