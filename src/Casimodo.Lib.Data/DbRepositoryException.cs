using System;

namespace Casimodo.Lib.Data
{
    [Serializable]
    public class DbRepositoryException : Exception
    {
        public DbRepositoryException() { }

        public DbRepositoryException(string message) : base(message) { }

        public DbRepositoryException(string message, Exception inner) : base(message, inner) { }
    }
}
