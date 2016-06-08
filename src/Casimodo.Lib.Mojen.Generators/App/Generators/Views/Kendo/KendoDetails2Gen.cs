using Casimodo.Lib.Data;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class KendoDetails2Gen : KendoReadOnlyViewGenBase
    {
        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>()
                .Where(x => x.Uses(this)))
            {
                PerformWrite(view, () => GenerateView(new WebViewGenContext
                {
                    View = view
                }));
            }
        }

        public override void Define(WebViewGenContext context)
        {
            base.Define(context);
            DataViewModelAccessor = "item.";
        }

        public override void BeginView(WebViewGenContext context)
        {
            if (context.View.Template.IsEmpty)
                return;

            ORazorGeneratedFileComment();

            ORazorUsing("Casimodo.Lib.Web", context.View.TypeConfig.Namespace);

            ORazorModel(context.View.TypeConfig);

            OB($"<div class='form-horizontal'{GetViewCssStyle(context)}> ");
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