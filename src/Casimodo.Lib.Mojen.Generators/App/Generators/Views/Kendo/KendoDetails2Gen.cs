using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class KendoFormReadOnlyViewGen : KendoReadOnlyViewGenBase
    {
        public string ScriptFilePath { get; set; }
        public string ScriptVirtualFilePath { get; set; }

        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>()
                .Where(x => x.Uses(this)))
            {
                KendoGen.ImplicitelyBindEditorView<KendoFormEditorViewGen>(view);

                var context = KendoGen.InitComponentNames(new WebViewGenContext
                {
                    View = view,
                    ViewRole = "details",
                    IsViewIdEnabled = true

                });

                ScriptFilePath = BuildJsScriptFilePath(view, suffix: ".vm.generated");
                ScriptVirtualFilePath = BuildJsScriptVirtualFilePath(view, suffix: ".vm.generated");

                PerformWrite(view, () => GenerateView(context));

                if (view.Standalone.Is)
                {
                    PerformWrite(ScriptFilePath, () =>
                    {
                        OScriptUseStrict();

                        KendoGen.OReadOnlyViewModel(context);
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

            if (context.View.Template.IsEmpty)
                return;

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

                // Edit button
                if (context.View.EditorView != null && context.View.EditorView.CanEdit)
                {
                    // NOTE: The button is hidden intially.
                    //   The view model will show or remove that button based on activity authorization.
                    O("<button type='button' class='k-button btn edit-command' style='display:none'><span class='k-icon k-edit'></span></button>");
                }

                // Refresh button
                O("<button type='button' class='k-button btn refresh-command'><span class='k-icon k-i-refresh'></span></button>");
                
                XE("</div>"); // Commands

                XE("</div>"); // Toolbar

                XB("<div class='details-view-content'>");

                XB($"<div class='form-horizontal'{GetViewCssStyle(context)}> ");
            }
            else
            {
                XB("<div class='form-horizontal'{0}{1}>",
                    GetViewCssStyle(context),
                    GetViewHtmlId(context));
            }
        }

        public override void EndView(WebViewGenContext context)
        {
            if (context.View.Template.IsEmpty)
                return;

            base.EndView(context);

            if (context.View.Standalone.Is)
            {
                XE($"</div>"); // standalone details view content
                XE($"</div>"); // standalone details view
            }

            // KABU TODO: REMOVE: OScriptReference(ScriptVirtualFilePath, async: true);
        }

        public override void OProp(WebViewGenContext context)
        {
            var vitem = context.PropInfo;
            var prop = vitem.Prop;
            var propPath = vitem.PropPath;

            CustomElemStyle(context);

            if (prop.FileRef.Is)
            {
                // Image thumbnail.
                if (prop.FileRef.IsImage)
                {
                    // Image-thumbnail
                    O($"<img alt='' data-bind='attr:{{src:{GetBinding(context, alias: true)}Uri}}{GetElemAttrs()}'/>");
                }
            }
            else if (prop.Reference.IsToOne)
            {
                // KABU TODO: IMPORTANT: How to use the DropDownList in MVVM scenarios?

                string key = "Value", display = "Text";
                string nullable = MojenUtils.ToCsValue(prop.Type.IsNullableValueType);
                // DropDownList
                // See http://demos.telerik.com/aspnet-mvc/dropdownlist/index
                // KABU TODO: REMOVE: Oo($"@(Html.Kendo().DropDownListFor{GetGenericPropArgs(context)}(m => m.{propPath})" +
                Oo($"@(Html.Kendo().DropDownListFor(m => m.{propPath})" +
                    $".Name(\"{propPath}\")" +
                    $".DataValueField(\"{key}\")" +
                    $".DataTextField(\"{display}\")" +
                    $".ValuePrimitive(true)" +
                    $".BindTo(PickItemsContainer.Get{prop.Reference.ToType.PluralName}(nullable: {nullable}))");

                OMvcAttrs(context, true);
                oO(")");
            }
            else if (prop.Type.IsEnum)
            {
                // KABU TODO: IMPORTANT: How to use the DropDownList in MVVM scenarios?

                string key = "Value", display = "Text";
                string nullable = MojenUtils.ToCsValue(prop.Type.IsNullableValueType);

                // DropDownList
                // See http://demos.telerik.com/aspnet-mvc/dropdownlist/index
                // KABU TODO: REMOVE: Oo($"@(Html.Kendo().DropDownListFor{GetGenericPropArgs(context)}(m => m.{propPath})" +
                Oo($"@(Html.Kendo().DropDownListFor(m => m.{propPath})" +
                    $".Name(\"{propPath}\")" +
                    $".DataValueField(\"{key}\")" +
                    $".DataTextField(\"{display}\")" +
                    $".ValuePrimitive(true)" +
                    $".BindTo(PickItemsHelper.ToSelectList<{prop.Type.NameNormalized}>(nullable: {nullable}, names: true))");

                OMvcAttrs(context, true);
                oO(")");
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