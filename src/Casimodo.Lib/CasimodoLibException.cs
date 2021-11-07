using System;

namespace Casimodo.Lib
{
    [Serializable]
    public class CasimodoLibException : Exception
    {
        public CasimodoLibException() { }
        public CasimodoLibException(string message) : base(message) { }
        public CasimodoLibException(string message, Exception inner) : base(message, inner) { }

        protected CasimodoLibException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
