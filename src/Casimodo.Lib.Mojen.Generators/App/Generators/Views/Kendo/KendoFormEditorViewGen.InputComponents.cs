using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoFormEditorViewGen : KendoTypeViewGenBase
    {
        void OKendoNumericInput(WebViewGenContext context, string ppath = null, int? min = null, int? max = null,
            bool validationElem = true)
        {
            var vprop = context.PropInfo.ViewProp;
            var dprop = context.PropInfo.TargetDisplayProp;
            ppath = ppath ?? context.PropInfo.PropPath;

            min = min ?? dprop.Rules.Min;
            max = max ?? dprop.Rules.Max;

            Oo("<input");
            oAttr("data-role", "numerictextbox");
            oAttr("id", ppath);
            oAttr("name", ppath);
            oAttr("type", "number");
            oAttr("data-type", "number");
            oAttr("data-display-name", vprop.DisplayLabel);
            OHtmlDataBindValue(context, ppath);

            string format = "";
            int? decimals = 0;
            bool spinners = false;
            if (dprop.Type.AnnotationDataType == DataType.Currency)
            {
                format = "#.##";
                decimals = 2;
                oAttr("step", "0.01");
            }
            else if (dprop.Type.IsDecimal)
            {

                // Format: http://docs.telerik.com/kendo-ui/framework/globalization/numberformatting
                // Format: http://stackoverflow.com/questions/15241603/formatting-kendo-numeric-text-box
                format = "#.##"; // "{0:#.##}";
                decimals = dprop.Attrs.Find<MojPrecisionAttr>()?.Scale ?? 2;
                oAttr("step", "any");
            }
            else
            {
                // Format: http://docs.telerik.com/kendo-ui/framework/globalization/numberformatting
                // Format: http://stackoverflow.com/questions/15241603/formatting-kendo-numeric-text-box
                format = "#"; // "{0:#.##}";
                decimals = 0;
                spinners = true;
            }

            oAttr("data-decimals", decimals.Value);
            // KABU TODO: IMPORTANT: KENDO: restrictDecimals is not implemented in our Kendo version.
            //   It was added in version 2016.2.504.
            //   See https://www.telerik.com/forums/restrictdecimals-option-not-working
            oAttr("data-restrict-decimals", "true");
            oAttr("data-format", format);
            oAttr("data-spinners", Moj.JS(spinners));
            OHtmlElemAttrs();
            // KABU TODO: IMPORTANT: Do we need a required validation error message?
            if (dprop.IsRequiredOnEdit)
                oAttr("required", "");

            // KABU TODO: VERY IMPORTANT: KENDO-VERSION-ISSUE: We can't use min/max because our kendo version
            //   will not display an error but just set the value automatically to the
            //   next value in range - which is unacceptable.
#if (false)
            if (min != null)
            {
                oAttr("min", min);
                oAttr("data-val-range-min", min);
                oAttr("aria-valmin", min);
                //oAttr("min", min);
            }

            if (max != null)
            {
                oAttr("max", max);
                oAttr("data-val-range-max", max);
                oAttr("aria-valmax", max);
            }
#endif
            oO("/>");

            if (validationElem)
                OValidationMessageElem(context.PropInfo.PropPath);
        }

        void OKendoColorPicker(WebViewGenContext context)
        {
            var vprop = context.PropInfo.ViewProp;
            var dprop = context.PropInfo.TargetDisplayProp;
            var ppath = context.PropInfo.PropPath;
            //ElemClass("k-input");
            Oo($@"<input id='{ppath}', name='{ppath}' data-role='colorpicker'");
            oAttr("data-opacity", Moj.JS(dprop.IsColorWithOpacity));
            OHtmlElemAttrs();
            OHtmlDataBindValue(context, ppath);
            oO("/>");
#if (false)
            <input class="k-input" id="Color" name="Color" /><script>
            jQuery(function(){jQuery("#Color").kendoColorPicker({"opacity":false});});
#endif
        }


        void OKendoDateTimePicker(WebViewGenContext context)
        {
            var vprop = context.PropInfo.ViewProp;
            var dprop = context.PropInfo.TargetDisplayProp;
            var ppath = context.PropInfo.PropPath;

            var time = vprop.DisplayDateTime ?? dprop.Type.DateTimeInfo;

            string role;
            if (time.IsDateAndTime)
                role = "datetimepicker";
            else if (time.IsDate)
                role = "datepicker";
            else
                role = "timepicker";

            Oo("<input");
            oAttr("data-role", role);
            oAttr("id", ppath);
            oAttr("name", ppath);
            oAttr("data-display-name", vprop.DisplayLabel);
            OHtmlElemAttrs();
            // KABU TODO: IMPORTANT: Do we need a require dvalidation error message?
            if (dprop.IsRequiredOnEdit)
                oAttr("required", "");
            OHtmlDataBindValue(context, ppath);
            oO("/>");
        }

        void OKendoTimeSpanEditor(WebViewGenContext context)
        {
            var vprop = context.PropInfo.ViewProp;
            var dprop = context.PropInfo.TargetDisplayProp;
            var ppath = context.PropInfo.PropPath;
            Oo("<div"); OHtmlElemAttrs(); oO(">");
            Push();
            OKendoNumericInput(context, ppath: ppath + ".Hours", min: 0, max: 23, validationElem: false);
            OKendoNumericInput(context, ppath: ppath + ".Minutes", min: 0, max: 59, validationElem: false);
            Pop();
            O("</div>");
            OValidationMessageElem(ppath + ".Hours");
            OValidationMessageElem(ppath + ".Minutes");
        }

        public void OKendoTextInput(WebViewGenContext context)
        {
            var vprop = context.PropInfo.ViewProp;
            var dprop = context.PropInfo.Prop;
            var ppath = context.PropInfo.PropPath;

            if (!dprop.IsSpellCheck)
                ElemAttr("spellcheck", false);

            if (dprop.Type.IsMultilineString)
            {
                Oo($@"<textarea id='{ppath}' name='{ppath}'");

                if (dprop.RowCount != 0) ElemAttr("rows", dprop.RowCount);
                if (dprop.ColCount != 0) ElemAttr("cols", dprop.ColCount);
                // KABU TODO: IMPORTANT: Check whether Required and LocallyRequired works.
                OHtmlElemAttrs();
                OHtmlRequiredttrs(context, dprop);
                OHtmlDataBindValue(context);
                if (vprop.UseCodeRenderer != null)
                    ElemAttr("data-use-renderer", vprop.UseCodeRenderer);
                oO("></textarea>");
                /*
                    <textarea class="form-control form-control" cols="20" data-val="true" data-val-length="Das Feld &quot;Notizen&quot; muss eine Zeichenfolge mit einer maximalen Länge von 4096 sein." data-val-length-max="4096" id="Notes" name="Notes" rows="6" spellcheck="false">
                    </textarea>
                */
            }
            else
            {
                var inputType = GetTextInputType(dprop.Type.AnnotationDataType);
                if (inputType != null)
                    ElemAttr("type", inputType);
                else
                    ElemAttr("type", "text");

                ElemClass("k-textbox");

                Oo($@"<input id='{ppath}' name='{ppath}'");
                //  spellcheck='false'
                // KABU TODO: IMPORTANT: Check whether Required and LocallyRequired works.
                OHtmlElemAttrs();
                OHtmlRequiredttrs(context, dprop);
                OHtmlDataBindValue(context);

                // Postal code
                if (dprop.Type.AnnotationDataType == DataType.PostalCode)
                {
                    o(@" data-val-length-min='5' data-val-length-max='5' data-val-length='Die Postleitzahl muss fünfstellig sein.'");
                    o(@" data-val-regex='Die Postleitzahl muss eine fünfstellige Zahl sein.' data-val-regex-pattern='^\d{5}$'");
                }
                // Min/max length
                else
                    oLengthValidationAttrs(context);

                oO("/>");
            }
        }      

        public void OKendoCheckbox(WebViewGenContext context)
        {
            var ppath = context.PropInfo.PropPath;

            Oo($@"<input id='{ppath}' name='{ppath}' type='checkbox'");
            ElemClass("k-checkbox");
            OHtmlElemAttrs();
            OHtmlDataBindValue(context, "checked");
            oO("/>");
            // Checkbox label
            O($@"<label class='k-checkbox-label' for='{ppath}'>{GetDisplayNameFor(context)}</label>");
        }

        void oLengthValidationAttrs(WebViewGenContext context)
        {
            var vprop = context.PropInfo.ViewProp;
            var dprop = context.PropInfo.Prop;
            var ppath = context.PropInfo.PropPath;

            var min = dprop.Rules.Min ?? vprop.Rules.Min;
            var max = dprop.Rules.Max ?? vprop.Rules.Max;

            if (min == null && max == null)
                return;

            if (min != null)
                o($@" data-val-length-min='{min}'");
            if (max != null)
                o($@" data-val-length-max='{max}'");

            // Validation error text.
            o($@" data-val-length='Feld “{GetDisplayNameFor(context)}” ");
            if (min != null)
                o($@"muss mindestens {min} Zeichen");
            if (max != null)
            {
                if (min != null)
                    o(@" und ");
                o($@"darf maximal {max} Zeichen");
            }
            o(@" lang sein.'");
        }

        string GetTextInputType(System.ComponentModel.DataAnnotations.DataType? type)
        {
            if (type == null)
                return null;

            // HTML input types:
            // color
            // date
            // datetime
            // datetime - local
            // email
            // month
            // number
            // range
            // search
            // tel
            // time
            // url
            // week
            string result;
            if (_textInputTypeByDataType.TryGetValue(type.Value, out result))
                return result;

            return null;
        }

        static readonly Dictionary<System.ComponentModel.DataAnnotations.DataType, string> _textInputTypeByDataType =
            new Dictionary<System.ComponentModel.DataAnnotations.DataType, string>
            {
                [DataType.EmailAddress] = "email",
                [DataType.PhoneNumber] = "tel",
                [DataType.Url] = "url",
                [DataType.DateTime] = "datetime",
                [DataType.Date] = "date",
                [DataType.Time] = "time",
                [DataType.Currency] = "number",
                [DataType.Password] = "password"
            };

        void OFileUpload(WebViewGenContext context)
        {
            var vprop = context.PropInfo.ViewProp;
            var dprop = context.PropInfo.Prop;
            var ppath = context.PropInfo.PropPath;

            string name = context.View.TypeConfig.ClassName + dprop.Alias;
            string alias = dprop.Alias;
            string uploadTemplateId = name + "Template";

            // KABU TODO: IMPL currently disabled
            throw new MojenException("File upload is not supported currently.");
#if (false)

            O("@Html.HiddenFor(m => m.{0})", ppath);
            // KABU TODO: This probably won't work with nested type properties.
            foreach (var relatedProp in prop.AutoRelatedProps) O("@Html.HiddenFor(m => m.{0})", relatedProp.Name);

            if (prop.FileRef.IsImage)
            {
                // Image-thumbnail
                XB("<div class='edit-image-thumbnail'>");
                Oo($"<img id='{alias}ImageThumbnail' alt=''");
                // KABU TODO: REMOVE: We don't have an URI property anymore.
                //o($"data-bind='attr: {{ src: {alias}Uri }}'");
                oO(" />");
                OE("</div>");
            }

            // Kendo upload template.
            O();
            XB("<script id='{0}' type='text/x-kendo-template'>", uploadTemplateId);
            XB("<div class='file-wrapper'>");
            O("<div style='font-size: 0.9em; text-align:left'>#=name#</div>");
            OE("</div>");
            O("<span class='k-progress' style='height: 10px;'></span>");
            OE("</script>");

            // Kendo upload widget.
            O();
            O("@(Html.Kendo().Upload().Name(\"{0}\")", name);
            Push();
            O(".Multiple(false)");
            O(".Async(a => a.SaveUrl(\"api/UploadFile/{0}\").AutoUpload(true))", name);
            O(".ShowFileList(false)");
            O(".Messages(m => m.StatusUploaded(\" \").HeaderStatusUploaded(\" \").HeaderStatusUploading(\" \").StatusUploading(\" \"))");
            O(".Events(e => e.Success(\"kmodo.onPhotoUploaded\").Error(\"kmodo.onFileUploadFailed\").Remove(\"kmodo.onFileUploadRemoving\"))");
            // Add the Kendo upload template defined above.
            O(".TemplateId(\"{0}\")", uploadTemplateId);

            // Accepted file types (by file extension).
            if (prop.FileRef.IsImage)
                AddAttr("accept", ".png,.jpg");
            AddAttr("data-file-prop", alias);
            AddAttr("max-width", "100px");
            OMvcAttrs(true);

            oO(")");
            Pop();
#endif
        }

#if (false)
        void ONumericInputScriptCore(string ppath, string format, int? decimals)
        {
            Oo($@"jQuery(function(){{ jQuery('#{ppath}').kendoNumericTextBox({{");
            // Format: http://docs.telerik.com/kendo-ui/framework/globalization/numberformatting
            // Format: http://stackoverflow.com/questions/15241603/formatting-kendo-numeric-text-box
            o($"'format':'{format}'");
            // Decimals                
            if (decimals != null)
                o($",'decimals':{decimals}");
            oO("});});");
        }

        void ONumericInputScript(MojProp prop, string ppath)
        {
            // Format: http://docs.telerik.com/kendo-ui/framework/globalization/numberformatting
            // Format: http://stackoverflow.com/questions/15241603/formatting-kendo-numeric-text-box
            var format = prop.Type.IsInteger ? "#" : "#.##"; // "{0:#.##}";
            // Decimals
            int? decimals = prop.Type.IsInteger ? 0 : (int?)null;
            ONumericInputScriptCore(ppath, format, decimals);
        }
#endif
    }
}