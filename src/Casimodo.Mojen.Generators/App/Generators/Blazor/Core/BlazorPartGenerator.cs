using Casimodo.Mojen.App.Generators.Blazor.Configs;
using System.IO;

#nullable enable
namespace Casimodo.Mojen.App.Generators.Blazor.Core
{
    public abstract class BlazorPartGenerator : AppPartGenerator
    {
        public BlazorConfig BlazorConfig { get; private set; } = default!;

        public BlazorDataServiceConfig DataServicesConfig { get; private set; } = default!;

        protected override void GenerateCore()
        {
            base.GenerateCore();

            BlazorConfig = App.Get<BlazorConfig>();
            DataServicesConfig = App.Get<BlazorDataServiceConfig>(required: false);
        }

        public void ONullableEnable()
        {
            O("#nullable enable");
        }

        public void OBlazorCode(Action content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            O("@code {");
            Push();
            content();
            Pop();
            O("}");
        }

        public void OStateHasChanged()
        {
            O("StateHasChanged();");
        }

        public void Write(MojViewConfig view, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            PerformWrite(BuildFilePath(view), (stream, writer) => action());
        }

        public string BuildFilePath(MojViewConfig view, string? name = null)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));

            name = BuildRelativeFilePath(view, name);

            return Path.Combine(GetViewDirPath(view), name);
        }

        string BuildRelativeFilePath(MojViewConfig view, string? pathOrName)
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
                name = BuildLookupViewName(view);
            }

            if (string.IsNullOrWhiteSpace(name))
                throw new MojenException("Failed to computed the file name/path of the view.");

            // NOTE: When using intermediate dots in the file name, the component is
            // accessible by replacing the dots with underscores.
            // E.g. the component of file "MyComponent.j.razor" can be accessed with "MyComponent_j".
            name += ".gen.razor";

            pathOrName = path != null
                ? Path.Combine(path, name).Replace(@"\", "/")
                : name;

            return pathOrName;
        }

        public string BuildLookupViewName(MojViewConfig view)
        {
            if (null != (view.FileName ?? view.Alias ?? view.Name))
            {
                return view.FileName ?? view.Alias ?? view.Name;
            }
            else
            {
                return view.TypeConfig.Name + view.MainRoleName;
            }
        }

        public string BuildLookupViewComponentName(MojViewConfig view)
        {
            return BuildLookupViewName(view) + "_gen";
        }

        public string GetViewDirPath(MojViewConfig view)
        {
            return Path.Combine(BlazorConfig.ComponentsOutputDirPath, view.TypeConfig.PluralName);
        }

        protected void ORazorTODO(string text)
        {
            ORazorComment($"TODO: {text ?? ""}");
        }

        protected void OTODO(string text)
        {
            O($"// TODO: {text ?? ""}");
        }
    }
}
