namespace Casimodo.Lib.Mojen
{
    public partial class PageWithKendoGridContentViewGen : KendoViewGenBase
    {
        protected override void GenerateCore()
        {
            foreach (var view in App.GetItems<MojControllerViewConfig>().Where(x => x.Uses(this)))
            {
                if (view.IsCustom) throw new MojenException("This view must not be custom.");
                if (!view.IsPage) throw new MojenException("This view must have a page role.");

                // Bind content views.
                KendoGen.BindPageContentView(view, MojViewRole.List);

                var context = new WebViewGenContext
                {
                    View = view
                };

                PerformWrite(context.View, () =>
                {
                    var grid = view.ContentViews.First(x => x.Uses<KendoGridViewGen>());

                    string gridVirtualFilePath = BuildVirtualFilePath(grid);

                    ORazorGeneratedFileComment();

                    var title = view.Title ?? grid.GetDefaultTitle();
                    if (!string.IsNullOrEmpty(title))
                        O($"@{{ ViewBag.Title = \"{title}\"; }}");

                    OMvcPartialView(gridVirtualFilePath);

                    OMvcScriptBegin();
                    OJsOnPageReady(() =>
                    {
                        O($"cmodo.componentRegistry.get({Quote(grid.Id)}).create(true).processNavigation().refresh();");
                    });
                    OMvcScriptEnd();

                    ORazorStyleSection(() =>
                    {
                        O("<style>");
                        O("    #site-page-container { overflow-y: auto; }");
                        O("</style>");
                    });
                });

                RegisterComponent(context);
            }
        }
    }
}