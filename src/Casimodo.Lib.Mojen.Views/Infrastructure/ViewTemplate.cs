using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    // See http://wiki.selfhtml.org/wiki/HTML/Textstrukturierung
    public class ViewTemplate
    {
        public readonly ViewTemplateItem Root = new ViewTemplateItem { Directive = "root", IsContainer = true };

        public ViewTemplate()
        {
            Cur = Root;
        }

        public ViewTemplateItem Cur { get; private set; }

        ViewTemplateItem RunStart;

        public MojViewBuilder ViewBuilder { get; set; }

        public MojViewConfig View
        {
            get { return ViewBuilder.View; }
        }

        public bool IsEmpty
        {
            get { return Root.Child == null; }
        }

        ViewTemplateItem Add(string directive, MojViewProp prop = null, bool child = false)
        {
            var item = new ViewTemplateItem();
            item.Directive = directive;
            if (prop != null)
                item.Prop = prop;

            if (Cur.IsContainer && !Cur.IsContainerEnd)
            {
                if (Cur.Child != null) throw new MojenException("The current layout node has already a child node.");
                Cur.Child = item;
                item.Parent = Cur;
            }
            else
            {
                if (false && child)
                {
                    if (Cur.Child != null) throw new MojenException("The current layout node has already a child node.");
                    Cur.Child = item;
                    item.Parent = Cur;
                }
                else
                {
                    if (Cur.Next != null) throw new MojenException("The current layout node has already a next node.");
                    Cur.Next = item;
                    item.Prev = Cur;
                    item.Parent = Cur.Parent;
                }
            }

            Cur = item;

            return item;
        }

        public ViewTemplate GroupBox(string header = null)
        {
            AnyGroup("group-box");
            return this;
        }

        public ViewTemplateItem AnyGroup(string directive, object group = null, string name = null,
            string header = null,
            int index = -1,
            MojColumnDefinition col = null)
        {
            EndRun();
            Add(directive, child: true);
            Cur.IsContainer = true;
            Cur.Name = name;
            Cur.GroupObj = group;
            Cur.TextValue = header;
            Cur.Index = index;

            if (col != null) col.Build();
            Cur.Style = col;
            EndRun();

            return Cur;
        }

        public ViewTemplate EndGroup(ViewTemplateItem item = null)
        {
            EndRun();

            if (Cur == item || (Cur.IsContainer && Cur.Child == null))
            {
                // This is the case when a container has no child nodes.
            }
            else
                Cur = Cur.Parent;

            if (item != null && Cur != item)
                throw new MojenException($"ViewTemplate: Tree error: wrong container.");

            Cur.IsContainerEnd = true;

            return this;
        }

        public ViewTemplate Hr()
        {
            Add("hr");
            StartRun();
            EndRun();
            return this;
        }

        public ViewTemplate CustomView(string name, MojViewMode showOn = MojViewMode.All)
        {
            Add("custom-view");
            Cur.Name = name;
            Cur.HideModes = MojViewMode.All & ~showOn;
            StartRun();
            EndRun();
            return this;
        }

        public ViewTemplate Br()
        {
            Add("br");
            EndRun();
            return this;
        }

        public ViewTemplate Label(string label = null)
        {
            Add("label");
            Cur.TextValue = label;
            StartRun();
            return this;
        }

        public ViewTemplate Label(MojViewProp prop)
        {
            Add("label");
            Cur.Prop = prop;
            StartRun();
            return this;
        }

        public ViewTemplate oO()
        {
            EndRun();
            return this;
        }

        internal ViewTemplate o(MojViewProp prop)
        {
            Add("append", prop);
            StartRun();
            return this;
        }

        public ViewTemplate o(MojProp prop)
        {
            return oCore(prop);
        }

        public ViewTemplate o(MojProp prop, bool readOnly)
        {
            return oCore(prop, readOnly: readOnly);
        }

        public ViewTemplate o(MojProp prop, Action<MojViewPropBuilder> build = null)
        {
            return oCore(prop, build: build);
        }

        ViewTemplate oCore(MojProp prop, bool readOnly = false, Action<MojViewPropBuilder> build = null)
        {
            var builder = BuildViewProp(prop, readOnly: readOnly);
            Add("append", builder.Prop);
            StartRun();

            build?.Invoke(builder);

            return this;
        }

        public ViewTemplate o(string text)
        {
            Add("append").TextValue = text;
            return this;
        }

        MojViewPropBuilder BuildViewProp(MojProp prop, bool readOnly = false)
        {
            if (prop.GetType() != typeof(MojProp))
                throw new ArgumentException($"A prop of type {nameof(MojProp)} was expected.", nameof(prop));

            return ViewBuilder.SimplePropCore(prop, readOnly: readOnly);
        }

        void StartRun()
        {
            if (RunStart == null)
            {
                RunStart = Cur;
                Cur.IsRunStart = true;
            }
        }

        public void EndRun()
        {
            Cur.IsRunEnd = true;
            RunStart = null;
        }

        public override string ToString()
        {
            return ToDebugString();
        }

        public string ToDebugString()
        {
            return ToDebug(Root).First().ToString();
        }

        IEnumerable<XElement> ToDebug(ViewTemplateItem cur)
        {
            while (cur != null)
            {
                if (cur.Child != null)
                    yield return new XElement(cur.Directive, ToDebug(cur.Child));
                else if (cur.Prop != null)
                    yield return new XElement(cur.Directive, new XAttribute("prop", cur.Prop.FormedTargetPath));
                else
                    yield return new XElement(cur.Directive);

                cur = cur.Next;
            }
        }
    }
}