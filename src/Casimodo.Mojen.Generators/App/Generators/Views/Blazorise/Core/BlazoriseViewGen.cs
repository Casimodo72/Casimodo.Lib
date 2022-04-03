using Casimodo.Lib;
using System.IO;

namespace Casimodo.Mojen.Blazorise
{
    public abstract class BlazoriseViewGen : BlazorPartBaseGenerator
    {
        public string DataViewModelAccessor { get; set; }

        string GetBindingRootPath()
        {
            // E.g. "item.FirstName" or just "FirstName" if DataViewModelAccessor is not assigned.
            return !string.IsNullOrWhiteSpace(DataViewModelAccessor) ? $"{DataViewModelAccessor}." : "";
        }

        public string GetBinding(MojViewProp vprop)
        {
            string path = vprop.FormedTargetPath.ToString();

            if (path == null)
                throw new MojenException($"Failed to build property binding path for view prop.");

            return $"{GetBindingRootPath()}{path}";
        }

        public string GetBinding(MojFormedType propTypePath, bool alias = false)
        {
            string path = alias ? propTypePath.FormedNavigationFrom.TargetAliasPath : propTypePath.FormedNavigationFrom.TargetPath;

            if (path == null)
                throw new MojenException($"Failed to build property binding path for formed type path.");

            return $"{GetBindingRootPath()}{path}";
        }

        public string GetBinding(WebViewGenContext context, bool alias = false)
        {
            string path = alias ? context.PropInfo.PropAliasPath : context.PropInfo.PropPath;
            if (path == null)
                throw new MojenException($"Failed to build property binding path.");

            return $"{GetBindingRootPath()}{path}";
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
    }
}