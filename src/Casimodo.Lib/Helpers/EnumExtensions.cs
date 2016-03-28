using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Casimodo.Lib
{
    public static class EnumHelper
    {
        public static IEnumerable<Enum> GetUniqueFlags(this Enum flags)
        {
            var flag = 1ul;
            foreach (var value in Enum.GetValues(flags.GetType()).Cast<Enum>())
            {
                ulong bits = Convert.ToUInt64(value);
                while (flag < bits)
                {
                    flag <<= 1;
                }

                if (flag == bits && flags.HasFlag(value))
                {
                    yield return value;
                }
            }
        }

        public static string GetDisplayName(this Enum item)
        {
            var attr = GetDisplayAttribute(item);
            return attr != null ? attr.Name : item.ToString();
        }

        public static string GetDescription(this Enum item)
        {
            var attr = GetDisplayAttribute(item);
            return attr != null ? attr.Description : item.ToString();
        }

        private static DisplayAttribute GetDisplayAttribute(object value)
        {
            Type type = value.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException(string.Format("Type {0} is not an enum", type));
            }

            // Get the enum field.
            var field = type.GetField(value.ToString());
            return field == null ? null : field.GetCustomAttribute<DisplayAttribute>();
        }

        public static List<TEnum> GetActiveFlags<TEnum>(this TEnum flags)
             where TEnum : struct
        {
            List<TEnum> result = new List<TEnum>();
            var enumValues = (TEnum[])System.Enum.GetValues(typeof(TEnum));
            foreach (var enumValue in enumValues)
            {
                if (Convert.ToInt32(enumValue) != 0 && HasFlag(flags, enumValue))
                {
                    result.Add(enumValue);
                }
            }
            return result;
        }

        static TEnum SetFlag<TEnum>(this TEnum flags, TEnum flag)
             where TEnum : struct
        {
            if (HasFlag(flags, flag))
                return flags;

            int flagsInt = Convert.ToInt32(flags);
            int flagInt = Convert.ToInt32(flag);

            flags = (TEnum)(object)(flagsInt | flagInt);

            return flags;
        }

        static TEnum SetFlag<TEnum>(this TEnum flags, TEnum flag, bool set)
             where TEnum : struct
        {
            if (set)
                return SetFlag(flags, flag);
            else
                return UnsetFlag(flags, flag);
        }

        public static TEnum UnsetFlag<TEnum>(this TEnum flags, TEnum flag)
            where TEnum : struct
        {
            int flagsInt = Convert.ToInt32(flags);
            int flagInt = Convert.ToInt32(flag);

            flags = (TEnum)(object)(flagsInt & ~flagInt);

            return flags;
        }
        
        static bool HasFlag<TEnum>(TEnum flags, TEnum flag)
             where TEnum : struct
        {
            int flagInt = Convert.ToInt32(flag);
            int flagsInt = Convert.ToInt32(flags);
            return (flagsInt & flagInt) == flagInt;
        }
    }
}