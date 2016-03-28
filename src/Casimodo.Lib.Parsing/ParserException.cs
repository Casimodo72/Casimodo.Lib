using System;

namespace Casimodo.Lib.Parser
{
    public class ParserException : Exception
    {
        public ParserException(string message)
            : base(message)
        { }
    }
}