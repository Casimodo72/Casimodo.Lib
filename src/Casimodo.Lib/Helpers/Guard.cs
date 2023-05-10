using System;
using System.Diagnostics.CodeAnalysis;
#nullable enable

namespace Casimodo.Lib
{
    // KABU TODO: REMOVE: Use a dedicated foreign open-source lib. Use C# 6.
    public static class Guard
    {
        public static void ArgNotNull([NotNull] object? arg, string name)
        {
            if (arg == null)
                throw new ArgumentNullException(name);
        }

        public static void ArgNotEmpty([NotNull] byte[]? arg, string name)
        {
            ArgNotNull(arg, name);
            if (arg.Length == 0)
                throw new ArgumentException("The array must not be empty.", name);
        }

        public static void ArgNotEmpty([NotNull] object[]? arg, string name)
        {
            ArgNotNull(arg, name);
            if (arg.Length == 0)
                throw new ArgumentException("The array must not be empty.", name);
        }

        public static void ArgNotEmpty([NotNull] string[]? arg, string name)
        {
            ArgNotNull(arg, name);
            if (arg.Length == 0)
                throw new ArgumentException("The array must not be empty.", name);
        }

        public static void ArgNotNone<T>([NotNull] T? arg, string name)
        {
            if (arg == null || object.Equals(arg, default(T)))
                throw new ArgumentException($"The given '{name}' must not be not null or empty.");
        }

        public static void ArgNotEmpty([NotNull] string? arg, string name)
        {
            ArgNotNullOrWhitespace(arg, name);
        }

        public static void ArgNotEmpty([NotNull] Guid? arg, string name)
        {
            if (arg == null || arg == Guid.Empty)
                throw new ArgumentException("The given GUID must not be null or empty.", name);
        }

        public static void ArgNotNullOrWhitespace([NotNull] string? arg, string name)
        {
            if (arg == null)
                throw new ArgumentNullException(name);

            if (string.IsNullOrWhiteSpace(arg))
                throw new ArgumentException($"The argument '{name}' must not be null, empty or whitespace.");
        }

        public static void ArgOneNotNull(object arg1, object arg2, string name1, string name2)
        {
            if (arg1 == null && arg2 == null)
                throw new ArgumentException($"One of the arguments {name1} and {name2} must have a value.");
        }

        public static void ArgMutuallyExclusive(object arg1, object arg2, string name1, string name2)
        {
            if (arg1 != null && arg2 != null)
                throw new ArgumentException($"The arguments {name1} and {name2} are mutually exclusive.");
        }
    }
}