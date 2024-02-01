#nullable enable

namespace Casimodo.Lib.Templates
{
    public class TemplateLoopCursorVariable<T> : TemplateLoopCursor
      where T : class
    {
        public T Value
        {
            get
            {
                if (ValueObject == null)
                    throw new TemplateException("Value object is not assigned.");

                return (T)ValueObject;
            }
        }
    }
}
