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
    public class KendoGridInlineDetailsViewGen : KendoReadOnlyViewGenBase
    {
        // KABU TODO: IMPORTANT: Evaluate if we still really need this.

        //public override void OPropLabel(WebViewGenContext context)
        //{
        //    var vitem = context.PropInfo;

        //    Oo($"<label for='{vitem.PropPath}' class='{LabelClass}'>");

        //    if (vitem.CustomDisplayLabel != null)
        //        o(vitem.CustomDisplayLabel);
        //    else
        //        o($"@(Html.DisplayNameFor(x => x.{vitem.PropPath}).ToKendoTemplate())");

        //    oO("</label>");
        //}
    }
}