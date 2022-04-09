namespace Casimodo.Mojen
{
    public class EmptyMvcControllerGen : MvcControllerBaseGen
    {
        public override void GenerateControllerContent(MojControllerConfig controller)
        {
            // Index
            var view = controller.GetPageViews().FirstOrDefault();
            if (view == null)
                return;

            O();
            O("public ActionResult Index()");
            Begin();

            string name = view.BuildFileName(extension: false);
            if (name != "Index")
                O($"return View(\"{name}\");");
            else
                O("return View();");

            End();
        }
    }
}