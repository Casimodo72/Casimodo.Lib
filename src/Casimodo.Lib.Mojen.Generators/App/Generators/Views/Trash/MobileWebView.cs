using System.Linq;

namespace Casimodo.Lib.Mojen
{
    // KABU TODO: REVISIT: Not used anymore. Keep for example purpose.
    public class MobileWebView : WebViewGenerator
    {
        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>())
                PerformWrite(view, GenerateView);
        }

        void GenerateView(MojViewConfig view)
        {
            if (view.Kind.Roles.HasFlag(MojViewRole.Index))
            {
                O("@model IEnumerable<{0}.{1}>", view.TypeConfig.Namespace, view.TypeConfig.ClassName);
                O();
                O("<h2>@ViewBag.Message</h2>");
                O("<div style='text-align:right'>");
                O("<a {0} data-role='button' data-inline='true' data-icon='plus' data-iconpos='notext' data-theme='e' data-mini='true'>x</a>", HrefUrlAction(ActionName.Create, view));
                O("</div><br/>");
                //O("<p>{0}</p>", ActionLink("Neuer Kunde", ActionName.Create, view.Controller.Name));
                O("<ul data-role='listview' data-inset='true' data-filter='true'>");
                O("@foreach (var item in Model)");
                Begin();
                O("<li {0}>", GetMobileDataFilterText(view));
                O("    <a {0}>", HrefUrlActionId(ActionName.Details, view));
                foreach (var prop in view.Props)
                {
                    if (prop.Model.IsKey)
                        continue;

                    O(GetMobileListItemProp(prop));
                }
                O("    </a>");
                O("</li>");
                End();
                O("</ul>");
            }
            else if (view.Kind.Roles.HasFlag(MojViewRole.Details))
            {
                O("@model {0}.{1}", view.TypeConfig.Namespace, view.TypeConfig.ClassName);
                O();
                GenerateMobileEditButton(view);

                foreach (var prop in view.Props)
                {
                    if (prop.Model.IsKey)
                        continue;

                    O("<h3>@Html.DisplayNameFor(item => item.{0})</h3>", prop.Model.Name);
                    O("<div>");
                    O("<p>@Html.DisplayFor(item => item.{0})</p>", prop.Model.Name);
                    O("<p>@Html.ValidationMessageFor(item => item.{0})</p>", prop.Model.Name);
                    O("</div>");
                }
            }
            else if (view.Kind.Roles.HasFlag(MojViewRole.Editor))
            {
                O("@model {0}.{1}", view.TypeConfig.Namespace, view.TypeConfig.ClassName);
                O();
                O("@using (Html.BeginForm()) {");
                O("@Html.AntiForgeryToken()");
                O("@Html.ValidationSummary(true)");
                foreach (var prop in view.Props)
                {
                    if (prop.Model.IsKey)
                        continue;

                    O("<h3>@Html.DisplayNameFor(item => item.{0})</h3>", prop.Model.Name);
                    O("<div>");
                    O("<p>@Html.EditorFor(item => item.{0})</p>", prop.Model.Name);
                    O("<p>@Html.ValidationMessageFor(item => item.{0})</p>", prop.Model.Name);
                    O("</div>");
                }
                End();
            }
        }

        string GetMobileDataFilterText(MojViewConfig view)
        {
            var prop = view.Props.FirstOrDefault(x => x.IsDataFilterText);
            if (prop == null)
                return "";

            return string.Format("data-filtertext='@item.{0}'", prop.Model.Name);
        }

        void GenerateMobileEditButton(MojViewConfig view)
        {
            //O("@using (Html.BeginForm("Edit", "Customer", new { id = Model.Id }, FormMethod.Get))");
            //B();
            O("<a {0} data-role='button' data-icon='grid' data-iconpos='notext' data-mini='true' data-theme='e'>Bearbeiten</a>", HrefUrlActionId(ActionName.Edit, view, isModel: true));
            //E();
        }
    }
}