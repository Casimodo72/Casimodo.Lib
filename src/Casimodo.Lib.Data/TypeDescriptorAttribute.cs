﻿using System;
using System.Linq;
using System.Reflection;

namespace Casimodo.Lib.Data
{
    public static class TypeIdentityHelper
    {
        public static Guid? GetTypeGuid(Type type)
        {
            return type.GetCustomAttribute<TypeIdentityAttribute>(false)?.Guid;
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