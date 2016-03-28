using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoPartGen : WebPartGenerator
    {
        public string GetPlainDisplayTemplate(MojViewProp prop, bool checkLastProp = false)
        {
            var sb = new StringBuilder();

            sb.o("#if("); GetNotNullExpressionTemplate(sb, prop, checkLastProp); sb.o("){#");

            sb.o("#:"); sb.o(prop.FormedNavigationTo.TargetPath); sb.o("#");

            sb.o("#}#");

            return sb.ToString();
        }

        void GetNotNullExpressionTemplate(StringBuilder sb, MojViewProp prop, bool checkLastProp = false)
        {
            // Example:
            // typeof Company !== 'undefined' && Company && Company.NameShort
            var steps = prop.FormedTargetPathNames.ToArray();
            var length = steps.Length - (checkLastProp ? 0 : 1);
            for (int i = 0; i < length; i++)
            {
                if (i == 0) sb.o($"typeof {steps[0]}!=='undefined'&&");

                for (int k = 0; k <= i; k++)
                {
                    sb.o($"{steps[k]}");

                    if (k < i) sb.o(".");
                }

                if (i < length - 1) sb.o("&&");
            }
        }

        public void OModalMessage(MojenGeneratorBase gen, string containerVar, string message)
        {
            WriteTo(gen, () =>
            {
                Oo($"var wnd = $('<div></div>').appendTo({containerVar}).kendoWindow(");
                OWindowOptions(new KendoWindowConfig
                {
                    Animation = false,
                    Width = 300,
                    IsModal = true,
                });
                oO(").data('kendoWindow');"); // Kendo window

                O($"wnd.content('<div style=\"margin: 12px\">{message}</div>');");

                O("kendomodo.setModalWindowBehavior(wnd);");

                O("wnd.center().open();");

            });
        }

        public void OWindowOptions(KendoWindowConfig window)
        {
            // See http://docs.telerik.com/KENDO-UI/api/javascript/ui/window

            var options = XBuilder.CreateAnonymous();

            XBuilder animation = null;
            if (window.Animation == null || (window.Animation as bool?) == true)
            {
                if (window.IsParentModal)
                    animation = options.Add("animation", "false");
                else
                    animation = options.Add("animation", "kendomodo.getDefaultDialogWindowAnimation()");
            }

#if (false)
            if (window.Open.Is)
                animation.O("open")
                    .O("effects", window.Open.Effects).End()
                    .O("duration", window.Open.Duration).End();

            if (window.Close.Is)
                animation.O("close")
                    .O("effects", window.Close.Effects).End()
                    .O("duration", window.Close.Duration).End();
#endif
            if (window.ContentUrl != null)
            {
                options.Add("content")
                    .Add("url", window.ContentUrl, text: true);
            }

            options.Add("modal", window.IsModal);
            options.Add("visible", window.IsVisible);
            if (window.Title != null) options.Add("title", window.Title, text: true);
            if (window.Width != null) options.Add("width", window.Width);
            if (window.MaxWidth != null) options.Add("maxWidth", window.MaxWidth);
            if (window.MinHeight != null) options.Add("minHeight", window.MinHeight);
            if (window.Height != null) options.Add("height", window.Height);
            if (window.OnClosing != null) options.Add("close", window.OnClosing);
            if (window.OnDeactivated != null) options.Add("deactivate", window.OnDeactivated);

            OJsObjectLiteral(options.Elem, trailingNewline: false, trailingComma: false);
        }
    }
}
