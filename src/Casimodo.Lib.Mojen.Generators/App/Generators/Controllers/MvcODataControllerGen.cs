using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class MvcODataControllerGen : MvcControllerBaseGen
    {
        public override void GenerateController(MojControllerConfig controller)
        {
            foreach (var view in controller.GetPageViews())            
            {
                // Index
                O();
                OOutputCacheAttribute();
                O($"public ActionResult {view.ControllerActionName}()"); // Index{view.Group}
                Begin();

                string name = BuildFileName(view, extension: false);
                if (name != "Index")
                    O($"return View(\"{name}\");");
                else
                    O("return View();");

                End();
            }

            // Lookup actions.
            foreach (var view in controller.Views.Where(x => x.Lookup.Is))
            {
                O();
                OOutputCacheAttribute();
                O($"public ActionResult {view.ControllerActionName}({view.Lookup.Parameters.ToMethodArguments()})");
                Begin();

                foreach (var prop in view.Lookup.Parameters)
                {
                    O($"ViewBag.{prop.Name} = {prop.VName};");
                }

                var method = view.IsPartial ? "PartialView" : "View";

                O($"return {method}(\"{BuildFileName(view, extension: false)}\");");

                End();
            }

            // Standalone lists and editors.
            foreach (var view in controller.Views.Where(x => x.Standalone.Is))
            {
                O();
                OOutputCacheAttribute();
                O($"public ActionResult {view.ControllerActionName}()");
                Begin();

                var method = view.IsPartial ? "PartialView" : "View";

                O($"return {method}(\"{BuildFileName(view, extension: false)}\");");

                End();
            }

            // KABU REVISIT: Dispose anything?
        }
    }
}