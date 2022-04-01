using System;
using System.Collections.Generic;
using System.Linq;

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

        public string DataViewModelAccessor { get; set; }

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

        string GetBindingPrefixObject()
        {
            // E.g. "item.FirstName" or just "FirstName" if DataViewModelAccessor is not assigned.
            return !string.IsNullOrWhiteSpace(DataViewModelAccessor) ? DataViewModelAccessor : "";
        }

        public string GetBinding(MojViewProp vprop)
        {
            string path = vprop.FormedTargetPath.ToString();

            if (path == null)
                throw new MojenException($"Failed to build property binding path for view prop.");

            return $"{GetBindingPrefixObject()}{path}";
        }

        public string GetBinding(MojFormedType propTypePath, bool alias = false)
        {
            string path = alias ? propTypePath.FormedNavigationFrom.TargetAliasPath : propTypePath.FormedNavigationFrom.TargetPath;

            if (path == null)
                throw new MojenException($"Failed to build property binding path for formed type path.");

            return $"{GetBindingPrefixObject()}{path}";
        }

        public string GetBinding(WebViewGenContext context, bool alias = false)
        {
            string path = alias ? context.PropInfo.PropAliasPath : context.PropInfo.PropPath;
            if (path == null)
                throw new MojenException($"Failed to build property binding path.");

            return $"{GetBindingPrefixObject()}{path}";
        }

        public void ElemDataBindAttr(WebViewGenContext context)
        {
            if (context.View.UseMVVM)
            {
                GetOrCreateAttr("data-bind").Value = $"value:{GetBinding(context)}";
            }
        }

        public virtual string GetStyleAttr(HtmlStyleProp[] props)
        {
            if (props == null || props.Length == 0)
                return "";

            return $" style='{props.Select(x => $"{x.Name}:{x.Value}").Join(";")}'";
        }

        public HtmlStyleProp StyleProp(string name, string value)
        {
            return new HtmlStyleProp { Name = name, Value = value };
        }

        void AddStyleProp(List<HtmlStyleProp> props, string name, string value, Func<string, bool> filter = null)
        {
            if (filter != null && !filter(name))
                return;

            props.Add(StyleProp(name, value));
        }

        public virtual HtmlStyleProp[] GetViewStyles(WebViewGenContext context, Func<string, bool> filter = null)
        {
            var view = context.View;

            var props = new List<HtmlStyleProp>();

            if (view.Width != null) AddStyleProp(props, "width", $"{view.Width}px", filter);
            if (view.MinWidth != null) AddStyleProp(props, "min-width", $"{view.MinWidth}px", filter);
            if (view.MaxWidth != null) AddStyleProp(props, "max-width", $"{view.MaxWidth}px", filter);

            if (view.Height != null) AddStyleProp(props, "height", $"{view.Height}px", filter);
            if (view.MinHeight != null) AddStyleProp(props, "min-height", $"{view.MinHeight}px", filter);
            if (view.MaxHeight != null) AddStyleProp(props, "max-height", $"{view.MaxHeight}px", filter);


            return props.ToArray();
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

        // TODO: REMOVE
        //public void OValidationMessageElem(string propPath)
        //{
        //    // Validation error message.
        //    O($"<span class='field-validation-valid' data-valmsg-for='{propPath}' data-valmsg-replace='true'></span>");
        //}
    }
}