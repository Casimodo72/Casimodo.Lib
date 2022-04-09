#nullable enable

namespace Casimodo.Mojen
{
    public class ViewTemplateItem
    {
        public static readonly ViewTemplateItem None = new();

        ViewTemplateItem()
        {
            Directive = "";
            Parent = this;
        }

        public ViewTemplateItem(string directive, ViewTemplateItem parent)
        {
            Directive = directive ?? throw new ArgumentNullException(nameof(directive));
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public string Directive { get; }

        public ViewTemplateItem Parent { get; }
        public ViewTemplateItem? Child;
        public ViewTemplateItem? Prev;
        public ViewTemplateItem? Next;

        public string? Name;
        public string? TextValue;
        public MojViewProp? Prop;
        public MojViewMode HideModes = MojViewMode.None;
        public int Index = -1;
        public MojColumnDefinition? Style;
        public bool IsRunStart;
        public bool IsRunEnd;
        public bool IsContainer;
        public bool IsContainerEnd;
        public object? GroupObj;
        public object? VisibilityCondition;
        public string? _tag;

        public override string ToString()
        {
            return $"{Directive} {Prop?.Name}";
        }

        public void Remove()
        {
            // TODO: Adjust runstart, etc.
            if (Prev != null)
                Prev.Next = Next;

            if (Next != null)
                Next.Prev = Prev;

            if (Parent != None && Parent.Child == this)
                Parent.Child = Prev ?? Next;
        }

        public IEnumerable<ViewTemplateItem> GetChildren()
        {
            var cur = Child;
            while (cur != null)
            {
                yield return cur;

                cur = cur.Next;
            }
        }

        public IEnumerable<List<ViewTemplateItem>> Groups()
        {
            var cur = this;
            var group = new List<ViewTemplateItem>();

            while (cur != null)
            {
                group.Add(cur);

                if (cur.IsContainerEnd)
                {
                    yield return group;

                    group.Clear();

                    if (cur.IsContainer)
                        group.Add(cur);
                }

                cur = cur.Next;
            }

            if (group.Any())
                yield return group;
        }

        public IEnumerable<List<ViewTemplateItem>> GetRuns(Func<ViewTemplateItem, bool>? predicate = null)
        {
            var cur = this;
            var runs = new List<ViewTemplateItem>();

            while (cur != null)
            {
                runs.Add(cur);

                if (cur.IsRunEnd)
                {
                    yield return runs;

                    runs = new List<ViewTemplateItem>();
                }

                cur = cur.Next;
            }

            if (runs.Any())
                yield return runs;
        }

        public IEnumerable<ViewTemplateItem> GetRunsOf(Func<ViewTemplateItem, bool>? predicate = null)
        {
            var cur = this;

            // Find start.
            while (cur != null && !cur.IsRunStart)
                cur = cur.Prev;

            while (cur != null && !cur.IsRunEnd)
            {
                if (predicate == null || predicate(cur))
                    yield return cur;

                cur = cur.Next;
            }
        }

        public IEnumerable<ViewTemplateItem> GetRunRangeBefore(Func<ViewTemplateItem, bool>? predicate = null)
        {
            var cur = this;

            // Find start.
            while (cur != null && !cur.IsRunStart)
                cur = cur.Prev;

            while (cur != null && !cur.IsRunEnd)
            {
                if (cur == this)
                    yield break;

                if (predicate == null || predicate(cur))
                    yield return cur;

                cur = cur.Next;
            }
        }
    }
}
