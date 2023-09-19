using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#nullable enable

namespace Casimodo.Lib
{
    public static class Guard
    {
        public static void ArgNotNull([NotNull] object? arg, [CallerArgumentExpression("arg")] string? name = null)
        {
            if (arg == null)
                throw new ArgumentNullException(name);
        }

        public static void ArgNotEmpty<T>([NotNull] T[]? arg, [CallerArgumentExpression("arg")] string? name = null)
        {
            ArgNotNull(arg, name);
            if (arg.Length == 0)
                throw new ArgumentException("The array must not be empty.", name);
        }

        public static void ArgNotEmpty([NotNull] DateTimeOffset? arg, [CallerArgumentExpression("arg")] string? name = null)
        {
            ArgNotNull(arg, name);
            if (arg == DateTimeOffset.MinValue || arg == DateTimeOffset.MaxValue)
                throw new ArgumentException("The date time offset must have a real value (not a min/max value).", name);
        }

        //public static void ArgNotEmpty([NotNull] object[]? arg, [CallerArgumentExpression("arg")] string? name = null)
        //{
        //    ArgNotNull(arg, name);
        //    if (arg.Length == 0)
        //        throw new ArgumentException("The array must not be empty.", name);
        //}

        public static void ArgNotEmpty([NotNull] string[]? arg, [CallerArgumentExpression("arg")] string? name = null)
        {
            ArgNotNull(arg, name);
            if (arg.Length == 0)
                throw new ArgumentException("The array must not be empty.", name);
        }

        public static void ArgNotNone<T>([NotNull] T? arg, [CallerArgumentExpression("arg")] string? name = null)
        {
            if (arg == null || object.Equals(arg, default(T)))
                throw new ArgumentException($"The given '{name}' must not be not null or empty.");
        }

        public static void ArgNotEmpty([NotNull] string? arg, [CallerArgumentExpression("arg")] string? name = null)
        {
            ArgNotNullOrWhitespace(arg, name);
        }

        public static void ArgNotEmpty([NotNull] Guid? arg, [CallerArgumentExpression("arg")] string? name = null)
        {
            if (arg == null || arg == Guid.Empty)
                throw new ArgumentException("The given GUID must not be null or empty.", name);
        }

        public static void ArgNotNullOrWhitespace([NotNull] string? arg, [CallerArgumentExpression("arg")] string? name = null)
        {
            if (arg == null)
                throw new ArgumentNullException(name);

            if (string.IsNullOrWhiteSpace(arg))
                throw new ArgumentException($"The argument '{name}' must not be null, empty or whitespace.");
        }

        public static void ArgOneNotNull(object arg1, object arg2,
            [CallerArgumentExpression("arg1")] string? name1 = null,
            [CallerArgumentExpression("arg2")] string? name2 = null)
        {
            if (arg1 == null && arg2 == null)
                throw new ArgumentException($"One of the arguments {name1} and {name2} must have a value.");
        }

        public static void ArgMutuallyExclusive(object arg1, object arg2,
            [CallerArgumentExpression("arg1")] string? name1 = null,
            [CallerArgumentExpression("arg2")] string? name2 = null)
        {
            if (arg1 != null && arg2 != null)
                throw new ArgumentException($"The arguments {name1} and {name2} are mutually exclusive.");
        }
    }
}