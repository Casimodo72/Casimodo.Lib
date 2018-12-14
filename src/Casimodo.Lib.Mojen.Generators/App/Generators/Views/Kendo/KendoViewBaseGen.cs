using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    public abstract class KendoViewGenBase : WebViewGenerator
    {
        public KendoViewGenBase()
        {
            KendoGen = AddSub<KendoPartGen>();
            KendoGen.SetParent(this);
        }

        public KendoPartGen KendoGen { get; private set; }

        public void KendoJsTemplate(Action action)
        {
            // Buffer any output in order to transform to Kendo template.
            StartBuffer();

            action();

            var text = BufferedText
                .Replace(@"'#", @"'\\#")
                .Replace("\"#", "\"\\#")
                .Replace(@"&#", @"&\\#");

            EndBuffer();

            Writer.Write(text);
        }

        public void OMvcPartialView(string virtualPath, bool kendoEscape = false)
        {
            O("@Html.Partial(\"{0}\"){1}",
                virtualPath,
                kendoEscape ? ".ToKendoTemplate()" : "");
        }

        public virtual string GetViewCssStyle(WebViewGenContext context)
        {
            var view = context.View;

            var styles = new List<string>();

            if (view.MinHeight != null) styles.Add($"min-height:{view.MinHeight}px");
            if (view.MaxHeight != null) styles.Add($"max-height:{view.MaxHeight}px");
            if (view.MinWidth != null) styles.Add($"min-width:{view.MinWidth}px");
            if (view.MaxWidth != null) styles.Add($"max-width:{view.MaxWidth}px");

            if (styles.Count == 0)
                return "";

            return $" style='{styles.Join(";")}'";
        }

        public void OKendoTemplateBegin(string templateId)
        {
            XB($"<script id='{templateId}' type='text/x-kendo-template'>");
        }

        public void OKendoTemplateEnd()
        {
            XE("</script>");
        }

        protected string GetViewHtmlId(WebViewGenContext context)
        {
            if (!context.IsViewIdEnabled)
                return "";

            CheckViewId(context.View);

            return string.Format(" id='{0}view-{1}'",
                    (context.ViewRole != null ? context.ViewRole + "-" : ""),
                    context.View.Id);
        }

        public void OValidationMessageElem(string propPath)
        {
            // Validation error message.
            O($"<span class='field-validation-valid' data-valmsg-for='{propPath}' data-valmsg-replace='true'></span>");
        }
    }
}