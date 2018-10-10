using Casimodo.Lib.SimpleParser;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Hosting;

namespace Casimodo.Lib.Templates
{
    public class HtmlTemplateElement : TemplateElement
    {
        public HtmlNode Elem { get; set; }
        public HtmlAttribute Attr { get; set; }

    };

    public class HtmlTemplate
    {
        public HtmlDocument Doc { get; set; }
        public List<HtmlInlineTemplate> InlineTemplates { get; set; } = new List<HtmlInlineTemplate>();
    }

    public class HtmlInlineTemplate
    {
        public string Name { get; set; }
        public HtmlNode Elem { get; set; }
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

        public HtmlTemplateProcessor()
        { }

        protected List<HtmlTemplate> Fragments { get; set; } = new List<HtmlTemplate>();

        public RazorRemoverParser RazorRemover { get; set; } = new RazorRemoverParser();

        // KABU TODO: MAGIC paths.
        // KABU TODO: ELIMINATE
        string BaseDocumentCssPath { get; set; } = "~/Content/app/css/print/print-document.css";
        // KABU TODO: ELIMINATE
        string BaseContainerCssPath { get; set; } = "~/Content/app/css/print/print-container.css";

        // KABU TODO: MAGIC path.
        public string ImagePageTemplatePath { get; set; } = "~/Content/app/print/image-page-template.html";

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
            ProcessTemplateElemCore(CurrentTemplate.Doc.DocumentNode, ExecuteCurrentTemplateElement);
        }

        void ProcessTemplateElemCore(HtmlNode elem, Action visitor)
        {
            VisitTemplateElements(elem, () =>
            {
                if (CurTemplateElement.IsForeach)
                {
                    var values = FindObjects(CurTemplateElement)
                        .Where(x => x != null)
                        .ToArray();

                    var originalElem = CurTemplateElement.Elem;

                    if (values.Length != 0)
                    {
                        var parentElem = originalElem.ParentNode;
                        var cursorNode = originalElem;
                        // KABU TODO: IMPORTANT: We don't allow nested foreach instructions yet.

                        // KABU TODO: IMPORTANT: If we allow nested foreach then:
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
                            var currentTemplateElem = originalElem.Clone();

                            ProcessTemplateElemCore(currentTemplateElem, visitor);

                            // Insert all children of the transformed "foreach" template element.
                            foreach (var child in currentTemplateElem.ChildNodes)
                            {
                                parentElem.InsertAfter(child, cursorNode);
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
                    var parentElem = originalElem.ParentNode;
                    var cursorNode = originalElem;

                    if (EvaluateCondition(CurTemplateElement))
                    {
                        // Operate on a clone of the original "if" element.
                        var currentTemplateElem = originalElem.Clone();

                        ProcessTemplateElemCore(currentTemplateElem, visitor);

                        // Insert all children of the transformed "foreach" template element.
                        foreach (var child in currentTemplateElem.ChildNodes)
                        {
                            parentElem.InsertAfter(child, cursorNode);
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
                    var parentElem = originalElem.ParentNode;
                    var cursorNode = originalElem;
                    var valueVarName = "value";
                    var value = EvaluateValue(CurTemplateElement);

                    CoreContext.Data.AddProp(value?.GetType() ?? typeof(object), valueVarName, value);

                    // Operate on a clone of the original value template element.
                    var currentTemplateElem = template.Elem.Clone();

                    ProcessTemplateElemCore(currentTemplateElem, visitor);

                    // Insert all children of the transformed "foreach" template element.
                    foreach (var child in currentTemplateElem.ChildNodes)
                    {
                        parentElem.InsertAfter(child, cursorNode);
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
            CurElem.SetAttributeValue("src", value);
        }

        static readonly string[] NewLineTokens = { "\n", Environment.NewLine };

        public override void SetText(string value)
        {
            CurElem.RemoveAllChildren();

            if (IsEmpty(value))
                return;

            if (!value.Contains(NewLineTokens))
                TextNode(value);
            else
            {
                // Emit <br/> for each new line token.
                // Trim start because normally we'll usually have an initial unwanted new line
                // due to how users define the such property values in XML.
                var lines = value.TrimStart().Split(NewLineTokens, StringSplitOptions.None);

                for (int i = 0; i < lines.Length; i++)
                {
                    TextNode(lines[i]);
                    if (i < lines.Length - 1)
                        Br();
                }
            }
        }

        public override void RemoveValue()
        {
            CurElem.Remove();
        }

        public Func<IEnumerable<string>> GetLibCssFilePaths = () => Enumerable.Empty<string>();
        public Func<IEnumerable<string>> GetCssFilePaths = () => Enumerable.Empty<string>();

        // KABU TODO: ELIMINATE
        IEnumerable<string> GetBaseCssFilePaths()
        {
            yield return BaseDocumentCssPath;
            yield return BaseContainerCssPath;
        }

        public string StylesHtml { get; set; }

        public void ClearDocument()
        {
            Fragments.Clear();
        }

        public virtual void BuildStylesheets()
        {
            // Stylesheets
            if (StylesHtml == null)
            {
                var sb = new StringBuilder();
                var files = new List<string>();
                files.AddRange(GetLibCssFilePaths());
                files.AddRange(GetBaseCssFilePaths());
                files.AddRange(GetCssFilePaths());

                foreach (var styleFilePath in files)
                {
                    sb.Append("<style>");
                    sb.Append(FileHelper.ReadTextFile(HostingEnvironment.MapPath(styleFilePath)));
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
            return RazorRemover.RemoveRazorSyntax(FileHelper.ReadTextFile(HostingEnvironment.MapPath(virtualFilePath)));
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

        // KABU TODO: Find a better name? This does not always correspond to a page (e.g. PDF page).
        public HtmlTemplate NewPage()
        {
            if (PageTemplate == null)
                PageTemplate = CreatePageTemplate();

            var doc = new HtmlDocument();
            doc.LoadHtml(PageTemplate);

            var fragment = new HtmlTemplate();
            fragment.Doc = doc;
            fragment.InlineTemplates = GetInlineTemplateHtmlElemNodes(doc.DocumentNode).ToList();

            ExpandInlineHtmlTemplates(fragment);

            Fragments.Add(fragment);

            Page = fragment;

            return fragment;
        }

        void ExpandInlineHtmlTemplates(HtmlTemplate fragment)
        {
            foreach (var node in GetHtmlElemNodes(fragment.Doc.DocumentNode)
                .Where(x => HasAttr(x, TemplateAttr.IncludeTemplate))
                .ToArray())
            {
                // Replace template includes with template content.

                var templateName = node.GetAttributeValue(TemplateAttr.IncludeTemplate, null);

                var template = fragment.InlineTemplates.FirstOrDefault(x => x.Name == templateName);
                if (template == null)
                    throw new TemplateException($"Inline template '{templateName}' not found.");

                var templateElem = template.Elem.Clone();
                var cursorNode = node;
                // Insert all children of the transformed "foreach" template element.
                foreach (var child in templateElem.ChildNodes)
                {
                    node.ParentNode.InsertAfter(child, cursorNode);
                    cursorNode = child;
                }

                // Remove "include template" instruction from tree.
                node.Remove();
            }
        }

        public HtmlTemplate NewImagePage()
        {
            if (ImagePageTemplate == null)
                ImagePageTemplate = LoadTemplatePart(ImagePageTemplatePath);

            var doc = new HtmlDocument();
            doc.LoadHtml(ImagePageTemplate);

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
                    page.Doc.Save(writer);
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

        IEnumerable<HtmlInlineTemplate> GetInlineTemplateHtmlElemNodes(HtmlNode root)
        {
            // Return top level "data-template" element nodes.
            foreach (var node in root.ChildNodes
                .Where(x => x.NodeType == HtmlNodeType.Element)
                .ToArray())
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

        protected IEnumerable<HtmlNode> GetHtmlElemNodes(HtmlNode parent)
        {
            if (parent.OwnerDocument == null)
                // Node might have already been removed by the transformation.
                yield break;

            foreach (var node in parent.ChildNodes
                .Where(x => x.NodeType == HtmlNodeType.Element)
                .ToArray())
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

        bool HasAttr(HtmlNode node, string attrName)
        {
            return node.Attributes.FirstOrDefault(a => a.Name == attrName) != null;
        }

        string Attr(HtmlNode node, string attrName)
        {
            return node.GetAttributeValue(attrName, null);
        }

        protected IEnumerable<HtmlTemplateElement> GetTemplateElements(HtmlNode elem)
        {
            if (elem.OwnerDocument == null)
                // Node might have already been removed by the transformation.
                yield break;

            foreach (var node in elem.ChildNodes.ToArray())
            {
                if (node.NodeType != HtmlNodeType.Element)
                    continue;

                if (node.OwnerDocument == null)
                    // Node might have already been removed by the transformation.
                    continue;

                // KABU TODO: MAGIC strings
                var attr = node.Attributes.FirstOrDefault(a =>
                    a.Name == TemplateAttr.Property ||
                    // KABU TODO: IMPORTANT: Change occurences of "data-placeholder" to "data-property".
                    a.Name == TemplateAttr.Placeholder ||
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

        HtmlTemplateElement CreateTemplateElement(HtmlNode node, HtmlAttribute attr)
        {
            var elem = TemplateNodeFactory.Create<HtmlTemplateElement>(attr.Value);
            elem.Elem = CleanupAttributes(node);
            elem.Attr = attr;
            elem.IsForeach = attr.Name == TemplateAttr.Foreach;
            elem.IsCondition = attr.Name == TemplateAttr.If;
            elem.ValueTemplateName = Attr(node, TemplateAttr.ValueTemplateName);

            return elem;
        }

        protected HtmlNode CurElem
        {
            get { return ((HtmlTemplateElement)CurTemplateElement).Elem; }
        }

        protected void VisitTemplateElements(HtmlNode template, Action action)
        {
            foreach (HtmlTemplateElement item in GetTemplateElements(template))
            {
                if (item.Elem.OwnerDocument == null)
                    // Element might have already been removed by the transformation.
                    continue;

                CurTemplateElement = item;
                IsMatch = false;
                action();

                // Remove placeholder attribute.
                item.Attr.Remove();
            }
            CurTemplateElement = null;
        }

        void TextNode(string value)
        {
            AppendNode(HtmlEntity.Entitize(CurElem.OwnerDocument.CreateTextNode(value)));
        }

        void Br()
        {
            AppendElem("br");
        }

        HtmlNode AppendElem(string name)
        {
            return AppendNode(CurElem.OwnerDocument.CreateElement(name));
        }

        HtmlNode AppendNode(HtmlNode node)
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

        protected void Css(string name, object value)
        {
            Guard.ArgNotEmpty(name, nameof(name));

            var attr = CurElem.Attributes.FirstOrDefault(x => x.Name == "style");
            if (attr == null)
                attr = CurElem.Attributes.Append("style", "");

            var valueStr = (value != null ? value.ToString() : "").Trim();
            bool remove = string.IsNullOrEmpty(valueStr);

            var items = (attr.Value ?? "").Trim()
                .Split(";", StringSplitOptions.RemoveEmptyEntries)
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

        protected HtmlNode E(string name, params object[] content)
        {
            var elem = Page.Doc.CreateElement(name);

            if (content != null)
            {
                foreach (var attr in content.OfType<HtmlAttribute>())
                    elem.Attributes.Add(attr);

                foreach (var obj in content)
                {
                    if (obj is HtmlAttribute)
                        continue;

                    if (obj is string)
                        elem.AppendChild(HtmlEntity.Entitize(Page.Doc.CreateTextNode((string)obj)));
                    else
                        elem.AppendChild((HtmlNode)obj);
                }
            }

            return elem;
        }

        protected HtmlAttribute A(string name, string value)
        {
            return Page.Doc.CreateAttribute(name, value);
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

        protected HtmlNode CleanupAttributes(HtmlNode elem)
        {
            var attrs = elem.Attributes;
            attrs.Remove("id");
            // KABU TODO: Remove all attributes starting with "ng-" and "data-" (except for template attributes).
            attrs.Remove("ng-bind");
            attrs.Remove("ng-hide");
            attrs.Remove("data-ng-src");

            return elem;
        }

        protected HtmlNode GetStylesheet(string path)
        {
            string content = FileHelper.ReadTextFile(HostingEnvironment.MapPath(path));

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
