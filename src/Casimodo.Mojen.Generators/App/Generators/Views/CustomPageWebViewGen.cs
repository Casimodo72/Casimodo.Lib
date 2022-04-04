namespace Casimodo.Lib.Mojen
{
    public partial class CustomPageWebViewGen : WebViewGenerator
    {
        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>().Where(x => x.Uses(this)))
            {
                if (!view.IsCustom) throw new MojenException("This view must be custom.");
                if (!view.IsPage) throw new MojenException("This view must have a page role.");

                var context = new WebViewGenContext
                {
                    View = view
                };
                RegisterComponent(context);
            }
        }
    }

    public partial class CustomViewWebViewGen : WebViewGenerator
    {
        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>().Where(x => x.Uses(this)))
            {
                if (!view.IsCustom) throw new MojenException("This view must be custom.");

                var context = new WebViewGenContext
                {
                    View = view
                };
                RegisterComponent(context);
            }
        }
    }
}