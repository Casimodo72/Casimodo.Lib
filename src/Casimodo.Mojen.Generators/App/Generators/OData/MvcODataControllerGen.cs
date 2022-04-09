namespace Casimodo.Mojen
{
    public class MvcODataControllerGen : MvcControllerBaseGen
    {
        public override void GenerateControllerContent(MojControllerConfig controller)
        {
            foreach (var view in controller.GetPageViews())
            {
                // Index
                O();
                this.OMvcActionAuthAttribute(view);
                this.OOutputCacheAttribute();
                O($"public ActionResult {view.ControllerActionName}()");
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
                this.OMvcActionAuthAttribute(view);
                this.OOutputCacheAttribute();
                O($"public ActionResult {view.ControllerActionName}({view.Lookup.Parameters.ToMethodArguments()})");
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
                this.OMvcActionAuthAttribute(view);
                this.OOutputCacheAttribute();
                O($"public ActionResult {view.ControllerActionName}()");
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