using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    // KABU TODO: REVISIT: Not used anymore. Keep for example purpose.
    public class BootstrapWebView : WebViewGenerator
    {
        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>())
                PerformWrite(view, GenerateView);
        }

        public void GenerateView(MojViewConfig view)
        {
            if (view.Kind.Roles.HasFlag(MojViewRole.Index))
            {
                GenerateDesktopListAsAccordion(view);
                //GenerateDesktopListAsTable(view);
            }
            else if (view.Kind.Roles.HasFlag(MojViewRole.Editor))
            {
                O("@using TwitterBootstrapMVC");
                O("@model {0}.{1}", view.TypeConfig.Namespace, view.TypeConfig.ClassName);
                O();

                // This will generate wrap-items-container like flow of label/editor components.
                //O("@using (Html.Bootstrap().Begin(new Form().Type(FormType.Inline)))");

                O("@using (Html.Bootstrap().Begin(new Form().Type(FormType.Horizontal)))");
                Begin();

                var mprops = view.TypeConfig.GetProps().ToArray();
                var eprops = view.TypeConfig.Store.GetProps().ToArray();

                foreach (var eprop in eprops)
                {
                    MojProp mprop = mprops.FirstOrDefault(x => x.Store == eprop);
                    if (mprop == null || mprop.IsKey)
                    {
                        // Hidden form field.
                        O("@Html.HiddenFor(item => item.{0});", eprop.Name);
                        continue;
                    }
                    MojViewProp vprop = view.Props.FirstOrDefault(x => x.Model == mprop);
                    if (vprop == null)
                    {
                        // Hidden form field.
                        O("@Html.HiddenFor(item => item.{0});", mprop.Name);
                        continue;
                    }

                    //O("@Html.Bootstrap().LabelFor(item => item.{0})", prop.ModelProp.PropName);
                    O("@Html.Bootstrap().ControlGroup().{0}(item => item.{1})", GetBootstrapPropertyDisplay(mprop), mprop.Name);
                    //O("<p>@Html.ValidationMessageFor(item => item.{0})</p>", prop.ModelProp.PropName);
                }

                O("using(Html.Bootstrap().Begin(new FormActions()))");
                Begin();
                O("<div>");
                O("    @Html.Bootstrap().SubmitButton().Text(\"Speichern\")");
                O("    @*@Html.Bootstrap().ActionLinkButton(\"Go Back\", \"previousAction\", \"controller\")*@");
                O("</div>");
                End();

                End();
            }
            else if (App.Get<WebBuildConfig>().ThrowIfControllerActionIsMissing)
            {
                throw new NotImplementedException(string.Format("View kind '{0}' is not implemented yet.", view.Kind.ActionName));
            }
        }

        void GenerateDesktopListAsAccordion(MojViewConfig view)
        {
            O("@using TwitterBootstrapMVC");
            O("@model IEnumerable<{0}.{1}>", view.TypeConfig.Namespace, view.TypeConfig.ClassName);
            O();

            O("@using (var accordion = Html.Bootstrap().Begin(new Accordion(\"uniqueAccordionId\")))");
            Begin();

            O("foreach (var item in Model)");
            Begin();
            O("using(accordion.BeginPanel(item.{0}))", view.Props.First().Name);
            Begin();
            //O("<a {0}>", HrefUrlActionId(ActionName.Details, view));
            foreach (var prop in view.Props.Skip(1))
            {
                O(string.Format("<{0}>@item.{1}</{0}>", Paragraph(prop), prop.Model.Name)); //GetMobileListItemProp(prop));
            }
            //O("</a>");
            End();

            End();

            //O("using(table.BeginRow(RowColor.Error))");

            End();
        }

        void GenerateDesktopListAsTable(MojViewConfig view)
        {
            O("@using TwitterBootstrapMVC");
            O("@model IEnumerable<{0}.{1}>", view.TypeConfig.Namespace, view.TypeConfig.ClassName);
            O();

            O("@using(var table = Html.Bootstrap().Begin(new Table()))");
            Begin();

            O("foreach (var item in Model)");
            Begin();

            O("using(table.BeginRow())");
            Begin();
            O("<td>");
            O("<a {0}>", HrefUrlActionId(ActionName.Details, view));
            foreach (var prop in view.Props)
            {
                //if (prop.ModelProp.IsKey)
                //    continue;

                O(GetMobileListItemProp(prop));
            }
            O("</a>");
            O("</td>");
            End();

            End();

            //O("using(table.BeginRow(RowColor.Error))");

            End();
        }

        string GetDesktopListItemProp(MojViewProp prop)
        {
            return string.Format("<{0}>@item.{1}</{0}>", Paragraph(prop), prop.Name);
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        string GetBootstrapPropertyDisplay(MojProp prop)
        {
            DataType? dt = prop.Type.AnnotationDataType;

            if (dt == DataType.MultilineText)
                return "TextAreaFor";

            if (dt == DataType.Password)
                return "PasswordFor";

            if (prop.Type.TypeNormalized == typeof(bool))
                return "CheckBoxFor";

            return "TextBoxFor";
        }
    }
}