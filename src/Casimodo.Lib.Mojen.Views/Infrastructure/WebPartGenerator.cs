using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Casimodo.Lib;

namespace Casimodo.Lib.Mojen
{
    public static class MvcExtensions
    {
        public static string GetVirtualFilePath(this MojViewConfig view, bool isFullRequired = false)
        {
            if (!string.IsNullOrEmpty(view.VirtualFilePath))
                return view.VirtualFilePath;

            return view.BuildFileName(extension: false);
        }

        public static string BuildFileName(this MojViewConfig view, string pathOrName = null, bool partial = false, bool extension = true)
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
                name = view.FileName ?? view.Name ?? view.MainRoleName ?? view.CustomControllerActionName;

                // NOTE: We now use the PluaralName for pages. I.e "People.cshtml" instead of "Index.cshtml".
                if (!partial && view.IsPage && name == view.MainRoleName)
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

        public static void OMvcActionAuthAttribute(this WebPartGenerator gen, MojViewConfig view)
        {
            if (view.IsAuthEnabled)
            {
                gen.O("[MvcActionAuth(Part = \"{0}\", Group = {1}, VRole = \"{2}\")]",
                    view.GetPartName(),
                    Moj.CS(view.Group),
                    view.MainRoleName);
            }
        }

        public static void OOutputCacheAttribute(this WebPartGenerator gen)
        {
            if (gen.WebConfig.OutputCache.IsEnabled)
            {
                // This attribute is located in Casimodo.Lib.Web
                if (gen.App.IsDotNetCore())
                {
                    gen.O("[CustomResponseCache(CacheProfileName = \"{0}\"{1})]",
                        gen.WebConfig.OutputCache.CacheProfile,
                        gen.WebConfig.OutputCache.Revalidate ? ", Revalidate = true" : "");
                }
                else
                {
                    gen.O("[CustomOutputCache(CacheProfile = \"{0}\"{1})]",
                        gen.WebConfig.OutputCache.CacheProfile,
                        gen.WebConfig.OutputCache.Revalidate ? ", Revalidate = true" : "");
                }
            }
        }
    }

    public abstract class WebPartGenerator : AppPartGenerator
    {
        public WebPartGenerator()
        {
            ClassGen = AddSub<JsClassGen>();
            ClassGen.SetParent(this);
        }

        public JsClassGen ClassGen { get; private set; }

        public WebAppBuildConfig WebConfig { get; set; }

        public override void Prepare()
        {
            base.Prepare();
            WebConfig = App.Get<WebAppBuildConfig>();
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

            App.Get<WebResultBuildInfo>().Components.Add(new WebResultComponentInfo
            {
                View = view
            });
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

        //protected void WriteViewBagPageTitle(MojViewConfig view)
        //{
        //    var title = view.GetDefaultTitle();
        //    if (!string.IsNullOrEmpty(title))
        //        O("ViewBag.Title = \"{0}\";", title);
        //}

        protected void WriteViewBagMessage(MojViewConfig view)
        {
            if (!string.IsNullOrEmpty(view.Message))
                O("ViewBag.Message = \"{0}\";", view.Message);
        }

        // KABU TODO: ELIMINATE
        public virtual string GetWebRepositoryName(MojType type)
        {
            return type.PluralName + (type.Kind == MojTypeKind.Model ? "Model" : "") + "WebRepository";
        }

        public void OScriptReference(string path, bool async = false)
        {
            O("<script src='{0}' type='text/javascript'{1}></script>",
                path, (async ? " async" : ""));
        }

        public void OMvcScriptBegin()
        {
            XB("<script>");
            OScriptUseStrict();
        }

        public void OScriptUseStrict()
        {
            O("\"use strict\";");
        }

        public void OMvcScriptEnd()
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

        public void OTsNamespace(string ns, Action<MojNamespaceContext> action)
        {
            OTsNamespaceCore(new MojNamespaceContext(ns), action);
        }

        public void OTsNamespaceCore(MojNamespaceContext context, Action<MojNamespaceContext> action)
        {
            while (context.Next())
            {
                if (context.Previous != null)
                    throw new MojenException("Only one namespace level is supported for TypeScript.");

                var absoluteNs = context.Previous != null ? context.Previous + "." + context.Current : "window[\"" + context.Current + "\"]";
                OB($"namespace {context.Current}");
                O();

                action?.Invoke(context);

                End();
            }
        }


        public void OJsNamespace(string ns, Action<MojNamespaceContext> action)
        {
            OJsNamespaceCore(new MojNamespaceContext(ns), action);
        }

        public void OJsNamespaceCore(MojNamespaceContext context, Action<MojNamespaceContext> action)
        {
            while (context.Next())
            {
                // KABU TODO: REMOVE
                //if (context.Index == 0)
                //    O($"var {context.Current};");

                var absoluteNs = context.Previous != null ? context.Previous + "." + context.Current : "window." + context.Current;
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

        public void OJsOnPageReady(Action content)
        {
            // This uses jQuery.
            OB($"$(function ()");

            content?.Invoke();

            End(");");
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

        public void OImmediateInvoked(Action content)
        {
            OB($"(function ()");
            content();
            End(")();");
        }

        public void OJQueryOnDocReady(Action content)
        {
            OBegin($@"$(function ()", content, ");");
        }

        // KABU TODO: Find a ways to share JS methods with DataLayerGenerator.
        // KABU TODO: REMOVE? Not used.
        //public void OJsClass(string name, bool isstatic = false, string extends = null,
        //    string args = null, Action content = null)
        //{
        //    OJsClass(App.Get<DataLayerConfig>().ScriptNamespace, name, isstatic, extends, args, content);
        //}

        public string BuildJSGetOrCreate(string name, string constructor)
        {
            return $"{name} || ({name} = {constructor})";
        }

        public string GetViewDirPath(MojViewConfig view)
        {
            return Path.Combine(App.Get<WebAppBuildConfig>().WebMvcViewDirPath, view.TypeConfig.PluralName);
        }

        public string BuildTsScriptFilePath(MojViewConfig view, string name = null, string part = null, string suffix = null)
        {
            return Path.Combine(App.Get<WebAppBuildConfig>().WebViewsTypeScriptDirPath, BuildTsScriptFileName(view, name, part: part, suffix: suffix));
        }

        //public string BuildJsScriptFilePath(MojViewConfig view, string name = null, string part = null, string suffix = null)
        //{
        //    return Path.Combine(App.Get<WebAppBuildConfig>().WebViewsJavaScriptDirPath, BuildJsScriptFileName(view, name, part: part, suffix: suffix));
        //}

        // KABU TODO: REMOVE? Not used
        public string BuildJsScriptVirtualFilePath(MojViewConfig view, string name = null, string part = null, string suffix = null)
        {
            return App.Get<WebAppBuildConfig>().WebViewsJavaScriptVirtualDirPath + "/" + BuildJsScriptFileName(view, name, part: part, suffix: suffix);
        }


        public string BuildJsClassName(MojViewConfig view)
        {
            var name = view.TypeConfig.Name;

            if (view.Group != null)
                name += "_" + view.Group + "_";

            name += view.MainRoleName;

            return name;
        }

        public string BuildTsScriptFileName(MojViewConfig view, string name = null,
            string suffix = null, string part = null, bool extension = true)
        {
            return BuildScriptFileNameCore(view, "ts", name, suffix, part, extension);
        }

        public string BuildJsScriptFileName(MojViewConfig view, string name = null,
            string suffix = null, string part = null, bool extension = true)
        {
            return BuildScriptFileNameCore(view, "js", name, suffix, part, extension);
        }

        string BuildScriptFileNameCore(MojViewConfig view, string scriptType, string name = null,
            string suffix = null, string part = null, bool extension = true)
        {
            if (name == null)
            {
                name = view.TypeConfig.Name;

                if (view.Group != null)
                    name += "." + view.Group;

                name += "." + (part ?? "") + view.MainRoleName.ToLower();

                // KABU TODO: ELIMINATE: Currently we need a hack to compensate for
                //   the issue that lookup views are also lists.
                //   There should be two separate views instead: one lookup view and its
                //   content would be the list view
                if (view.IsLookup)
                    name += ".list";

                if (suffix != null)
                    name += suffix.FirstLetterToLower();
            }

            if (extension && !name.EndsWith("." + scriptType))
                name += "." + scriptType;

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
            name = view.BuildFileName(name, partial: partial);

            return Path.Combine(GetViewDirPath(view), name);
        }

        public string BuildVirtualFilePath(MojViewConfig view, string name = null, string path = null, bool partial = false)
        {
            return $"~/Views/{view.TypeConfig.PluralName}/{view.BuildFileName(pathOrName: name, partial: partial)}";
        }


    }
}
