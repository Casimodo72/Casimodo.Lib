namespace Casimodo.Mojen
{
    public class MojenException : Exception
    {
        public MojenException(string message)
            : base(message)
        { }

        public MojenException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}