using Casimodo.Lib;
using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoEditorGen : KendoTypeViewGenBase
    {
        // NumericInput ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void ONumericInputElemCore(string propPath, int? min, int? max)
        {
            Oo("<input");

            oAttr("id", propPath);
            oAttr("name", propPath);

            if (min != null)
            {
                oAttr("data-val-range-min", min);
                oAttr("min", min);
            }

            if (max != null)
            {
                oAttr("data-val-range-max", max);
                oAttr("max", max);
            }

            oAttr("type", "text");

            OElemAttrs();

            oO("/>");
        }

        void ONumericInputElem(MojProp prop, string propPath)
        {
            ONumericInputElemCore(propPath, prop.Rules.Min, prop.Rules.Max);
        }

        void ONumericInputScriptCore(string propPath, string format, int? decimals)
        {
            Oo($@"jQuery(function(){{ jQuery('#{propPath}').kendoNumericTextBox({{");

            // Format: http://docs.telerik.com/kendo-ui/framework/globalization/numberformatting
            // Format: http://stackoverflow.com/questions/15241603/formatting-kendo-numeric-text-box
            o($"'format':'{format}'");

            // Decimals                
            if (decimals != null)
                o($",'decimals':{decimals}");

            oO("});});");
        }

        void ONumericInputScript(MojProp prop, string propPath)
        {
            // Format: http://docs.telerik.com/kendo-ui/framework/globalization/numberformatting
            // Format: http://stackoverflow.com/questions/15241603/formatting-kendo-numeric-text-box
            var format = prop.Type.IsInteger ? "#" : "#.##"; // "{0:#.##}";

            // Decimals
            int? decimals = prop.Type.IsInteger ? 0 : (int?)null;

            ONumericInputScriptCore(propPath, format, decimals);
        }

        void ONumericInput(MojProp prop, string propPath)
        {
            ONumericInputElem(prop, propPath);

            OScriptBegin();
            ONumericInputScript(prop, propPath);
            OScriptEnd();

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

        void OTimeSpanInput(MojProp prop, string propPath)
        {
            Oo("<div"); OElemAttrs(); oO(">");
            Push();
            ONumericInputElemCore(propPath + ".Hours", min: 0, max: 23);
            ONumericInputElemCore(propPath + ".Minutes", min: 0, max: 59);
            Pop();
            O("</div>");

            OScriptBegin();
            ONumericInputScriptCore(propPath + ".Hours", format: "#", decimals: 0);
            ONumericInputScriptCore(propPath + ".Minutes", format: "#", decimals: 0);
            OScriptEnd();

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