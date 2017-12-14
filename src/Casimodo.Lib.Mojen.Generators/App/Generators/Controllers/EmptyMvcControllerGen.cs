using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class EmptyMvcControllerGen : MvcControllerBaseGen
    {
        public override void GenerateController(ControllerConfig controller)
        {
            // Index
            var view = controller.GetIndexViews().FirstOrDefault();
            if (view == null)
                return;

            O();
            O("public ActionResult Index()");
            Begin();

            string name = BuildFileName(view, extension: false);
            if (name != "Index")
                O($"return View(\"{name}\");");
            else
                O("return View();");

            End();
        }
    }
}