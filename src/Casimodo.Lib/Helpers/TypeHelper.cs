// Copyright (c) 2009 Kasimier Buchcik

using System;
using System.Reflection;

namespace Casimodo.Lib
{
    public static class TypeHelper
    {
        public static TAttr FindTypeAttr<TType, TAttr>()
            where TAttr : class
        {
            return typeof(TType).GetCustomAttribute(typeof(TAttr)) as TAttr;
        }

        public static TAttr FindAttr<TAttr>(this Type type)
            where TAttr : class
        {
            return type.GetCustomAttribute(typeof(TAttr)) as TAttr;
        }

        public static T GetPropValueOrDefault<T>(object item, string name, T defaultValue = default(T))
        {
            var prop = item.GetTypeProperty(name);
            if (prop == null)
                return defaultValue;

            T value = (T)prop.GetValue(item);

            if (object.Equals(value, default(T)))
                return defaultValue;

            return value;
        }

        public static PropertyInfo GetTypeProperty(this object obj, string name, bool required = false)
        {
            if (obj == null)
                return null;

            var prop = obj.GetType().GetProperty(name);
            if (prop == null && required)
                throw new Exception(string.Format("Property not found ('{0}') on type '{1}'.", name, obj.GetType().Name));

            return prop;
        }

        public static bool IsNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;

            // return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}