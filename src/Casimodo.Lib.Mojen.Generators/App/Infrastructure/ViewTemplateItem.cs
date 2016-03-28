using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public class ViewTemplateItem
    {
        public ViewTemplateItem Prev;
        public ViewTemplateItem Next;
        public ViewTemplateItem Parent;
        public ViewTemplateItem Child;
        public string Directive;
        public string Name;
        public string TextValue;
        public MojViewProp Prop;
        public int Index = -1;
        public MojColumnDefinition Style;
        public bool IsRunStart;
        public bool IsRunEnd;
        public bool IsContainer;
        public bool IsContainerEnd;
        public object GroupObj;
        public object VisibilityPredicate;

        public override string ToString()
        {
            return $"{Directive ?? "(null)"} {Prop?.Name}";
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

        public IEnumerable<List<ViewTemplateItem>> Groups() // Func<FormatTemplateItem, bool> predicate = null)
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

        public IEnumerable<List<ViewTemplateItem>> GetRuns(Func<ViewTemplateItem, bool> predicate = null)
        {
            var cur = this;

            var runs = new List<ViewTemplateItem>();
            while (cur != null)
            {
                //if (cur.IsContainer || cur.IsContainerEnd)
                //    yield break;

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

        public IEnumerable<ViewTemplateItem> GetRunsOf(Func<ViewTemplateItem, bool> predicate = null)
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

        public IEnumerable<ViewTemplateItem> GetRunRangeBefore(Func<ViewTemplateItem, bool> predicate = null)
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
