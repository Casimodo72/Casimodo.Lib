// Copyright (c) 2009 Kasimier Buchcik

using System;
using System.Reflection;

namespace Casimodo.Lib
{
    public static class TypeHelper
    {
#if (WINDOWS_UWP)
        public static TypeInfo GetTypeInfo(Type type)
        {
            return type.GetTypeInfo();
        }
#else
        public static Type GetTypeInfo(Type type)
        {
            return type;
        }
#endif

        public static TAttr FindTypeAttr<TType, TAttr>()
            where TAttr : class
        {
            return GetTypeInfo(typeof(TType)).GetCustomAttribute(typeof(TAttr)) as TAttr;
        }

        public static TAttr FindAttr<TAttr>(this Type type)
            where TAttr : class
        {
            return GetTypeInfo(type).GetCustomAttribute(typeof(TAttr)) as TAttr;
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

        public static bool IsSimple(Type type)
        {
            // Source: https://stackoverflow.com/questions/863881/how-do-i-tell-if-a-type-is-a-simple-type-i-e-holds-a-single-value

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // Nullable type, check if the nested type is simple.
                return IsSimple(type.GetGenericArguments()[0]);
            }

            return type.IsPrimitive || type.IsEnum || type.Equals(typeof(string)) || type.Equals(typeof(decimal));
        }
    }
}