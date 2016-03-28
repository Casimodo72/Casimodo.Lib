using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    /// <summary>
    /// Inline details view for the Kendo grid.
    /// </summary>
    public class KendoWebDisplayTemplate2Gen : KendoReadOnlyViewGenBase
    {
        public override void OPropLabel(WebViewGenContext context)
        {
            var vitem = context.PropInfo;

            Oo($"<label for='{vitem.PropPath}' class='{LabelClass}'>");

            if (vitem.CustomDisplayLabel != null)
                o(vitem.CustomDisplayLabel);
            else
                o($"@(Html.DisplayNameFor(x => x.{vitem.PropPath}).ToKendoTemplate())");

            oO("</label>");
        }
    }
}