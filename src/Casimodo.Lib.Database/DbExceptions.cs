using System;

namespace Casimodo.Lib.Data
{

    [Serializable]
    public class DbException : Exception
    {
        public DbException() { }
        public DbException(string message) : base(message) { }
        public DbException(string message, Exception inner) : base(message, inner) { }
        protected DbException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class EntityNotFoundException : DbException
    {
        public EntityNotFoundException() { }
        public EntityNotFoundException(string message) : base(message) { }
        public EntityNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected EntityNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
