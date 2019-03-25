using System;

namespace Casimodo.Lib.Templates
{
    [Serializable]
    public class TemplateException : Exception
    {
        public TemplateException() { }
        public TemplateException(string message) : base(message) { }
        public TemplateException(string message, Exception inner) : base(message, inner) { }
    }
}
