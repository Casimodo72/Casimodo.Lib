using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    public abstract class WebPartGenerator : AppPartGenerator
    {
        public WebBuildConfig WebConfig { get; set; }

        public override void Prepare()
        {
            base.Prepare();
            WebConfig = App.Get<WebBuildConfig>();
        }

        public DataLayerConfig GetDataConfig(MojViewConfig view)
        {
            return App.GetDataLayerConfig(view.TypeConfig.DataContextName);
        }

        public string GetJsScriptNamespace(MojViewConfig view)
        {
            return WebConfig.ScriptNamespace;
        }

        public string GetJsScriptUINamespace(MojViewConfig view)
        {
            var ns = WebConfig.ScriptUINamespace;
            if (string.IsNullOrWhiteSpace(ns))
                throw new MojenException($"No UI namespace defined for data layer config '{GetDataConfig(view).Name}'.");

            return ns;
        }

        public void RegisterComponent(WebViewGenContext context)
        {
            var view = context.View;

            // KABU TODO: REVISIT: Currently we need to add "Page,List" components
            //   twice, because we need "Page" for e.g. navigation to pages and
            //   "List" for authorization of the "List" component itself.
            // KABU TODO: IMPORTANT: Find a way to eliminate this hack.
            //   Basically we need a dedicated object to represent a page.
            //   A single view should have only *one* view role.

            if (view.Kind.Roles.HasFlag(MojViewRole.Page))
            {
                App.Get<WebResultBuildInfo>().Components.Add(new WebResultComponentInfo
                {
                    View = view,
                    Role = view.MainRoleName,
                    Url = view.Url
                });
            }           

            if (view.Kind.Roles.HasFlag(MojViewRole.Page) && view.Kind.Roles.HasFlag(MojViewRole.List))
            {
                App.Get<WebResultBuildInfo>().Components.Add(new WebResultComponentInfo
                {
                    View = view,
                    Role = "List",
                    Name = context.ComponentName,
                    Namespace = context.UINamespace,
                    Url = view.Url
                });
            }
        }

        // KABU TODO: REMOVE? Not used.
        public string ConvertToCssName(string text)
        {
            Guard.ArgNotNullOrWhitespace(text, nameof(text));

            var s = new StringBuilder();
            foreach (var ch in text)
            {
                if (char.IsUpper(ch) && s.Length != 0)
                    s.Append("-");
                s.Append(char.ToLower(ch));
            }

            return s.ToString();
        }

        protected string ReturnRedirectToActionIndex()
        {
            return "return RedirectToAction(\"Index\");";
        }

        protected void WriteTitleAndMessage(MojViewConfig view)
        {
            if (!string.IsNullOrWhiteSpace(view.Title))
                O("ViewBag.Title = \"{0}\";", view.Title);

            if (!string.IsNullOrWhiteSpace(view.Message))
                O("ViewBag.Message = \"{0}\";", view.Message);
        }

        public string GetWebRepositoryName(MojType type)
        {
            return type.PluralName + (type.Kind == MojTypeKind.Model ? "Model" : "") + "WebRepository";
        }

        public void PerformWrite(MojControllerConfig controller, Action<MojControllerConfig> callback)
        {
            string outputFilePath =
                Path.Combine(
                    App.Get<WebBuildConfig>().WebControllersOutputDirPath,
                    controller.ClassName + ".generated.cs");

            PerformWrite(outputFilePath, () => callback(controller));
        }

        public void OScriptReference(string path, bool async = false)
        {
            O("<script src='{0}' type='text/javascript'{1}></script>",
                path, (async ? " async" : ""));
        }

        public void OScriptBegin()
        {
            XB("<script>");
            OScriptUseStrict();
        }

        public void OScriptUseStrict()
        {
            O("\"use strict\";");
        }

        public void OScriptEnd()
        {
            XE("</script>");
        }

        public void OInlineScriptSourceUrl(string fileName)
        {
            O("//# sourceUrl=" + fileName);
        }

        public void OJsDocReadyBegin()
        {
            OB("$(function ()");
        }

        public void OJsDocReadyEnd()
        {
            End(");");
        }

        public void OJsComment(params string[] comment)
        {
            if (comment == null)
                return;

            foreach (var item in comment)
                O("// " + item);
        }

        public class MojNamespaceContext
        {
            public MojNamespaceContext(string namespaces)
            {
                Guard.ArgNotNullOrWhitespace(namespaces, nameof(namespaces));

                Namespaces.AddRange(namespaces.Split("."));
            }

            public List<string> Namespaces { get; set; } = new List<string>();

            public string Current
            {
                get { return Index >= 0 ? Namespaces[Index] : null; }

            }

            public string Previous
            {
                get { return Index >= 1 ? Namespaces[Index - 1] : null; }

            }

            public bool IsLast
            {
                get { return Index == Namespaces.Count - 1; }
            }

            public bool Next()
            {
                if (Index >= Namespaces.Count - 1)
                    return false;

                Index++;
                return true;
            }

            public int Index { get; private set; } = -1;
        }

        public void OJsNamespace(string ns, Action<MojNamespaceContext> action)
        {
            OUseStrict();

            OJsNamespaceCore(new MojNamespaceContext(ns), action);
        }

        public void OJsNamespaceCore(MojNamespaceContext context, Action<MojNamespaceContext> action)
        {
            while (context.Next())
            {
                if (context.Index == 0)
                    O($"var {context.Current};");

                var absoluteNs = context.Previous != null ? context.Previous + "." + context.Current : context.Current;
                OB($"(function({context.Current})");
                O();

                if (context.IsLast && action != null)
                    action(context);
                else
                    OJsNamespaceCore(context, action);

                O();
                End($")({absoluteNs} || ({absoluteNs} = {{}}));");
            }
        }

        public void OJsNamespace(string ns, Action action)
        {
            OUseStrict();
            O($"var {ns};");
            OB($"(function({ns})");
            O();

            action();

            O();
            End($")({ns} || ({ns} = {{}}));");
        }

        public void OUseStrict()
        {
            O("\"use strict\";");
        }

        public void OJsImmediateBegin(string parameters = "", string variable = null)
        {
            if (!string.IsNullOrWhiteSpace(variable))
                O($"var {variable};");
            OB($"(function ({parameters})");
        }

        public void OJsImmediateEnd(string args = "")
        {
            End($")({args});");
        }

        // KABU TODO: Find a ways to share JS methods with DataLayerGenerator.
        // KABU TODO: REMOVE? Not used.
        //public void OJsClass(string name, bool isstatic = false, string extends = null,
        //    string args = null, Action content = null)
        //{
        //    OJsClass(App.Get<DataLayerConfig>().ScriptNamespace, name, isstatic, extends, args, content);
        //}

        public void OJsClass(string ns, string name, bool isstatic = false,
            string extends = null, string args = null, Action content = null)
        {
            var isderived = !string.IsNullOrWhiteSpace(extends);
            var hasargs = !string.IsNullOrWhiteSpace(args);

            OB($"var {name} = (function ({(isderived ? "_super" : "")})");

            if (isderived)
                O($"casimodo.__extends({name}, _super);");

            // Constructor function.
            O();
            OB($"function {name}({(hasargs ? args : "")})");

            if (isderived)
            {
                O($"_super.call(this{(hasargs ? ", " + args : "")});");
                O();
            }

            if (content != null)
                content();

            End();

            O();
            O($"return {name};");

            End($")({(isderived ? extends : "")});");

            if (isstatic)
                O("{0}.{1} = new {1}();", ns, name);
            else
                O("{0}.{1} = {1};", ns, name);
        }

        public const string SpaceConstructorFunc = "casimodo.ui.createComponentSpace()";

        public string BuildJSGetOrCreateSpace(WebViewGenContext context, string constructor = SpaceConstructorFunc)
        {
            return BuildJSGetOrCreate(context.SpaceName, constructor);
        }

        public string BuildJSGetOrCreate(string name, string constructor)
        {
            return $"{name} || ({name} = {constructor})";
        }

        public string GetViewDirPath(MojViewConfig view)
        {
            return Path.Combine(App.Get<WebBuildConfig>().WebViewsDirPath, view.TypeConfig.PluralName);
        }

        public string BuildJsScriptFilePath(MojViewConfig view, string name = null, string suffix = null)
        {
            return Path.Combine(App.Get<WebBuildConfig>().WebViewsJavaScriptDirPath, BuildJsScriptFileName(view, name, suffix));
        }

        public string BuildJsScriptVirtualFilePath(MojViewConfig view, string name = null, string suffix = null)
        {
            return App.Get<WebBuildConfig>().WebViewsJavaScriptVirtualDirPath + "/" + BuildJsScriptFileName(view, name, suffix);
        }


        public string BuildJsClassName(MojViewConfig view)
        {
            var name = view.TypeConfig.Name;

            if (view.Group != null)
                name += "_" + view.Group + "_";

            var roles = view.Kind.Roles;
            string role = null;

            if (roles.HasFlag(MojViewRole.Lookup))
                role = "Lookup";
            else if (roles.HasFlag(MojViewRole.List))
                role = "List";
            else if (roles.HasFlag(MojViewRole.Details))
                role = "Details";
            else if (roles.HasFlag(MojViewRole.Editor))
                role = "Editor";
            else if (roles.HasFlag(MojViewRole.Delete))
                role = "Delete";

            if (role == null)
                throw new MojenException("Failed to build a JS component name.");

            name += role;

            return name;
        }

        public string BuildJsScriptFileName(MojViewConfig view, string name = null,
            string suffix = null, bool extension = true)
        {
            if (name == null)
            {
                name = view.TypeConfig.Name;

                if (view.Group != null)
                    name += "." + view.Group;

                var roles = view.Kind.Roles;
                string lookup = null;
                string role = null;

                if (roles.HasFlag(MojViewRole.Lookup))
                    lookup = "lookup";

                if (roles.HasFlag(MojViewRole.Editor))
                    role = "editor";
                else if (roles.HasFlag(MojViewRole.Details))
                    role = "details";
                else if (roles.HasFlag(MojViewRole.Delete))
                    role = "delete";
                else if (roles.HasFlag(MojViewRole.List))
                    role = "list";

                if (role == null)
                    throw new MojenException("Failed to build a JS file name.");

                name += "." + (lookup != null ? lookup + "." : "") + role;

                if (suffix != null)
                    name += suffix.FirstLetterToLower();
            }

            if (extension && !name.EndsWith(".js"))
                name += ".js";

            name = name.Split('.').Select(x => x.FirstLetterToLower()).Join(".");

            return name;
        }

        public string BuildEditorDataModelFileName(MojViewConfig view)
        {
            string name = "";

            if (view.Group != null)
                name = "_" + view.Group + ".";

            name += "EditorDataViewModel.cs";

            return name;
        }

        public string BuildFilePath(MojViewConfig view, string name = null, bool partial = false)
        {
            name = BuildFileName(view, name, partial: partial);

            return Path.Combine(GetViewDirPath(view), name);
        }

        public string BuildVirtualFilePath(MojViewConfig view, string name = null, string path = null, bool partial = false)
        {
            return $"~/Views/{view.TypeConfig.PluralName}/{BuildFileName(view, pathOrName: name, partial: partial)}";
        }

        public string BuildFileName(MojViewConfig view, string pathOrName = null, bool partial = false, bool extension = true)
        {
            partial = partial || view.IsPartial;

            string name = pathOrName;
            string path = null;

            if (pathOrName != null && pathOrName.Contains("/", @"\"))
            {
                name = Path.GetFileName(pathOrName);
                if (path != null)
                    path = Path.Combine(path, Path.GetDirectoryName(pathOrName));
                else
                    path = Path.GetDirectoryName(pathOrName);
            }

            if (name == null)
            {
                name = view.FileName ?? view.Name ?? view.Kind.RoleName ?? view.CustomControllerActionName;

                // NOTE: We now use the PluaralName for pages. I.e "People.cshtml" instead of "Index.cshtml".
                if (!partial && name == view.Kind.RawAction && view.IsPage)
                    name = view.TypeConfig.PluralName;
            }

            //// Must not have a file extension at this point.
            //if (!string.IsNullOrEmpty(name))
            //{
            //    if (!string.IsNullOrEmpty(Path.GetExtension(name)))
            //        throw new MojenException("The file extension must not be specified for view files.");
            //}

            if (view.Group != null && view.Group != name)
            {
                name = name ?? "";
                // Prepend group name.
                name = (name.StartsWith("_") ? "_" : "") + view.Group + (!string.IsNullOrEmpty(name) ? "." + name : "");
            }

            if (string.IsNullOrEmpty(name))
                throw new MojenException("Failed to computed the file name/path of the view.");

            // Ensure leading underscore for partial views.
            if (partial && !name.StartsWith("_"))
            {
                //if (pathOrName.Contains("/", @"\"))
                //{
                //    var steps = pathOrName.Split('/', '\\');
                //    if (!steps[steps.Length - 1].StartsWith("_"))
                //        pathOrName = steps.Take(pathOrName.Length - 1).Join("/") + "_" + steps.Last();
                //}
                //else
                //{
                name = "_" + name;
            }

            if (view.IsForMobile)
                name += ".Mobile";

            if (extension)
                name += ".cshtml";

            pathOrName = path != null
                ? Path.Combine(path, name).Replace(@"\", "/")
                : name;

            return pathOrName;
        }
    }
}
