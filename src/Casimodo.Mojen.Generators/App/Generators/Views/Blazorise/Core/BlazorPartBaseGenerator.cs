using Casimodo.Lib;
using System.IO;

#nullable enable
namespace Casimodo.Mojen.Blazorise
{
    public abstract class BlazorPartBaseGenerator : AppPartGenerator
    {
        public BlazorConfig BlazorConfig { get; set; } = default!;

        protected override void GenerateCore()
        {
            base.GenerateCore();

            BlazorConfig = App.Get<BlazorConfig>();
        }

        public void ONullableEnable()
        {
            O("#nullable enable");
        }

        public void OBlazorCode(Action content)
        {
            O("@code {");
            Push();
            content();
            Pop();
            O("}");
        }


        public void PerformWrite(MojViewConfig view, Action callback)
        {
            PerformWrite(BuildFilePath(view), (stream, writer) => callback());
        }

        public string GetViewDirPath(MojViewConfig view)
        {
            return Path.Combine(BlazorConfig.ComponentsOutputDirPath, view.TypeConfig.PluralName);
        }

        public string BuildFilePath(MojViewConfig view, string? name = null)
        {
            name = BuildFileName(view, name);

            return Path.Combine(GetViewDirPath(view), name);
        }

        string BuildFileName(MojViewConfig view, string? pathOrName)
        {
            string? name = pathOrName;
            string? path = null;

            if (pathOrName?.ContainsAny("/", @"\") == true)
            {
                name = Path.GetFileName(pathOrName);
                if (path != null)
                    path = Path.Combine(path, Path.GetDirectoryName(pathOrName)!);
                else
                    path = Path.GetDirectoryName(pathOrName);
            }

            if (name == null)
            {
                name = view.FileName ?? view.Alias ?? view.Name ?? view.MainRoleName;

                if (view.IsPage && name == view.MainRoleName)
                    name = view.TypeConfig.PluralName;
            }

            if (string.IsNullOrEmpty(name))
                throw new MojenException("Failed to computed the file name/path of the view.");


            name += "Generated";

            name += ".razor";

            pathOrName = path != null
                ? Path.Combine(path, name).Replace(@"\", "/")
                : name;

            return pathOrName;
        }

        public List<MojXAttribute> Attributes { get; } = new();

        public void ElemAttr(string name, object value)
        {
            Attributes.Add(XA(name, value));
        }       

#pragma warning disable IDE1006 // Naming Styles
        public void oElemAttrs(string? target = null)

        {
            var result = GetElemAttrs(target);
            if (result != null)
            {
                o(result);
                o(" ");
            }
        }
#pragma warning restore IDE1006 // Naming Styles

        public string GetElemAttrs(string? target = null)
        {
            string result = "";
            var attrs = GetElemAttrsByTarget(target);
            if (attrs.Any())
            {
                result = " " + attrs
                    .Select(x => $"{x.Name.LocalName}='{x.Value}'")
                    .Join(" ");
            }

            ClearElemAttrs(target);

            return result;
        }

        MojXAttribute[] GetElemAttrsByTarget(string? target)
        {
            return Attributes.Where(x => x.Target == target).ToArray();
        }

        public void ClearElemAttrs(string? target = null)
        {
            foreach (var attr in GetElemAttrsByTarget(target))
                Attributes.Remove(attr);
        }

        public void ElemFlag(string name)
        {
            Attributes.Add(XA(name, name));
        }

        public void ElemClass(string classes, string? target = null)
        {
            if (string.IsNullOrWhiteSpace(classes))
            {
                return;
            }

            var attr = GetOrCreateAttr("Class", target);
            attr.Value = string.IsNullOrEmpty(attr.Value) ? classes : attr.Value + " " + classes;
        }

        public void ElemStyle(string value)
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
    }
}
