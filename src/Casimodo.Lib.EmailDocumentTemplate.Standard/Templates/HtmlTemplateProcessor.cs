using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Casimodo.Lib.SimpleParser;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Templates
{
    public class HtmlTemplateElement : TemplateElement
    {
        public AngleSharp.Dom.IElement Elem { get; set; }
        public AngleSharp.Dom.IAttr Attr { get; set; }

    };

    public class HtmlTemplate
    {
        public AngleSharp.Dom.IDocument Doc { get; set; }
        public List<HtmlInlineTemplate> InlineTemplates { get; set; } = new List<HtmlInlineTemplate>();
    }

    public static class DomExtensions
    {
        public static AngleSharp.Dom.IElement InsertElemAfter(
            this AngleSharp.Dom.IElement parentElement,
            AngleSharp.Dom.IElement newElement,
            AngleSharp.Dom.IElement refereceElement)
        {
            return (AngleSharp.Dom.IElement)parentElement.InsertBefore(newElement, refereceElement.NextSibling);
        }

        // Attributes.SetNamedItem(;
        public static AngleSharp.Dom.IAttr GetOrSetAttr(this AngleSharp.Dom.IElement elem, string name)
        {
            var attr = elem.Attributes.GetNamedItem(name); // CurElem.Attributes.fi.FirstOrDefault(x => x.Name == name);
            if (attr != null)
                return attr;

            attr = elem.Owner.CreateAttribute(name);
            attr.Value = "";
            return elem.Attributes.SetNamedItem(attr);
        }


        public static void RemoveAllChildren(this AngleSharp.Dom.IElement element)
        {
            while (element.FirstChild != null)
                element.RemoveChild(element.FirstChild);
        }
    }

    public class HtmlInlineTemplate
    {
        public string Name { get; set; }
        public AngleSharp.Dom.IElement Elem { get; set; }
    }
    public class TemplateProcessorEventArgs : EventArgs
    {
        public TemplateProcessor Processor { get; set; }
    }

    public delegate void TemplateProcessorEvent(object sender, TemplateProcessorEventArgs e);


    public abstract class HtmlTemplateProcessor : TemplateProcessor, ITemplateProcessor
    {
        public static class TemplateAttr
        {
            public const string Property = "data-property";
            public const string Placeholder = "data-placeholder";
            public const string Foreach = "data-foreach";
            public const string If = "data-if";
            public const string Template = "data-template";
            public const string IncludeTemplate = "data-include-template";
            public const string ValueTemplateName = "data-value-template";
        }

        IFileProvider _fileProvider;

        public HtmlTemplateProcessor(IFileProvider fileProvider)
        {
            Guard.ArgNotNull(fileProvider, nameof(fileProvider));

            _fileProvider = fileProvider;
        }

        protected List<HtmlTemplate> Fragments { get; set; } = new List<HtmlTemplate>();

        public RazorRemoverParser RazorRemover { get; set; } = new RazorRemoverParser();

        string ImagePageTemplate { get; set; }

        string PageTemplate { get; set; }
        HtmlTemplate Page { get; set; }

        public new HtmlTemplateElement CurTemplateElement
        {
            get { return (HtmlTemplateElement)base.CurTemplateElement; }
            set { base.CurTemplateElement = value; }
        }

        HtmlTemplate CurrentTemplate { get; set; }

        public void ProcessTemplate(HtmlTemplate template)
        {
            CurrentTemplate = template;
            ProcessTemplateElemCore(CurrentTemplate.Doc.DocumentElement, ExecuteCurrentTemplateElement);
        }

        List<AngleSharp.Dom.IElement> _processedNodes = new List<AngleSharp.Dom.IElement>();

        void ProcessTemplateElemCore(AngleSharp.Dom.IElement elem, Action visitor)
        {
            VisitTemplateElements(elem, () =>
            {
                if (_processedNodes.Any(x => x == CurTemplateElement.Elem))
                    throw new Exception("This node was already processed.");

                if (CurTemplateElement.IsForeach)
                {
                    var values = FindObjects(CurTemplateElement)
                        .Where(x => x != null)
                        .ToArray();

                    var originalElem = CurTemplateElement.Elem;

                    if (values.Length != 0)
                    {
                        var parentElem = originalElem.ParentElement;
                        var cursorNode = originalElem;
                        // TODO: IMPORTANT: We don't allow nested foreach instructions yet.

                        // TODO: IMPORTANT: If we allow nested foreach then:
                        // 1) make loop variable name adjustable by consumer.
                        // 2) store/restore loop variable of outer scope with same name.
                        //    This means an equal loop variable name will shadow variables in outer scope.

                        var itemVarName = "item";

                        for (int i = 0; i < values.Length; i++)
                        {
                            var value = values[i];

                            var item = (TemplateLoopCursor)Activator.CreateInstance(
                                typeof(TemplateLoopCursorVariable<>).MakeGenericType(new Type[] { value.GetType() }));

                            CoreContext.Data.AddProp(item.GetType(), itemVarName, item);

                            item.ValueObject = values[i];

                            item.Index = i;
                            item.IsOdd = i % 2 != 0;
                            item.IsFirst = i == 0;
                            item.IsLast = i == values.Length - 1;

                            // Operate on a clone of the original "foreach" template element.
                            var currentTemplateElem = (AngleSharp.Dom.IElement)originalElem.Clone();

                            ProcessTemplateElemCore(currentTemplateElem, visitor);

                            // Insert all children of the transformed "foreach" template element.
                            foreach (var child in currentTemplateElem.Children)
                            {
                                _processedNodes.Add(child);
                                parentElem.InsertElemAfter(child, cursorNode);
                                cursorNode = child;
                            }

                            CoreContext.Data.RemoveProp(itemVarName);
                        }
                    }

                    // Remove the original "foreach" template element and its content.
                    originalElem.Remove();
                }
                else if (CurTemplateElement.IsCondition)
                {
                    var originalElem = CurTemplateElement.Elem;
                    var parentElem = originalElem.ParentElement;
                    var cursorNode = originalElem;

                    if (EvaluateCondition(CurTemplateElement))
                    {
                        // Operate on a clone of the original "if" element.
                        var currentTemplateElem = (AngleSharp.Dom.IElement)originalElem.Clone();

                        ProcessTemplateElemCore(currentTemplateElem, visitor);

                        // Insert all children of the transformed "foreach" template element.
                        foreach (var child in currentTemplateElem.Children)
                        {
                            _processedNodes.Add(child);
                            parentElem.InsertElemAfter(child, cursorNode);
                            cursorNode = child;
                        }
                    }

                    // Remove the original "if" element and its content.
                    originalElem.Remove();
                }
                else if (CurTemplateElement.ValueTemplateName != null)
                {
                    var template = GetInlineTemplate(CurTemplateElement.ValueTemplateName);

                    var originalElem = CurTemplateElement.Elem;
                    var parentElem = originalElem.ParentElement;
                    var cursorNode = originalElem;
                    var valueVarName = "value";
                    var value = EvaluateValue(CurTemplateElement);

                    CoreContext.Data.AddProp(value?.GetType() ?? typeof(object), valueVarName, value);

                    // Operate on a clone of the original value template element.
                    var currentTemplateElem = (AngleSharp.Dom.IElement)template.Elem.Clone();

                    ProcessTemplateElemCore(currentTemplateElem, visitor);

                    // Insert all children of the transformed "foreach" template element.
                    foreach (var child in currentTemplateElem.Children)
                    {
                        _processedNodes.Add(child);
                        parentElem.InsertElemAfter(child, cursorNode);
                        cursorNode = child;
                    }

                    CoreContext.Data.RemoveProp(valueVarName);

                    // Remove the original element and its content.
                    originalElem.Remove();
                }
                else
                {
                    visitor();
                }
            });
        }

        HtmlInlineTemplate GetInlineTemplate(string name)
        {
            var template = CurrentTemplate.InlineTemplates.FirstOrDefault(x => x.Name == name);
            if (template == null)
                throw new TemplateException($"Inline template '{name}' not found.");

            return template;
        }

        public Func<string> CreatePageTemplate = () => "";

        public void SetImageSource(string value)
        {
            CurElem.SetAttribute("src", value);
        }

        static readonly string[] NewLineTokens = { "\n", Environment.NewLine };

        public override void SetText(string value)
        {
            CurElem.RemoveAllChildren();

            if (IsEmpty(value))
                return;

            if (!value.Contains(NewLineTokens))
                AppendTextNode(value);
            else
            {
                // Emit <br/> for each new line token.
                // Trim start because normally we'll usually have an initial unwanted new line
                // due to how users define the such property values in XML.
                var lines = value.TrimStart().Split(NewLineTokens, StringSplitOptions.None);

                for (int i = 0; i < lines.Length; i++)
                {
                    AppendTextNode(lines[i]);
                    if (i < lines.Length - 1)
                        Br();
                }
            }
        }

        public override void RemoveValue()
        {
            CurElem.Remove();
        }

        public Func<IEnumerable<string>> GetCssFilePaths = () => Enumerable.Empty<string>();

        public string StylesHtml { get; set; }

        public void ClearDocument()
        {
            Fragments.Clear();
        }

        string ReadFile(string filePath)
        {
            return FileHelper.ReadTextFile(GetPhysicalFilePath(filePath));
        }

        string GetPhysicalFilePath(string filePath)
        {
            return _fileProvider.GetFileInfo(filePath).PhysicalPath;
        }

        public virtual void BuildStylesheets()
        {
            // Stylesheets
            if (StylesHtml == null)
            {
                var sb = new StringBuilder();

                foreach (var styleFilePath in GetCssFilePaths())
                {
                    sb.Append("<style>");
                    sb.Append(ReadFile(styleFilePath));
                    sb.Append("</style>");
                }

                StylesHtml = sb.ToString();
            }
        }

        public virtual void Prepare()
        {
            BuildStylesheets();

            // Page template
            if (PageTemplate == null)
                PageTemplate = CreatePageTemplate();
        }

        protected string LoadTemplatePart(string virtualFilePath)
        {
            return RazorRemover.RemoveRazorSyntax(ReadFile(virtualFilePath));
        }

        /// <summary>
        /// Expects the main cshtml as first arguments. All other following are expected to be partial views
        /// which will be merged into the main cshtml.
        /// </summary>
        protected string MergeTemplateParts(params string[] partPaths)
        {
            if (partPaths == null || partPaths.Length == 0)
                return null;

            string template = "";
            foreach (var path in partPaths)
            {
                if (string.IsNullOrEmpty(template))
                    template = LoadTemplatePart(path);
                else
                    template = template.Replace($"@Html.Partial(\"{path}\")", LoadTemplatePart(path));
            }

            return string.IsNullOrWhiteSpace(template) ? null : template;
        }

        // TODO: Find a better name? This does not always correspond to a page (e.g. PDF page).
        public HtmlTemplate NewPage()
        {
            if (PageTemplate == null)
                PageTemplate = CreatePageTemplate();

            var doc = new HtmlParser().ParseDocument(PageTemplate);

            var fragment = new HtmlTemplate();
            fragment.Doc = doc;
            fragment.InlineTemplates = GetInlineTemplateHtmlElemNodes(doc.DocumentElement).ToList();

            ExpandInlineHtmlTemplates(fragment);

            Fragments.Add(fragment);

            Page = fragment;

            return fragment;
        }

        void ExpandInlineHtmlTemplates(HtmlTemplate fragment)
        {
            foreach (var node in GetHtmlElemNodes(fragment.Doc.DocumentElement)
                .Where(x => HasAttr(x, TemplateAttr.IncludeTemplate))
                .ToArray())
            {
                // Replace template includes with template content.

                var templateName = node.GetAttribute(TemplateAttr.IncludeTemplate, null);

                var template = fragment.InlineTemplates.FirstOrDefault(x => x.Name == templateName);
                if (template == null)
                    throw new TemplateException($"Inline template '{templateName}' not found.");

                var templateElem = (AngleSharp.Dom.IElement)template.Elem.Clone();
                var cursorNode = node;
                // Insert all children of the transformed "foreach" template element.
                foreach (var child in templateElem.Children)
                {
                    node.ParentElement.InsertElemAfter(child, cursorNode);
                    cursorNode = child;
                }

                // Remove "include template" instruction from tree.
                node.Remove();
            }
        }

        const string DefaultImagePageTemplate = @"<div class='image-page'><img class='page-image' alt='Image' data-property='Ext.PageImage' style='max-width:100%' /></div>";

        public HtmlTemplate NewImagePage()
        {
            if (ImagePageTemplate == null)
                ImagePageTemplate = DefaultImagePageTemplate;

            var doc = new HtmlParser().ParseDocument(ImagePageTemplate);

            var fragment = new HtmlTemplate();
            fragment.Doc = doc;

            Fragments.Add(fragment);
            Page = fragment;

            return fragment;
        }

        public string BuildFinalDocument()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.Append("<html>");
            sb.Append("<head>");
            sb.Append("<meta charset='UTF-8'>");

            // Stylesheets
            sb.Append(StylesHtml);

            sb.Append("</head>");
            sb.Append("<body>");

            int i = 0;
            foreach (var page in Fragments)
            {
                using (var writer = new System.IO.StringWriter())
                {
                    page.Doc.ToHtml(writer);
                    writer.Flush();

                    sb.Append(writer.ToString());
                }

                // Page break
                if (i < Fragments.Count - 1)
                    sb.Append(@"<div style=""page-break-before:always""></div>");
                i++;
            }

            sb.Append("</body>");
            sb.Append("</html>");

            return sb.ToString();
        }

        IEnumerable<HtmlInlineTemplate> GetInlineTemplateHtmlElemNodes(AngleSharp.Dom.IElement root)
        {
            // Return top level "data-template" element nodes.
            foreach (var node in root.Children.ToArray())
            {
                if (HasAttr(node, TemplateAttr.Template))
                {
                    node.Remove();
                    yield return new HtmlInlineTemplate
                    {
                        Name = Attr(node, TemplateAttr.Template),
                        Elem = node
                    };
                }
            }
        }

        protected IEnumerable<AngleSharp.Dom.IElement> GetHtmlElemNodes(AngleSharp.Dom.IElement parent)
        {
            if (parent.Owner == null)
                // Node might have already been removed by the transformation.
                yield break;

            foreach (var node in parent.Children.ToArray())
            {
                yield return node;

                // Skip content of template includes.
                if (HasAttr(node, TemplateAttr.IncludeTemplate))
                    continue;

                // Process children.
                foreach (var node2 in GetHtmlElemNodes(node))
                    yield return node2;
            }
        }

        bool HasAttr(AngleSharp.Dom.IElement node, string attrName)
        {
            return node.Attributes.FirstOrDefault(a => a.Name == attrName) != null;
        }

        string Attr(AngleSharp.Dom.IElement node, string attrName)
        {
            return node.GetAttribute(attrName, null);
        }

        protected IEnumerable<HtmlTemplateElement> GetTemplateElements(AngleSharp.Dom.IElement elem)
        {
            if (elem.Owner == null)
                // Node might have already been removed by the transformation.
                yield break;

            foreach (var node in elem.Children.ToArray())
            {
                if (node.Owner == null)
                    // Node might have already been removed by the transformation.
                    continue;

                var attr = node.Attributes.FirstOrDefault(a =>
                    a.Name == TemplateAttr.Property ||
                    a.Name == TemplateAttr.Foreach ||
                    a.Name == TemplateAttr.If);

                if (attr != null)
                    yield return CreateTemplateElement(node, attr);

                // Don't process the content of loop and conditional instructions.
                if (attr?.Name == TemplateAttr.Foreach || attr?.Name == TemplateAttr.If)
                    continue;

                // Process children.
                foreach (var node2 in GetTemplateElements(node))
                    yield return node2;
            }
        }

        HtmlTemplateElement CreateTemplateElement(AngleSharp.Dom.IElement node, AngleSharp.Dom.IAttr attr)
        {
            var elem = TemplateNodeFactory.Create<HtmlTemplateElement>(attr.Value);
            elem.Elem = CleanupAttributes(node);
            elem.Attr = attr;
            elem.IsForeach = attr.Name == TemplateAttr.Foreach;
            elem.IsCondition = attr.Name == TemplateAttr.If;
            elem.ValueTemplateName = Attr(node, TemplateAttr.ValueTemplateName);

            return elem;
        }

        protected AngleSharp.Dom.IElement CurElem
        {
            get { return ((HtmlTemplateElement)CurTemplateElement).Elem; }
        }

        protected void VisitTemplateElements(AngleSharp.Dom.IElement template, Action action)
        {
            foreach (HtmlTemplateElement item in GetTemplateElements(template))
            {
                if (item.Elem.Owner == null)
                    // Element might have already been removed by the transformation.
                    continue;

                CurTemplateElement = item;
                IsMatch = false;
                action();

                // Remove placeholder attribute.
                item.Elem.RemoveAttribute(item.Attr.NamespaceUri, item.Attr.LocalName);
            }
            CurTemplateElement = null;
        }

        void AppendTextNode(string value)
        {
            AppendNode(CreateTextNode(value));
        }

        AngleSharp.Dom.INode CreateTextNode(string value)
        {
            // KABU TODO: IMPORTANT: In HtmlAgilityPack we had to use HtmlEntity.Entitize
            //  Do we have to entitize in AngleSharp as well?
            return CurElem.Owner.CreateTextNode(value);
        }

        void Br()
        {
            AppendElem("br");
        }

        AngleSharp.Dom.IElement AppendElem(string name)
        {
            return CurElem.AppendElement(CurElem.Owner.CreateElement(name));
        }

        AngleSharp.Dom.INode AppendNode(AngleSharp.Dom.INode node)
        {
            return CurElem.AppendChild(node);
        }

        protected void CssIfSet(string name, object value)
        {
            Guard.ArgNotEmpty(name, nameof(name));
            var valueStr = (value != null ? value.ToString() : "").Trim();
            if (string.IsNullOrEmpty(valueStr))
                return;

            Css(name, valueStr);
        }

        static readonly char[] _semicolonSeparator = new[] { ';' };

        protected void Css(string name, object value)
        {
            Guard.ArgNotEmpty(name, nameof(name));

            var attr = CurElem.GetOrSetAttr("style");

            var valueStr = (value != null ? value.ToString() : "").Trim();
            bool remove = string.IsNullOrEmpty(valueStr);

            var items = (attr.Value ?? "").Trim()
                .Split(_semicolonSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':'))
                .Select(x => new
                {
                    Name = x[0].Trim(),
                    Value = x[1].Trim()
                })
                .ToList();

            var idx = items.FindIndex(x => x.Name == name);
            if (idx != -1)
            {
                items.RemoveAt(idx);
                if (!remove)
                {
                    items.Insert(idx, new
                    {
                        Name = name,
                        Value = valueStr
                    });
                }
            }
            else if (!remove)
            {
                items.Add(new
                {
                    Name = name,
                    Value = valueStr
                });
            }


            attr.Value = items.Select(x => $"{x.Name}:{x.Value}").Join(";");
        }

        protected AngleSharp.Dom.IElement E(string name, params object[] content)
        {
            var elem = Page.Doc.CreateElement(name);

            if (content != null)
            {
                foreach (var attr in content.OfType<AngleSharp.Dom.IAttr>())
                    elem.Attributes.SetNamedItem(attr);

                foreach (var obj in content)
                {
                    if (obj is AngleSharp.Dom.IAttr)
                        continue;

                    if (obj is string)
                        elem.AppendChild(CreateTextNode((string)obj));
                    else
                        elem.AppendChild((AngleSharp.Dom.IElement)obj);
                }
            }

            return elem;
        }

        protected AngleSharp.Dom.IAttr A(string name, string value)
        {
            var attr = Page.Doc.CreateAttribute(name);
            attr.Value = value;

            return attr;
        }

        protected string ShrinkDecimal(decimal? value)
        {
            if (value == null)
                return "";

            if ((value % 1) == 0)
                return string.Format("{0:0}", value);

            var text = value.ToString();
            var sep = System.Threading.Thread.CurrentThread.CurrentUICulture.NumberFormat.NumberDecimalSeparator[0];
            while (text[text.Length - 1] == '0' || text[text.Length - 1] == sep)
                text = text.Substring(0, text.Length - 1);

            return text;
        }

        protected AngleSharp.Dom.IElement CleanupAttributes(AngleSharp.Dom.IElement elem)
        {
            elem.RemoveAttribute("id");
            // KABU TODO: Remove all attributes starting with "ng-" and "data-" (except for template attributes).
            elem.RemoveAttribute("ng-bind");
            elem.RemoveAttribute("ng-hide");
            elem.RemoveAttribute("data-ng-src");

            return elem;
        }

        protected AngleSharp.Dom.IElement GetStylesheet(string path)
        {
            string content = ReadFile(path);

            return E("style", content);
        }

        public class RazorRemoverParser : SimpleStringParser
        {
            public string RemoveRazorSyntax(string html)
            {
                Initialize(html);

                while (!IsEnd)
                {
                    // Remove Razor comments.
                    if (Is("@*"))
                    {
                        Skip(2);
                        while (!IsEnd && !Is("*@")) Skip();
                        Skip(2);
                    }
                    else
                        Consume();
                }

                return TextBuilder.ToString();
            }
        }
    }
}
