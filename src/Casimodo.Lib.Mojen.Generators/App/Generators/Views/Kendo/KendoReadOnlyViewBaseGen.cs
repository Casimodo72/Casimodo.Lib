using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    public abstract class KendoReadOnlyViewGenBase : KendoTypeViewGenBase
    {
        public override void Define(WebViewGenContext context)
        {
            base.Define(context);

            FormGroupClass = "form-group readonly";
        }

        public override void BeginView(WebViewGenContext context)
        {
            ORazorGeneratedFileComment();

            if (context.View.Template.IsEmpty)
                return;

            ORazorModel(context.View.TypeConfig);

            // See http://www.c-sharpcorner.com/UploadFile/6c9ca8/using-kendo-ui-templates/
            // Render values as HTML: #= #
            // Uses HTML encoding to display values: #: #
            // Execute arbitrary JavaScript code: # if(...){# ... #}#            

            OB($"<div class='form-horizontal'{GetViewCssStyle(context)}>");
        }

        public override void EndView(WebViewGenContext context)
        {
            if (context.View.Template.IsEmpty)
                return;

            OE("</div>");
        }

        public override void ORunBegin(WebViewGenContext context)
        {
            OB($"<div class='{FormGroupClass}'>");
        }

        public override void ORunEnd(WebViewGenContext context)
        {
            OE("</div>");
        }

        // KABU TODO: REMOVE:
        //public string GetGenericPropArgs(WebViewGenContext context)
        //{
        //    return $"<{context.View.TypeConfig.RequiredStore.ClassName}>"; // KABU TODO: REMOVE: , {context.PropInfo.Prop.Type.Name}>";
        //}

        public override void OPropLabel(WebViewGenContext context)
        {
            var vitem = context.PropInfo;

            Oo($"<label for='{vitem.PropPath}' class='{LabelClass}'>");

            if (vitem.CustomDisplayLabel != null)
                o(vitem.CustomDisplayLabel);
            else
                o($"@(Html.DisplayNameFor(x => x.{vitem.PropPath}))");

            oO("</label>");
        }

        public override void OProp(WebViewGenContext context)
        {
            var vprop = context.PropInfo.ViewProp;
            var prop = context.PropInfo.Prop;

            CustomElemStyle(context);

            if (prop.Type.IsBoolean)
            {
                O($"<span data-bind='yesno:{GetBinding(context)}'{GetElemAttrs()}></span>");
            }
            else if (prop.Type.IsAnyTime)
            {
                string binder = "";
                if (prop.Type.DateTimeInfo.IsDate) binder += "date";
                if (prop.Type.DateTimeInfo.IsTime) binder += "time";
                O($"<span data-bind='{binder}:{GetBinding(context)}'{GetElemAttrs()}></span>");
            }
            else if (prop.IsColor)
            {
                // Bind background-color to the value of the property.
                // http://demos.telerik.com/kendo-ui/mvvm/style
                O($"<div style='width: 30px' data-bind='style:{{backgroundColor:{GetBinding(context)}}}'{GetElemAttrs()}>&nbsp;</div>");
            }
            else if (prop.Type.IsMultilineString)
            {
                //AddClassAttr("form-control");
                ElemClass("k-textbox");
                ElemStyleDefaultWidth();
                // KABU TODO: IMPORTANT: How to avoid using a textarea?
                O($"<textarea data-bind='value:{GetBinding(context)}' readonly rows='{prop.RowCount}'{GetElemAttrs()}></textarea>");
            }
            else if (prop.Type.IsString && vprop.IsDisplayedAsHtml)
            {
                O($"<span data-bind='textToHtml:{GetBinding(context)}'{GetElemAttrs()} style='white-space:pre'></span>");
            }
            else
            {
                O($"<span data-bind='text:{GetBinding(context)}'{GetElemAttrs()}></span>");
            }

            // KABU TODO: Support TimeSpan
        }
    }
}