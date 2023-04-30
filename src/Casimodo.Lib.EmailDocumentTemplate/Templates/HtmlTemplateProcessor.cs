﻿using AngleSharp;
using AngleSharp.Dom;
using Casimodo.Lib.SimpleParser;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Templates
{
    public class HtmlTemplateElement : TemplateElement
    {
        public IElement Elem { get; set; }
        public IAttr Attr { get; set; }
    };

    public class HtmlTemplate
    {
        public HtmlTemplate(IDocument doc)
        {
            Guard.ArgNotNull(doc, nameof(doc));

            Doc = doc;
        }

        public IDocument Doc { get; set; }
        public List<HtmlInlineTemplate> InlineTemplates { get; set; } = new List<HtmlInlineTemplate>();

        public IEnumerable<IElement> Elements
        {
            get { return Doc?.Body?.Children ?? Enumerable.Empty<IElement>(); }
        }
    }

    public static class DomExtensions
    {
        public static IElement InsertElemAfter(
            this IElement parentElement,
            IElement newElement,
            IElement refereceElement)
        {
            return (IElement)parentElement.InsertBefore(newElement, refereceElement.NextSibling);
        }

        // Attributes.SetNamedItem(;
        public static IAttr GetOrSetAttr(this IElement elem, string name)
        {
            var attr = elem.Attributes.GetNamedItem(name);
            if (attr != null)
                return attr;

            attr = elem.Owner.CreateAttribute(name);
            attr.Value = "";
            return elem.Attributes.SetNamedItem(attr);
        }

        public static void RemoveAllChildren(this IElement element)
        {
            while (element.FirstChild != null)
                element.RemoveChild(element.FirstChild);
        }
    }

    public class HtmlInlineTemplate
    {
        public string Id { get; set; }
        public AngleSharp.Html.Dom.IHtmlTemplateElement TemplateElement { get; set; }
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
            // public const string Placeholder = "data-placeholder";
            public const string Foreach = "data-foreach";
            public const string If = "data-if";
            public const string TemplateRef = "template-id";
            public const string ValueTemplateRef = "value-template-id";
        }

        readonly IFileProvider _fileProvider;

        public HtmlTemplateProcessor(IFileProvider fileProvider)
        {
            Guard.ArgNotNull(fileProvider, nameof(fileProvider));

            _fileProvider = fileProvider;
        }

        protected List<HtmlTemplate> Pages { get; set; } = new List<HtmlTemplate>();

        public RazorRemoverParser RazorRemover { get; set; } = new RazorRemoverParser();

        string ImagePageTemplateHtml { get; set; }

        string PageTemplateHtml { get; set; }

        protected HtmlTemplate CurrentTemplate { get; set; }

        public new HtmlTemplateElement CurTemplateElement
        {
            get => (HtmlTemplateElement)base.CurTemplateElement;
            set => base.CurTemplateElement = value;
        }

        public async Task ProcessNewPage()
        {
            ClearProcessedElements();
            CurrentTemplate = AddPage(await NewPage());
            await ProcessTemplateElements(CurrentTemplate.Elements, ExecuteCurrentTemplateElement);
        }

        public async Task AddAndProcessPage(HtmlTemplate page)
        {
            ClearProcessedElements();
            CurrentTemplate = AddPage(page);
            await ProcessTemplateElements(CurrentTemplate.Elements, ExecuteCurrentTemplateElement);
        }

        readonly List<IElement> _processedElements = new();

        void ClearProcessedElements()
        {
            _processedElements.Clear();
        }

        async Task ProcessTemplateElements(IEnumerable<IElement> elements, Func<Task> visitor)
        {
            await WalkTemplateElements(elements, async (current) =>
            {
                if (current.IsForeach)
                {
                    var values = (await FindObjects(current))
                        .Where(x => x != null)
                        .ToArray();

                    var originalElem = current.Elem;

                    if (values.Length != 0)
                    {
                        var parentNode = originalElem.Parent;

                        // TODO: IMPORTANT: We don't support nested foreach instructions yet.

                        // TODO: IMPORTANT: If we allow nested foreach then:
                        // 1) make loop variable name adjustable by consumer.
                        // 2) store/restore loop variable of outer scope with same name.
                        //    This means an equal loop variable name will shadow variables in outer scope.

                        const string loopCurrenItemPropName = "item";

                        for (int i = 0; i < values.Length; i++)
                        {
                            var value = values[i];

                            var item = (TemplateLoopCursor)Activator.CreateInstance(
                                typeof(TemplateLoopCursorVariable<>).MakeGenericType(new Type[] { value.GetType() }));

                            // Add "item" property.
                            CoreContext.Data.AddProp(item.GetType(), loopCurrenItemPropName, item);

                            item.ValueObject = values[i];

                            item.Index = i;
                            item.IsOdd = i % 2 != 0;
                            item.IsFirst = i == 0;
                            item.IsLast = i == values.Length - 1;

                            // Operate on a clone of the original "foreach" element.
                            var elemClone = (IElement)originalElem.Clone();

                            await ProcessTemplateElements(elemClone.Children, visitor);

                            // Insert all child nodes of the transformed "foreach" element.
                            foreach (var childNode in elemClone.ChildNodes.ToArray())
                                parentNode.InsertBefore(childNode, originalElem);

                            // Remove "item" property.
                            CoreContext.Data.RemoveProp(loopCurrenItemPropName);
                        }
                    }

                    // Remove the original "foreach" template element and its content.
                    originalElem.Remove();

                    // Skip content because we already processed the content.
                    return false;
                }
                else if (current.IsCondition)
                {
                    var originalElem = current.Elem;
                    var parentNode = originalElem.Parent;

                    if (await EvaluateCondition(current))
                    {
                        // Operate on a clone of the original "if" element.
                        var elemClone = (IElement)originalElem.Clone();

                        await ProcessTemplateElements(elemClone.Children, visitor);

                        // Insert all child nodes of the transformed "if" element.
                        foreach (var childNode in elemClone.ChildNodes.ToArray())
                            parentNode.InsertBefore(childNode, originalElem);
                    }

                    // Remove the original "if" element and its content.
                    originalElem.Remove();

                    // Skip content because we already processed the content.
                    return false;
                }
                else if (current.ValueTemplateName != null)
                {
                    var originalElem = current.Elem;
                    var valueTemplate = GetInlineTemplate(current.ValueTemplateName);

                    // Get the value that will be used by the value template.
                    var value = await EvaluateValue(current);

                    // Add "value" property.
                    const string valueVarName = "value";
                    CoreContext.Data.AddProp(value?.GetType() ?? typeof(object), "value", value);

                    var fragmentClone = (IDocumentFragment)valueTemplate.TemplateElement.Content.Clone();

                    // Process the content of the value template.
                    await ProcessTemplateElements(fragmentClone.Children, visitor);

                    // Add processed content of the value template to the result.
                    originalElem.ParentElement.InsertBefore(fragmentClone, originalElem);

                    // Remove the "value" property.
                    CoreContext.Data.RemoveProp(valueVarName);

                    // Remove the original element and its content.
                    originalElem.Remove();

                    // Skip content because we already processed the content.
                    return false;
                }
                else
                {
                    await visitor();
                    return true;
                }
            });
        }

        HtmlInlineTemplate GetInlineTemplate(string name)
        {
            var template = CurrentTemplate.InlineTemplates.FirstOrDefault(x => x.Id == name);
            if (template == null)
                throw new TemplateException($"Inline template '{name}' not found.");

            return template;
        }

        public Func<Task<string>> CreatePageTemplate = () => Task.FromResult("");

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

            if (!value.ContainsAny(NewLineTokens))
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

        public Func<IEnumerable<string>> GetCustomCss = () => Enumerable.Empty<string>();


        public string StylesHtml { get; set; }

        public void ClearDocument()
        {
            Pages.Clear();
        }

        async Task<string> ReadFileAsync(string filePath)
        {
            return await FileHelper.ReadTextFileAsync(GetPhysicalFilePath(filePath));
        }

        string GetPhysicalFilePath(string filePath)
        {
            return _fileProvider.GetFileInfo(filePath).PhysicalPath;
        }

        public virtual async Task BuildStylesheets()
        {
            // Stylesheets
            if (StylesHtml == null)
            {
                var sb = new StringBuilder();

                var cssFilePaths = GetCssFilePaths().ToArray();
                var customCss = GetCustomCss().ToArray();
                if (cssFilePaths.Any() || customCss.Any())
                {
                    sb.Append("<style>");

                    foreach (var styleFilePath in GetCssFilePaths())
                        sb.Append(await ReadFileAsync(styleFilePath));

                    foreach (var css in customCss)
                        sb.Append(css);

                    sb.Append("</style>");
                }

                StylesHtml = sb.ToString();
            }
        }

        public virtual async Task Prepare()
        {
            await BuildStylesheets();

            // Page template
            PageTemplateHtml ??= await CreatePageTemplate();
        }

        protected async Task<string> LoadTemplatePart(string virtualFilePath)
        {
            return RazorRemover.RemoveRazorSyntax(await ReadFileAsync(virtualFilePath));
        }

        /// <summary>
        /// Expects the main cshtml as first arguments. All other following are expected to be partial views
        /// which will be merged into the main cshtml.
        /// </summary>
        protected async Task<string> MergeTemplateParts(params string[] partPaths)
        {
            if (partPaths == null || partPaths.Length == 0)
                return null;

            string template = "";
            foreach (var path in partPaths)
            {
                if (string.IsNullOrEmpty(template))
                    template = await LoadTemplatePart(path);
                else // TODO: ELIMINATE: We don't use pre ASP Core Razor include syntax anymore.
                    template = template.Replace($"@Html.Partial(\"{path}\")", await LoadTemplatePart(path));
            }

            return string.IsNullOrWhiteSpace(template) ? null : template;
        }

        protected async Task<HtmlTemplate> CreateHtmlTemplate(string htmlFragment)
        {
            var template = await ParseHtmlTemplate(htmlFragment);

            RemoveWhitespace(template.Doc.Body.GetDescendants());

            template.InlineTemplates = BuildTopLevelInlineTemplates(template.Elements).ToList();
            Expand(template);

            return template;
        }

        static async Task<HtmlTemplate> ParseHtmlTemplate(string htmlFragment)
        {
            var document = await BrowsingContext.New().OpenNewAsync();
            document.Body.InnerHtml = htmlFragment;

            return new HtmlTemplate(document);
        }

        // TODO: Find a better name? This does not always correspond to a page (e.g. PDF page).
        public async Task<HtmlTemplate> NewPage()
        {
            if (PageTemplateHtml == null)
                throw new InvalidOperationException("PageTemplate not assigned.");

            return await CreateHtmlTemplate(PageTemplateHtml);
        }

        static void RemoveWhitespace(IEnumerable<INode> nodes)
        {
            foreach (INode node in nodes.Where(x => x.NodeType == NodeType.Text).ToArray())
            {
                if (string.IsNullOrWhiteSpace(node.TextContent))
                    node.Parent?.RemoveChild(node);
            }
        }

        void Expand(HtmlTemplate template)
        {
            foreach (var elem in GetHtmlElements(template.Elements, (x) => HasAttr(x, TemplateAttr.TemplateRef))
                .ToArray())
            {
                // Replace template includes with template content.
                var inlineTemplateId = elem.GetAttribute(TemplateAttr.TemplateRef);
                var inlineTemplate = template.InlineTemplates.FirstOrDefault(x => x.Id == inlineTemplateId);
                if (inlineTemplate == null)
                    throw new TemplateException($"Inline template '{inlineTemplateId}' not found.");

                // Clone the document fragment content of the template element.
                var clone = (IDocumentFragment)inlineTemplate.TemplateElement.Content.Clone();
                // Insert the fragment's child nodes.
                elem.ParentElement.InsertBefore(clone, elem);

                // Remove "template" include instruction element.
                elem.Remove();
            }
        }

        protected IEnumerable<IElement> GetHtmlElements(
            IEnumerable<IElement> elements,
            Func<IElement, bool> predicate = null)
        {
            foreach (var elem in elements.ToArray())
            {
                if (predicate?.Invoke(elem) != false)
                    yield return elem;

                // Skip content of template includes.
                if (HasAttr(elem, TemplateAttr.TemplateRef) || HasAttr(elem, TemplateAttr.ValueTemplateRef))
                    continue;

                // Process child elements.
                foreach (var childElem in GetHtmlElements(elem.Children, predicate))
                    yield return childElem;
            }
        }

        const string DefaultImagePageTemplate = @"<div class='image-page'><img class='page-image' alt='Image' data-property='Ext.PageImage' style='max-width:100%' /></div>";

        protected async Task<HtmlTemplate> NewImagePage()
        {
            ImagePageTemplateHtml ??= DefaultImagePageTemplate;

            // TODO: Eval if we should cache the template DOM.
            return await ParseHtmlTemplate(ImagePageTemplateHtml);
        }

        protected HtmlTemplate AddPage(HtmlTemplate pageTemplate)
        {
            Pages.Add(pageTemplate);

            return pageTemplate;
        }

        public string BuildFinalDocument()
        {
            var sb = new StringBuilder();
            using (var wr = new System.IO.StringWriter(sb))
            {
                wr.WriteLine("<!DOCTYPE html>");
                wr.Write("<html>");
                wr.Write("<head>");
                wr.Write("<meta charset='UTF-8'>");

                // Stylesheets
                wr.Write(StylesHtml);

                wr.Write("</head>");
                wr.Write("<body>");

                int i = 0;
                foreach (var page in Pages)
                {
                    page.Doc.Body.ChildNodes.ToHtml(wr);

                    // Page break
                    if (i < Pages.Count - 1)
                        wr.Write(@"<div style=""page-break-before:always""></div>");
                    i++;
                }

                wr.Write("</body>");
                wr.Write("</html>");

                wr.Flush();
            }

            return sb.ToString();
        }

        static IEnumerable<HtmlInlineTemplate> BuildTopLevelInlineTemplates(IEnumerable<IElement> elements)
        {
            // Return top level template elements.
            var items = elements
                .OfType<AngleSharp.Html.Dom.IHtmlTemplateElement>()
                .Select(elem => new HtmlInlineTemplate
                {
                    Id = elem.GetAttribute("id"),
                    TemplateElement = elem
                })
                .ToArray();

            foreach (var item in items)
            {
                // Remove from tree.
                item.TemplateElement.Remove();

                RemoveWhitespace(item.TemplateElement.Content.GetDescendants());
            }

            return items;
        }

        static bool HasAttr(IElement elem, string attrName)
        {
            return elem.HasAttribute(attrName);
        }

        static string Attr(IElement elem, string attrName)
        {
            return elem.GetAttribute(attrName);
        }

        protected async Task WalkTemplateElements(IEnumerable<IElement> elements,
            Func<HtmlTemplateElement, Task<bool>> action)
        {
            foreach (var elem in elements.ToArray())
            {
                if (_processedElements.Any(x => x == elem))
                    throw new Exception("This element was already processed.");

                _processedElements.Add(elem);

                if (elem is AngleSharp.Html.Dom.IHtmlTemplateElement)
                    continue;

                if (elem.Owner == null)
                    throw new Exception("This node has no owner.");

                if (elem.Parent == null)
                    throw new Exception("This node has no parent.");

                // Find instruction attribute.
                var attr = elem.Attributes.FirstOrDefault(a =>
                    a.Name == TemplateAttr.Property ||
                    a.Name == TemplateAttr.Foreach ||
                    a.Name == TemplateAttr.If);

                if (attr != null)
                {
                    var cur = CurTemplateElement = CreateTemplateElement(elem, attr);

                    // Remove instruction attribute.
                    elem.RemoveAttribute(attr.Name);

                    IsMatch = false;

                    var processContent = await action(CurTemplateElement);

                    if (!processContent)
                        continue;

                    // Skip content if this node was removed.
                    if (elem.Parent == null)
                        continue;
                }

                CurTemplateElement = null;

                // Process content.
                await WalkTemplateElements(elem.Children, action);
            }
        }

        static HtmlTemplateElement CreateTemplateElement(IElement node, IAttr attr)
        {
            var elem = TemplateNodeFactory.Create<HtmlTemplateElement>(attr.Value);
            elem.Elem = CleanupAttributes(node);
            elem.Attr = attr;
            elem.IsForeach = attr.Name == TemplateAttr.Foreach;
            elem.IsCondition = attr.Name == TemplateAttr.If;
            elem.ValueTemplateName = Attr(node, TemplateAttr.ValueTemplateRef);

            return elem;
        }

        protected IElement CurElem
        {
            get { return ((HtmlTemplateElement)CurTemplateElement).Elem; }
        }

        void AppendTextNode(string value)
        {
            AppendNode(CreateTextNode(value));
        }

        INode CreateTextNode(string value)
        {
            // KABU TODO: IMPORTANT: In HtmlAgilityPack we had to use HtmlEntity.Entitize
            //  Do we have to entitize in AngleSharp as well?
            return CurElem.Owner.CreateTextNode(value);
        }

        void Br()
        {
            AppendElem("br");
        }

        IElement AppendElem(string name)
        {
            return CurElem.AppendElement(CurElem.Owner.CreateElement(name));
        }

        INode AppendNode(INode node)
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

        protected IElement E(string name, params object[] content)
        {
            var elem = CurrentTemplate.Doc.CreateElement(name);

            if (content != null)
            {
                foreach (var attr in content.OfType<IAttr>())
                    elem.Attributes.SetNamedItem(attr);

                foreach (var obj in content)
                {
                    if (obj is IAttr)
                        continue;

                    if (obj is string text)
                        elem.AppendChild(CreateTextNode(text));
                    else
                        elem.AppendChild((IElement)obj);
                }
            }

            return elem;
        }

        protected IAttr A(string name, string value)
        {
            var attr = CurrentTemplate.Doc.CreateAttribute(name);
            attr.Value = value;

            return attr;
        }

        protected static string ShrinkDecimal(decimal? value, int decimalPlaces = 1)
        {
            if (value == null)
                return "";

            if ((value % 1) == 0)
                return string.Format("{0:0}", value);

            var text = value.ToString();
            var sep = System.Threading.Thread.CurrentThread.CurrentUICulture.NumberFormat.NumberDecimalSeparator[0];
            while (text[^1] == '0' || text[^1] == sep)
                text = text[..^1];

            return text;
        }

        protected static IElement CleanupAttributes(IElement elem)
        {
            // TODO: Do we really need to remove "id"?
            elem.RemoveAttribute("id");

            foreach (var attr in elem.Attributes.ToArray())
                if (attr.LocalName.Contains('-'))
                    elem.RemoveAttribute(attr.NamespaceUri, attr.LocalName);

            return elem;
        }

        protected async Task<IElement> GetStylesheet(string path)
        {
            string content = await ReadFileAsync(path);

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
