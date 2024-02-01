namespace Casimodo.Lib.Data
{
    public class DbException : Exception
    {
        public DbException() { }
        public DbException(string message) : base(message) { }
        public DbException(string message, Exception inner) : base(message, inner) { }
    }

    public class EntityNotFoundException : DbException
    {
        public EntityNotFoundException() { }
        public EntityNotFoundException(string message) : base(message) { }
    }
}
