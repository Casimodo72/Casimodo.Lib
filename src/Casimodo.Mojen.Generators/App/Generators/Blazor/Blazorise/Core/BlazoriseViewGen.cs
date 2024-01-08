using Casimodo.Mojen.App.Generators.Blazor.Core;

#nullable enable
namespace Casimodo.Mojen.App.Generators.Blazor.Blazorise
{
    public abstract class BlazoriseViewGen : BlazorPartGenerator
    {
        public string? DataViewModelAccessor { get; set; }

        string GetBindingRootPath()
        {
            // E.g. "item.FirstName" or just "FirstName" if DataViewModelAccessor is not assigned.
            return !string.IsNullOrWhiteSpace(DataViewModelAccessor) ? $"{DataViewModelAccessor}." : "";
        }

        // TODO: REMOVE?
        public string GetBinding(MojViewProp vprop)
        {
            return BuildBinding(vprop.FormedTargetPath.ToString());
        }

        public string GetBinding(MojFormedType propTypePath, bool alias = false)
        {
            return BuildBinding(alias ? propTypePath.FormedNavigationFrom.TargetAliasPath : propTypePath.FormedNavigationFrom.TargetPath);
        }

        public string GetBinding(WebViewGenContext context, bool alias = false)
        {
            return BuildBinding(alias ? context.PropInfo.PropAliasPath : context.PropInfo.PropPath);
        }

        string BuildBinding(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new MojenException($"Failed to build property binding path.");

            path = path.Replace(".", "?.");

            return $"{GetBindingRootPath()}{path}";
        }

        public string GetElementId(MojViewPropInfo iprop)
        {
            return iprop.PropPath.Replace(".", "_");
        }

        public List<MojXAttribute> Attributes { get; } = [];

        public void Attr(string name, object value)
        {
            Attributes.Add(XA(name, value));
        }

        public string GetAttrs(string? target = null, bool consume = true)
        {
            string result = "";
            var attrs = GetAttrsByTarget(target);
            if (attrs.Any())
            {
                result = " " + attrs
                    .Select(x => $"{x.Name.LocalName}='{x.Value}'")
                    .Join(" ");
            }

            if (consume)
                ClearAttrs(target);

            return result;
        }

        MojXAttribute[] GetAttrsByTarget(string? target)
        {
            return Attributes.Where(x => x.Target == target).ToArray();
        }

        public void ClearAttrs(string? target = null)
        {
            foreach (var attr in GetAttrsByTarget(target))
                Attributes.Remove(attr);
        }

        public void FlagAttr(string name)
        {
            Attributes.Add(XA(name, name));
        }

        public void ClassAttr(string classes, string? target = null)
        {
            if (string.IsNullOrWhiteSpace(classes))
            {
                return;
            }

            var attr = GetOrCreateAttr("Class", target);
            attr.Value = string.IsNullOrEmpty(attr.Value) ? classes : attr.Value + " " + classes;
        }

        public void StyleAttr(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var attr = GetOrCreateAttr("Style");
            attr.Value = string.IsNullOrEmpty(attr.Value) ? value : attr.Value + ";" + value;
        }

        public MojXAttribute GetOrCreateAttr(string name, string? target = null)
        {
            if (Attributes.FirstOrDefault(x => x.Name == name && x.Target == target) is not MojXAttribute attr)
            {
                attr = XA(name, "");
                attr.Target = target;
                Attributes.Add(attr);
            }
            return attr;
        }

        public virtual string GetStyleAttr(HtmlStyleProp[] props)
        {
            if (props == null || props.Length == 0)
                return "";

            return $" style='{props.Select(x => $"{x.Name}:{x.Value}").Join(";")}'";
        }

#pragma warning disable IDE1006 // Naming Styles
        public void oAttrs(string? target = null, bool consume = true)

        {
            var result = GetAttrs(target, consume);
            if (result != null)
            {
                o(result);
                o(" ");
            }
        }
#pragma warning restore IDE1006 // Naming Styles

        public HtmlStyleProp StyleProp(string name, string value)
        {
            return new HtmlStyleProp { Name = name, Value = value };
        }

        void AddStyleProp(List<HtmlStyleProp> props, string name, string value, Func<string, bool>? filter = null)
        {
            if (filter != null && !filter(name))
                return;

            props.Add(StyleProp(name, value));
        }

        public virtual HtmlStyleProp[] GetViewStyles(WebViewGenContext context, Func<string, bool>? filter = null)
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