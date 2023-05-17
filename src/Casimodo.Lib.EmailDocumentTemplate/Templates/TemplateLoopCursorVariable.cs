#nullable enable

namespace Casimodo.Lib.Templates
{
    public class TemplateLoopCursorVariable<T> : TemplateLoopCursor
      where T : class
    {
        public T Value { get { return (T)ValueObject; } }
    }
}
