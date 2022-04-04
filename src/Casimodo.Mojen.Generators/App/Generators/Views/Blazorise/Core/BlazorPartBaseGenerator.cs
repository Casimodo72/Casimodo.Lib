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

        protected void OTODO(string text)
        {
            O($"// TODO: {text ?? ""}");
        }
    }
}
