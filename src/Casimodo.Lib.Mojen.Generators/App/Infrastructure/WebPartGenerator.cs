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

        public void PerformWrite(ControllerConfig controller, Action<ControllerConfig> callback)
        {
            string outputFilePath =
                Path.Combine(
                    App.Get<WebBuildConfig>().WebControllersOutputDirPath,
                    controller.ClassName + ".generated.cs");

            PerformWrite(outputFilePath, () => callback(controller));
        }

        public void OScriptReference(string path)
        {
            O($"<script src='{path}' type='text/javascript'></script>");
        }

        public void OScriptBegin()
        {
            OB("<script>");
            O("\"use strict\";");
        }

        public void OScriptEnd()
        {
            OE("</script>");
        }

        public void OJsDocReadyBegin()
        {
            OB("$(function ()");
        }

        public void OJsDocReadyEnd()
        {
            End(");");
        }

        public void OJsImmediateBegin(string parameters = "")
        {
            OB($"(function ({parameters})");
        }

        public void OJsImmediateEnd(string args = "")
        {
            End($")({args});");
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

        string BuildJsScriptFileName(MojViewConfig view, string name = null, string suffix = null)
        {
            if (name == null)
            {
                name = view.TypeConfig.Name;

                string role = null;
                var roles = view.Kind.Roles;
                if (roles.HasFlag(MojViewRole.Editor))
                    role = "editor";
                else if (roles.HasFlag(MojViewRole.Details))
                    role = "details";
                else if (roles.HasFlag(MojViewRole.Delete))
                    role = "delete";
                else if (roles.HasFlag(MojViewRole.List))
                    role = "list";

                if (roles.HasFlag(MojViewRole.Lookup))
                    role = "lookup" + (role != null ? "." + role : "");

                if (role == null)
                    throw new MojenException("Failed to build a JS file name.");

                name += "." + role;

                if (view.Group != null &&
                    !view.Group.Equals("lookup", StringComparison.OrdinalIgnoreCase))
                {
                    name += "." + view.Group.FirstLetterToLower();
                }

                if (suffix != null)
                    name += suffix.FirstLetterToLower();
            }

            if (!name.EndsWith(".js"))
                name += ".js";

            name = name.Split('.').Select(x => x.FirstLetterToLower()).Join(".");

            return name;
        }

        public string BuildViewModelFileName(MojViewConfig view)
        {
            if (view.Group != null)
                return "_" + view.Group + ".DataViewModel.cs";
            else
                return "DataViewModel.cs";
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
                name = view.FileName ?? view.Name ?? view.Kind.ComponentRoleName ?? view.Kind.ActionName;

                // NOTE: We now use the PluaralName for index pages. I.e "People.cshtml" instead of "Index.cshtml".
                if (!partial && name == view.Kind.ActionName && view.Kind.Roles.HasFlag(MojViewRole.Index))
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
                name = $"_{name}";
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
