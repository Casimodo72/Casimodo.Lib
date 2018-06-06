using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Casimodo.Lib.Templates
{
    public class XmlTemplateElement : TemplateElement
    {
        public XElement Elem { get; set; }
    };

    public abstract class XmlTemplateProcessor : TemplateProcessor, ITemplateProcessor
    {
        public XmlTemplateProcessor()
        { }

        public override void SetText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            CurElem.AddAfterSelf(new XText(value.Trim()));
        }

        public void SetImageSource(string value)
        {
            throw new NotSupportedException();
        }

        public override void SetImage(Guid? imageFileId, bool removeIfEmpty = false)
        {
            throw new NotSupportedException();
        }

        public override void RemoveValue()
        {
            // NOP. The element will be removed anyway.
        }

        public void Clear()
        {
            CurTemplateElement = null;
        }

        protected List<XmlTemplateElement> GetTemplateElements(XElement template)
        {
            var items = new List<XmlTemplateElement>();

            foreach (var elem in template.Descendants("value"))
            {
                var telem = TemplateNodeFactory.Create<XmlTemplateElement>((string)elem.Attr("use"));
                telem.Elem = elem;

                items.Add(telem);
            }

            return items;
        }

        protected void ProcessTemplateElements(XElement template, Action action)
        {
            var elements = GetTemplateElements(template);
            foreach (XmlTemplateElement item in elements)
            {
                CurTemplateElement = item;
                IsMatch = false;
                action();

                // Remove placeholder element.
                item.Elem.Remove();
            }
            CurTemplateElement = null;
        }

        protected XElement CurElem
        {
            get { return ((XmlTemplateElement)CurTemplateElement).Elem; }
        }
    }
}
