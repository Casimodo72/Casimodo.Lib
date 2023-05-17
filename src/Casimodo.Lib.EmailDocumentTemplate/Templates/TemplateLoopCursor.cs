#nullable enable

namespace Casimodo.Lib.Templates
{
    public class TemplateLoopCursor
    {
        public object? ValueObject { get; set; }
        public int Index { get; set; }
        public int Position { get; set; }
        public bool IsLast { get; set; }
        public bool IsFirst { get; set; }
        public bool IsOdd { get; set; }
        public int Count { get; set; }
    }
}
