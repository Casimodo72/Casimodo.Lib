using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    // Routing to controller actions in ASP.NET Core:
    //   https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/routing?view=aspnetcore-2.1

    public class CoreODataMvcControllerGen : MvcControllerBaseGen
    {
        protected override string[] GetNamespaces(MojControllerConfig controller)
        {
            return new string[]
            {
                "System", "System.Linq", "System.Collections.Generic","System.Threading.Tasks",
                "Casimodo.Lib", "Casimodo.Lib.Web", "Casimodo.Lib.Web.Auth",
                "Microsoft.AspNetCore.Mvc",
                "Microsoft.AspNetCore.Authorization", // For auth attributes               
                controller.TypeConfig.Namespace
            };
        }

        protected override void OControllerAttrs(MojControllerConfig controller)
        {
            O($@"[Route(""{controller.TypeConfig.PluralName}"")]");
        }

        string GetActionResult()
        {
            return "IActionResult"; // return "async Task<IActionResult>";
        }

        void OControllerActionAttrs(MojControllerConfig controller, MojViewConfig view)
        {
            var name = view.ControllerActionName;

            // Routing to controller actions: https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/routing?view=aspnetcore-2.1
            if (name == "Index")
                O($@"[Route(""""), Route(""{name}"")]");
            else
                O($@"[Route(""{name}"")]");

            this.OMvcActionAuthAttribute(view);
            this.OOutputCacheAttribute();
        }

        public override void GenerateControllerContent(MojControllerConfig controller)
        {
            foreach (var view in controller.GetPageViews().Where(x => !x.IsCustomControllerMethod))
            {
                // Index
                O();
                OControllerActionAttrs(controller, view);
                O($"public {GetActionResult()} {view.ControllerActionName}()");
                Begin();

                if (view.IsGlobalCompanyFilterEnabled == false)
                    O("ViewBag.HideGlobalCompanyFilter = true;");

                string path = view.GetVirtualFilePath();
                if (path != "Index")
                    O($"return View(\"{path}\");");
                else
                    O("return View();");

                End();
            }

            // Lookup actions.
            foreach (var view in controller.Views.Where(x => x.Lookup.Is))
            {
                O();
                OControllerActionAttrs(controller, view);
                O($"public {GetActionResult()} {view.ControllerActionName}({view.Lookup.Parameters.ToMethodArguments()})");
                Begin();

                foreach (var prop in view.Lookup.Parameters)
                {
                    O($"ViewBag.{prop.Name} = {prop.VName};");
                }

                var method = view.IsPartial ? "PartialView" : "View";

                O($"return {method}(\"{view.GetVirtualFilePath()}\");");

                End();
            }

            // Standalone lists, details and editors.
            foreach (var view in controller.Views.Where(x => x.Standalone.Is))
            {
                O();
                OControllerActionAttrs(controller, view);
                O($"public {GetActionResult()} {view.ControllerActionName}()");
                Begin();

                var method = view.IsPartial ? "PartialView" : "View";

                O($"return {method}(\"{view.GetVirtualFilePath()}\");");

                End();
            }

            // Let IMvcActionInjectors inject further MVC actions.
            foreach (var view in controller.Views)
            {
                foreach (var gen in App.Generators.OfType<IMvcActionInjector>())
                    gen.GenerateMvcActionFor(this, view);
            }

            // KABU REVISIT: Dispose anything?
        }
    }
}