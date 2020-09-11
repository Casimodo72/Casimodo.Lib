namespace Casimodo.Lib.Mojen
{
    public abstract class KendoReadOnlyViewGenBase : KendoTypeViewGenBase
    {
        public string FormClass { get; set; } = "form-horizontal km-form-readonly";

        public override void Define(WebViewGenContext context)
        {
            base.Define(context);

            FormGroupClass = "form-group readonly";

            OLabelContainerBegin = c =>
            {
                ElemClass(LabelContainerClass, target: "label");
            };

            OPropContainerBegin = c =>
            {
                XB($"<div class='{PropContainerClass}'>");
                ElemClass("km-readonly-form-control");
            };
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

            XB($"<div class='{FormClass}'{GetStyleAttr(GetViewStyles(context))}{GetViewHtmlId(context)}>");
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

        public override void OPropLabel(WebViewGenContext context)
        {
            var vitem = context.PropInfo;

            ElemClass(LabelClass, target: "label");

            // TODO: REMOVE? Oo($"<label for='{vitem.PropPath}' class='{GetElemAttrs("label")}'>");
            Oo($"<label{GetElemAttrs("label")}>");

            o(GetDisplayNameFor(context));

            oO("</label>");
        }

        public override void OProp(WebViewGenContext context)
        {
            OReadOnlyProp(context);
        }

        public void OReadOnlyProp(WebViewGenContext context, string binding = null)
        {
            var vprop = context.PropInfo.ViewProp;
            var prop = context.PropInfo.TargetDisplayProp;
            binding = binding ?? GetBinding(context);

            CustomElemStyle(context);

            if (vprop.Width != null)
                ElemStyle($"width:{vprop.Width}px !important");
            if (vprop.MaxWidth != null)
                ElemStyle($"max-width:{vprop.MaxWidth}px !important");

            if (prop.Type.IsBoolean)
            {
                O($"<span data-bind='yesno:{binding}'{GetElemAttrs()}></span>");
            }
            else if (prop.Type.IsAnyTime)
            {
                string binder = "";
                if (prop.Type.DateTimeInfo.IsDate) binder += "date";
                if (prop.Type.DateTimeInfo.IsTime) binder += "time";
                O($"<span data-bind='{binder}:{binding}'{GetElemAttrs()}></span>");
            }
            else if (prop.IsColor)
            {
                // Bind background-color to the value of the property.
                // http://demos.telerik.com/kendo-ui/mvvm/style
                O($"<div style='width: 30px' data-bind='style:{{backgroundColor:{binding}}}'{GetElemAttrs()}>&nbsp;</div>");
            }
            else if (prop.Type.IsString && vprop.IsRenderedHtml)
            {
                O($"<span data-bind='textToHtml:{binding}'{GetElemAttrs()} style='white-space:pre'></span>");
            }
            else if (prop.Type.IsMultilineString)
            {
                ElemClass("k-textbox");
                ElemStyleDefaultWidth();

                if (vprop.UseCodeRenderer != null)
                    ElemAttr("data-use-renderer", vprop.UseCodeRenderer);

                O($"<textarea data-bind='value:{binding}' readonly rows='{vprop.RowCount}'{GetElemAttrs()}></textarea>");
            }
            else if (prop.Type.IsString && vprop.IsRenderedHtml)
            {
                O($"<span data-bind='textToHtml:{binding}'{GetElemAttrs()} style='white-space:pre'></span>");
            }
            else
            {
                O($"<span data-bind='text:{binding}'{GetElemAttrs()}></span>");
            }

            // TODO: Support TimeSpan
        }
    }
}