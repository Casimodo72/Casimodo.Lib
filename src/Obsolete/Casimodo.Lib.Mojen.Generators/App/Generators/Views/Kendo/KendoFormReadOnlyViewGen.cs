using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class KendoFormReadOnlyViewGen : KendoReadOnlyViewGenBase
    {
        public string ScriptFilePath { get; set; }

        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>()
                .Where(x => x.Uses(this)))
            {
                KendoGen.BindEditorView<KendoFormEditorViewGen>(view);
                KendoGen.BindCustomTagsEditorView(view);

                var context = KendoGen.InitComponentNames(new WebViewGenContext
                {
                    View = view,
                    ViewRole = "details",
                    IsViewIdEnabled = true

                });

                ScriptFilePath = BuildTsScriptFilePath(view, suffix: ".vm.generated");

                PerformWrite(view, () => GenerateView(context));

                if (view.Standalone.Is)
                {
                    PerformWrite(ScriptFilePath, () =>
                    {
                        KendoGen.OReadOnlyFormComponent(context);
                    });

                    RegisterComponent(context);
                }
            }
        }

        public override MojenGenerator Initialize(MojenApp app)
        {
            base.Initialize(app);
            KendoGen.SetParent(this);

            return this;
        }

        public override void Define(WebViewGenContext context)
        {
            base.Define(context);
            DataViewModelAccessor = "item.";
        }

        public override void BeginView(WebViewGenContext context)
        {
            ORazorGeneratedFileComment();

            ORazorUsing("Casimodo.Lib.Web", context.View.TypeConfig.Namespace);

            ORazorModel(context.View.TypeConfig);

            if (context.View.Standalone.Is)
            {
                XB("<div class='standalone-details-view'{0}>",
                    GetViewHtmlId(context));

                // Toolbar
                XB("<div class='details-view-toolbar'>");

                // Title in toolbar.
                O("<div class='details-view-title'></div>");

                // Command buttons.
                XB("<div class='details-view-commands'>");

                foreach (var command in context.View.CustomCommands)
                {
                    O($"<button type='button' class='k-button btn custom-command' data-command-name='{command.Name}'>{command.DisplayName}</button>");
                }

                // Edit button
                if (context.View.EditorView != null && context.View.EditorView.CanModify)
                {
                    // NOTE: The button is hidden intially.
                    //   The view model will show or remove that button based on activity authorization.
                    O("<button type='button' class='k-button btn edit-command' style='display:none'><span class='k-icon k-edit'></span></button>");
                }

                if (context.View.IsReloadable)
                {
                    // Refresh button
                    O("<button type='button' class='k-button btn refresh-command'><span class='k-icon k-i-refresh'></span></button>");
                }

                XE("</div>"); // Commands

                XE("</div>"); // Toolbar

                XB("<div class='details-view-content'>");

                XB($"<div class='{FormClass}'{GetStyleAttr(GetViewStyles(context))}>");
            }
            else
            {
                XB($"<div class='{FormClass}'{GetStyleAttr(GetViewStyles(context))}{GetViewHtmlId(context)}>");
            }
        }

        public override void EndView(WebViewGenContext context)
        {
            base.EndView(context);

            if (context.View.Standalone.Is)
            {
                XE($"</div>"); // standalone details view content
                XE($"</div>"); // standalone details view
            }
        }

        public override void OProp(WebViewGenContext context)
        {
            var vitem = context.PropInfo;
            var prop = vitem.Prop;
            var propPath = vitem.PropPath;

            if (prop.Reference.Is || prop.FileRef.Is || prop.Type.IsEnum)
            {
                throw new MojenException("No references and enums allowed in read-only views.");
            }
            else
            {
                base.OProp(context);
            }
        }

        void OMvcAttrs(WebViewGenContext context, bool kendo)
        {
            if (!kendo)
                ElemDataBindAttr(context);

            OMvcAttrs(kendo);
        }
    }
}