using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib
{
    public static class NumericHelper
    {
        public static bool IsInteger<T>(this T obj)
            where T : struct
        {
            return IsInteger(typeof(T));
        }

        public static bool IsInteger(this Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            return TypeHelper.GetTypeInfo(type).IsPrimitive &&
                // See https://msdn.microsoft.com/en-us/library/exx3b86w.aspx
                (type == typeof(int) ||
                 type == typeof(long) ||
                 type == typeof(byte) ||
                 type == typeof(short) ||
                 type == typeof(uint) ||
                 type == typeof(ulong) ||
                 type == typeof(sbyte) ||
                 type == typeof(ushort));
        }

        public static bool IsNumber(this Type type)
        {
            if (type == null) return false;
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (TypeHelper.GetTypeInfo(type).IsPrimitive)
            {
                return type != typeof(bool) &&
                    type != typeof(char) &&
                    type != typeof(IntPtr) &&
                    type != typeof(UIntPtr);
            }

            return type == typeof(decimal);
        }

        public static bool IsDecimal(this Type type)
        {
            if (type == null) return false;
            type = Nullable.GetUnderlyingType(type) ?? type;

            return type == typeof(decimal);
        }
    }
}