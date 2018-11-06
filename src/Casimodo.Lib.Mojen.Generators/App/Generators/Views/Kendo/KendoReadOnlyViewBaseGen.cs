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

            XB("<div class='form-horizontal'{0}{1}>",
                GetViewCssStyle(context),
                GetViewHtmlId(context));
        }

        public override void EndView(WebViewGenContext context)
        {
            // KABU TODO: REMOVE
            //if (context.View.Template.IsEmpty)
            //    return;

            XE("</div>");
        }

        public override bool ORunBegin(WebViewGenContext context)
        {
            if (!base.ORunBegin(context))
                return false;

            XB($"<div class='{FormGroupClass}'>");

            return true;
        }

        public override bool ORunEnd(WebViewGenContext context)
        {
            if (!base.ORunEnd(context))
                return false;

            XE("</div>");

            return false;
        }

        public override void OProp(WebViewGenContext context)
        {
            var vprop = context.PropInfo.ViewProp;
            var dprop = context.PropInfo.TargetDisplayProp;

            CustomElemStyle(context);

            if (dprop.Type.IsBoolean)
            {
                O($"<span data-bind='yesno:{GetBinding(context)}'{GetElemAttrs()}></span>");
            }
            else if (dprop.Type.IsAnyTime)
            {
                string binder = "";
                if (dprop.Type.DateTimeInfo.IsDate) binder += "date";
                if (dprop.Type.DateTimeInfo.IsTime) binder += "time";
                O($"<span data-bind='{binder}:{GetBinding(context)}'{GetElemAttrs()}></span>");
            }
            else if (dprop.IsColor)
            {
                // Bind background-color to the value of the property.
                // http://demos.telerik.com/kendo-ui/mvvm/style
                O($"<div style='width: 30px' data-bind='style:{{backgroundColor:{GetBinding(context)}}}'{GetElemAttrs()}>&nbsp;</div>");
            }
            else if (dprop.Type.IsString && vprop.IsRenderedHtml)
            {
                O($"<span data-bind='textToHtml:{GetBinding(context)}'{GetElemAttrs()} style='white-space:pre'></span>");
            }
            else if (dprop.Type.IsMultilineString)
            {
                ElemClass("k-textbox");
                ElemStyleDefaultWidth();

                if (vprop.UseCodeRenderer != null)
                    ElemAttr("data-use-renderer", vprop.UseCodeRenderer);

                O($"<textarea data-bind='value:{GetBinding(context)}' readonly rows='{dprop.RowCount}'{GetElemAttrs()}></textarea>");
            }
            else if (dprop.Type.IsString && vprop.IsRenderedHtml)
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