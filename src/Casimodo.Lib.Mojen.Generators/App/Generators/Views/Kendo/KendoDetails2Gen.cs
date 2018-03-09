using Casimodo.Lib.Data;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class KendoDetails2Gen : KendoReadOnlyViewGenBase
    {
        public KendoPartGen KendoGen { get; set; } = new KendoPartGen();

        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>()
                .Where(x => x.Uses(this)))
            {
                var context = new WebViewGenContext
                {
                    View = view
                };

                PerformWrite(view, () => GenerateView(context));

                if (view.Standalone.Is)
                {
                    var path = BuildJsScriptFilePath(view, suffix: ".vm.generated", newConvention: true);
                    var componentName = view.TypeConfig.Name.FirstLetterToLower() + (view.Group ?? "") + "DetailsSpace";
                    PerformWrite(path, () =>
                    {
                        OScriptUseStrict();

                        KendoGen.OStandaloneEditableDetailsViewModel(context, componentName);
                    });
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
                OB($"<div class='standalone-details-view' id='view-{context.View.Id}'>");

                // Toolbar
                OB("<div class='details-view-toolbar'>");

                // Title in toolbar.
                O("<div class='details-view-title'></div>");

                // Command buttons.
                OB("<div class='details-view-commands'>");

                // Edit button
                if (context.View.CanEdit)
                {
                    // NOTE: The button is hidden intially.
                    //   The view model will show or remove that button based on activity authorization.
                    O("<button type='button' class='k-button btn edit-command' style='display:none'><span class='k-icon k-edit'></span></button>");
                }

                // Refresh button
                O("<button type='button' class='k-button btn refresh-command'><span class='k-icon k-i-refresh'></span></button>");

                OE("</div>"); // Commands

                OE("</div>"); // Toolbar

                OB("<div class='details-view-content'>");
            }

            OB($"<div class='form-horizontal'{GetViewCssStyle(context)}> ");
        }

        public override void EndView(WebViewGenContext context)
        {
            if (context.View.Template.IsEmpty)
                return;

            base.EndView(context);

            if (context.View.Standalone.Is)
            {
                OE($"</div>"); // standalone details view content
                OE($"</div>"); // standalone details view
            }
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