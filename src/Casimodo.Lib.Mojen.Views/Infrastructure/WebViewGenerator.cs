using System;
using System.IO;

namespace Casimodo.Lib.Mojen
{
    public enum WebViewHttpVerb
    {
        Get,
        Post
    }

    public abstract class WebViewGenerator : WebPartGenerator
    {
        public void OHiddenInputFor(string name)
        {
            O($"<input type='hidden' name='{name}' />");
        }

        protected string ActionLink(string text, string action, string controller)
        {
            return string.Format("@Html.ActionLink(\"{0}\", \"{1}\", \"{2}\")", text, action, controller);
        }

        protected string Paragraph(MojViewProp prop)
        {
            if (prop.IsHeader)
                return "h3";
            else
                return "p";
        }

        protected string HrefUrlAction(string action, MojViewConfig view)
        {
            return string.Format("href='@Url.Action(\"{0}\", \"{1}\")'", action, view.TypeConfig.PluralName);
        }

        protected string HrefUrlActionId(string action, MojViewConfig view, bool isModel = false)
        {
            //var prop = view.Model.Entity.Props.FirstOrDefault(x => x.IsPrimaryKey);
            //if (prop == null)
            //    throw new Exception(string.Format("Model '{0}' of view '{1}/{2}' has no key defined.", view.Model.ClassName, view.Controller.Name, view.Action.Name));

            return string.Format("href='@Url.Action(\"{0}\", \"{1}\", new {{ id = {2}.State.{3} }})'", action, view.TypeConfig.PluralName, (isModel ? "Model" : "item"), view.TypeConfig.Key.Name);
        }

        protected string GetMobileListItemProp(MojViewProp prop)
        {
            return string.Format("<{0}>@item.{1}</{0}>", Paragraph(prop), prop.Model.Name);
        }

        protected void PerformWrite(MojViewConfig view, Action callback)
        {
            PerformWrite(BuildFilePath(view), callback);
        }

        protected void PerformWrite(MojViewConfig view, Action<MojViewConfig> callback)
        {
            PerformWrite(BuildFilePath(view), () => callback(view));
        }
    }
}