using System.ComponentModel.DataAnnotations;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoFormEditorViewGen : KendoTypeViewGenBase
    {
        // NumericInput ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void ONumericInputElemCore(MojViewProp vprop, MojProp prop, string propPath, int? min = null, int? max = null)
        {
            min = min ?? prop.Rules.Min;
            max = max ?? prop.Rules.Max;

            Oo("<input");

            oAttr("data-role", "numerictextbox");
            //oAttr("type", "text");
            oAttr("id", propPath);
            oAttr("name", propPath);
            oAttr("type", "number");
            oAttr("data-type", "number");
            oAttr("data-display-name", vprop.DisplayLabel);
            // data-bind="value: CheckNumber"
            oAttr("data-bind", $"value: {propPath}");

            string format = "";
            int? decimals = 0;
            bool spinners = false;
            if (prop.Type.AnnotationDataType == DataType.Currency)
            {
                format = "#.##";
                decimals = 2;
            }
            else if (prop.Type.IsDecimal)
            {

                // Format: http://docs.telerik.com/kendo-ui/framework/globalization/numberformatting
                // Format: http://stackoverflow.com/questions/15241603/formatting-kendo-numeric-text-box
                format = "#.##"; // "{0:#.##}";
                decimals = prop.Attrs.Find<MojPrecisionAttr>()?.Scale ?? 2;
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
            oAttr("data-spinners", MojenUtils.ToJsValue(spinners));

            // KABU TODO: VERY IMPORTANT: We can't use min/max because our kendo version
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

            if (prop.IsRequiredOnEdit)
                oAttr("required", "");

            OElemAttrs();

            oO("/>");

            /*
            <input id="CheckNumber"
                data-display-name="Kennung"
                data-role="numerictextbox"
                data-format="#"
                data-decimals="0"
                data-min="1"
                data-max="9999"
                data-bind="value: CheckNumber"
                required="" /> 
            */
        }

        void ONumericInputElem(MojViewProp vprop, MojProp prop, string propPath)
        {
            ONumericInputElemCore(vprop, prop, propPath);
        }

        //void ONumericInputScriptCore(string propPath, string format, int? decimals)
        //{
        //    Oo($@"jQuery(function(){{ jQuery('#{propPath}').kendoNumericTextBox({{");

        //    // Format: http://docs.telerik.com/kendo-ui/framework/globalization/numberformatting
        //    // Format: http://stackoverflow.com/questions/15241603/formatting-kendo-numeric-text-box
        //    o($"'format':'{format}'");

        //    // Decimals                
        //    if (decimals != null)
        //        o($",'decimals':{decimals}");

        //    oO("});});");
        //}

        //void ONumericInputScript(MojProp prop, string propPath)
        //{
        //    // Format: http://docs.telerik.com/kendo-ui/framework/globalization/numberformatting
        //    // Format: http://stackoverflow.com/questions/15241603/formatting-kendo-numeric-text-box
        //    var format = prop.Type.IsInteger ? "#" : "#.##"; // "{0:#.##}";

        //    // Decimals
        //    int? decimals = prop.Type.IsInteger ? 0 : (int?)null;

        //    ONumericInputScriptCore(propPath, format, decimals);
        //}

        void ONumericInput(MojViewProp vprop, MojProp prop, string propPath)
        {
            ONumericInputElem(vprop, prop, propPath);

            //OScriptBegin();
            //ONumericInputScript(prop, propPath);
            //OScriptEnd();

            OValidationMessageElem(prop, propPath);

            /* Original MVC wrapper output:
            <input data-val="true" 
                    data-val-number="The field Anzahl der Mitarbeiter must be a number." 
                    data-val-range="Das Feld &quot;Anzahl der Mitarbeiter&quot; muss zwischen 1 und 99 liegen." 
                    data-val-range-max="99" 
                    data-val-range-min="1" 
                    data-val-required="Das Feld &quot;Anzahl der Mitarbeiter&quot; ist erforderlich." 
                    id="WorkersCount" 
                    max="99" 
                    min="1" 
                    name="WorkersCount" 
                    type="text" />
            <script>
	        jQuery(function(){jQuery("\#WorkersCount").kendoNumericTextBox({"format":"\#","decimals":0});});
            <\/script>
            <span class="field-validation-valid" data-valmsg-for="WorkersCount" data-valmsg-replace="true"></span>                        
            */
        }

        void OTimeSpanInput(MojViewProp vprop, MojProp prop, string propPath)
        {
            Oo("<div"); OElemAttrs(); oO(">");
            Push();
            ONumericInputElemCore(vprop, prop, propPath + ".Hours", min: 0, max: 23);
            ONumericInputElemCore(vprop, prop, propPath + ".Minutes", min: 0, max: 59);
            Pop();
            O("</div>");

            //OScriptBegin();
            //ONumericInputScriptCore(vprop, prop, propPath + ".Hours", format: "#", decimals: 0);
            //ONumericInputScriptCore(propPath + ".Minutes", format: "#", decimals: 0);
            //OScriptEnd();

            OValidationMessageElem(prop, propPath + ".Hours");
            OValidationMessageElem(prop, propPath + ".Minutes");
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void OValidationMessageElem(MojProp prop, string propPath)
        {
            // Validation error message.
            O($"<span class='field-validation-valid' data-valmsg-for='{propPath}' data-valmsg-replace='true'></span>");
        }
    }
}