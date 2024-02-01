using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
#nullable enable

namespace Casimodo.Lib.Templates
{
    public class XmlTemplateElement : TemplateElement
    {
        public XElement Elem { get; set; } = default!;
    };

    public abstract class XmlTemplateProcessor : TemplateProcessor, ITemplateProcessor
    {
        protected XmlTemplateProcessor(TemplateContext context)
            : base(context)
        { }

        public override void SetText(string? value)
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
            CurrentTemplateElement = null;
        }

        protected static List<XmlTemplateElement> GetTemplateElements(XElement template)
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

        public async Task ProcessTemplate(XElement template)
        {
            await ProcessTemplateElements(template, ExecuteCurrentTemplateElement);
        }

        protected async Task ProcessTemplateElements(XElement template, Func<Task> action)
        {
            var elements = GetTemplateElements(template);
            foreach (XmlTemplateElement item in elements)
            {
                CurrentTemplateElement = item;
                IsMatch = false;
                await action();

                // Remove placeholder element.
                item.Elem.Remove();
            }
            CurrentTemplateElement = null;
        }

        protected XElement CurElem
        {
            get
            {
                if (CurrentTemplateElement == null)
                    throw new Exception("Current template element is not assigned.");

                return ((XmlTemplateElement)CurrentTemplateElement).Elem;
            }
        }
    }
}
