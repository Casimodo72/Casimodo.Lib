using System;

namespace Casimodo.Lib.Templates
{
    [Serializable]
    public class TemplateProcessorException : Exception
    {
        public TemplateProcessorException() { }
        public TemplateProcessorException(string message) : base(message) { }
        public TemplateProcessorException(string message, Exception inner) : base(message, inner) { }
    }
}
