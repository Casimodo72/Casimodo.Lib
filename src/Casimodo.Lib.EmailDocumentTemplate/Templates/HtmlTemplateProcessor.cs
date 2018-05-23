using Casimodo.Lib;
using Casimodo.Lib.SimpleParser;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace Casimodo.Lib.Templates
{
    public class HtmlTemplateElement : TemplateElement
    {
        public HtmlNode Elem { get; set; }
        public HtmlAttribute Attr { get; set; }
    };

    public abstract class HtmlTemplateProcessor : TemplateProcessor, ITemplateProcessor
    {
        public HtmlTemplateProcessor()
        { }

        protected List<HtmlDocument> Fragments { get; set; } = new List<HtmlDocument>();

        public RazorRemoverParser RazorRemover { get; set; } = new RazorRemoverParser();

        string BaseDocumentCssPath { get; set; } = "~/Content/app/css/print/print-document.css";
        string BaseContainerCssPath { get; set; } = "~/Content/app/css/print/print-container.css";

        public string ImagePageTemplatePath { get; set; } = "~/Content/app/print/image-page-template.html";

        string ImagePageTemplate { get; set; }

        string PageTemplate { get; set; }
        HtmlDocument Page { get; set; }

        protected abstract string CreatePageTemplate();

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

        protected virtual IEnumerable<string> GetLibCssFilePaths()
        {
            return Enumerable.Empty<string>();
        }

        IEnumerable<string> GetBaseCssFilePaths()
        {
            yield return BaseDocumentCssPath;
            yield return BaseContainerCssPath;
        }

        protected virtual IEnumerable<string> GetCssFilePaths()
        {
            return Enumerable.Empty<string>();
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

        public HtmlDocument NewPage()
        {
            if (PageTemplate == null)
                PageTemplate = CreatePageTemplate();

            var fragment = new HtmlDocument();
            fragment.LoadHtml(PageTemplate);

            Fragments.Add(fragment);

            Page = fragment;

            return fragment;
        }

        public HtmlDocument NewImagePage()
        {
            if (ImagePageTemplate == null)
                ImagePageTemplate = LoadTemplatePart(ImagePageTemplatePath);

            var fragment = new HtmlDocument();
            fragment.LoadHtml(ImagePageTemplate);
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
                    page.Save(writer);
                    writer.Flush();

                    sb.Append(writer.ToString());
                }

                // Page break
                if (i < Fragments.Count - 1)
                    sb.Append("<div style=\"page-break-before:always\"></div>");
                i++;
            }

            sb.Append("</body>");
            sb.Append("</html>");

            return sb.ToString();
        }

        protected List<HtmlTemplateElement> GetTemplateElements(HtmlDocument template)
        {
            var items =
                (from node in template.DocumentNode.Descendants()
                 where node.NodeType == HtmlNodeType.Element
                 let attr = node.Attributes.FirstOrDefault(attr => attr.Name == "data-property" || attr.Name == "data-placeholder")
                 where attr != null
                 select new HtmlTemplateElement
                 {
                     Elem = CleanupAttributes(node),
                     Attr = attr,
                     Id = attr.Value,
                     Kind = attr.Name == "data-area" ? TemplateElemKind.Area : TemplateElemKind.Property
                 })
               .ToList();

            foreach (var item in items)
                BuildTemplateElement(item);

            return items;
        }

        protected HtmlNode CurElem
        {
            get { return ((HtmlTemplateElement)CurTemplateElement).Elem; }
        }

        protected void ProcessTemplateElements(HtmlDocument template, Action action)
        {
            var elements = GetTemplateElements(template);
            foreach (HtmlTemplateElement item in elements)
            {
                if (item.Elem.OwnerDocument == null)
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
            var elem = Page.CreateElement(name);

            if (content != null)
            {
                foreach (var attr in content.OfType<HtmlAttribute>())
                    elem.Attributes.Add(attr);

                foreach (var obj in content)
                {
                    if (obj is HtmlAttribute)
                        continue;

                    if (obj is string)
                        elem.AppendChild(HtmlEntity.Entitize(Page.CreateTextNode((string)obj)));
                    else
                        elem.AppendChild((HtmlNode)obj);
                }
            }

            return elem;
        }

        protected HtmlAttribute A(string name, string value)
        {
            return Page.CreateAttribute(name, value);
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
