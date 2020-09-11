using System;

namespace Casimodo.Lib.Data
{
    public enum DbRepositoryErrorKind
    {
        Error = 0,
        NotFound = 1 << 0,
        InvalidOperation = 1 << 1
    }

    [Serializable]
    public class DbRepositoryException : Exception
    {
        public DbRepositoryException() { }

        public DbRepositoryException(DbRepositoryErrorKind kind, string message)
            : base(message)
        {
            ErrorKind = kind;
        }

        public DbRepositoryException(string message) : base(message) { }

        public DbRepositoryException(string message, Exception inner) : base(message, inner) { }

        public DbRepositoryException(DbRepositoryErrorKind kind, string message, Exception inner)
            : base(message, inner)
        {
            ErrorKind = kind;
        }

        public DbRepositoryErrorKind ErrorKind { get; set; } = DbRepositoryErrorKind.Error;
    }
}
