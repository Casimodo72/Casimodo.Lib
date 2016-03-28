using System;

namespace Casimodo.Lib
{
    // KABU TODO: REMOVE: Use a dedicated foreign open-source lib. Use C# 6.
    public static class Guard
    {
        public static void ArgNotNull(object arg, string name)
        {
            if (arg == null)
                throw new ArgumentNullException(name);
        }

        public static void ArgNotNone<T>(T arg, string name)
        {
            if (object.Equals(arg, null) || object.Equals(arg, default(T)))
                throw new ArgumentException($"The given '{name}' must not be not null or empty.");
        }

        //public static void ArgNotNullOrDefault(Guid arg, string name)
        //{
        //    if (arg == Guid.Empty) throw new ArgumentException("The given GUID must not be empty.", name);
        //}

        public static void ArgNotEmpty(string arg, string name)
        {
            ArgNotNullOrWhitespace(arg, name);
        }

        public static void ArgNotNullOrWhitespace(string arg, string name)
        {
            if (arg == null)
                throw new ArgumentNullException(name);

            if (string.IsNullOrWhiteSpace(arg))
                throw new ArgumentException($"The argument '{name}' must not be null, empty or whitespace.");
        }
    }
}