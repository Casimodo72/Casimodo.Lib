using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public enum HttpVerb
    {
        Get,
        Post,
        Put,
        Patch,
        Delete
    }

    public class HtmlStyleProp
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public abstract class WebViewGenerator : WebPartGenerator
    {
        public List<MojXAttribute> Attributes { get; set; } = new List<MojXAttribute>();

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
            return string.Format("<{0}>@item.{1}</{0}>", Paragraph(prop), prop.OrigTargetProp.Name);
        }

        protected void PerformWrite(MojViewConfig view, Action callback)
        {
            PerformWrite(BuildFilePath(view), callback);
        }

        protected void PerformWrite(MojViewConfig view, Action<MojViewConfig> callback)
        {
            PerformWrite(BuildFilePath(view), () => callback(view));
        }

        protected void CheckViewId(MojViewConfig view)
        {
            if (string.IsNullOrWhiteSpace(view.Id))
                throw new MojenException($"The view for '{view.TypeConfig.Name}' has no ID.");
        }

        public void OJsScript(Action content)
        {
            XB("<script>");
            OUseStrict();
            content();
            XE("</script>");
        }

        public void ORazorModel(string className)
        {
            O($"@model {App.Get<WebAppBuildConfig>().WebDataViewModelNamespace}.{className}");
        }

        public void ORazorModel(MojType type)
        {
            type = type.RequiredStore;
            O($"@model {App.GetDataLayerConfig(type.DataContextName).DataNamespace}.{type.ClassName}");
        }

        public void CustomElemStyle(WebViewGenContext context)
        {
            var vprop = context.PropInfo.ViewProp;

            if (vprop.FontWeight == MojFontWeight.Bold)
                ElemClass("strong");
        }

        public string GetElemAttrs(string target = null)
        {
            string result = "";
            var attrs = GetElemAttrsByTarget(target);
            if (attrs.Any())
            {
                result = " " + attrs
                    .Select(x => $"{x.Name.LocalName}='{x.Value}'")
                    .Join(" ");
            }

            ClearElemAttrs(target);

            return result;
        }

        public void oElemAttrs(string target = null)
        {
            var result = GetElemAttrs(target);
            if (result != null)
            {
                o(result);
                o(" ");
            }

            // TODO: REMOVE
            //var attrs = GetElemAttrsByTarget(target);
            //if (attrs.Any())
            //{
            //    o(" ");
            //    o(attrs.Select(x => $"{x.Name.LocalName}='{x.Value}'").Join(" "));
            //    o(" ");

            //    ClearElemAttrs(target);
            //}
        }

        MojXAttribute[] GetElemAttrsByTarget(string target)
        {
            return Attributes.Where(x => x.Target == target).ToArray();
        }

        public void ClearElemAttrs(string target = null)
        {
            foreach (var attr in GetElemAttrsByTarget(target))
                Attributes.Remove(attr);
        }

        public virtual void OMvcAttrs(bool kendo)
        {
            if (Attributes.Any())
            {
                var members = Attributes.Select(x => $"{ConvertAttrName(x.Name.LocalName)} = \"{x.Value}\"").Join(", ");
                if (kendo)
                    Oo($".HtmlAttributes(new {{ {members} }})");
                else
                    o($", new {{ {members} }}");

                Attributes.Clear();
            }
        }

        public string ConvertAttrName(string name)
        {
            if (name == "class")
                return "@class";
            else if (name == "readonly")
                return "@readonly";

            return name.Replace("-", "_");
        }

        public void ElemAttr(string name, object value)
        {
            Attributes.Add(XA(name, value));
        }

        /// <summary>
        /// For HTML boolean attributes like "readonly" which don't have an attribute value.
        /// </summary>
        public void ElemFlag(string name)
        {
            Attributes.Add(XA(name, name));
        }

        public void ElemClass(string classes, string target = null)
        {
            var attr = GetOrCreateAttr("class", target);
            attr.Value = string.IsNullOrEmpty(attr.Value) ? classes : attr.Value + " " + classes;
        }

        public void ElemStyle(string value)
        {
            var attr = GetOrCreateAttr("style");
            attr.Value = string.IsNullOrEmpty(attr.Value) ? value : attr.Value + ";" + value;
        }



        public void ElemDataBindAttr(string expression)
        {
            var attr = GetOrCreateAttr("data-bind");
            attr.Value = string.IsNullOrEmpty(attr.Value) ? expression : attr.Value + ", " + expression;
        }

        public MojXAttribute GetOrCreateAttr(string name, string target = null)
        {
            var attr = Attributes.FirstOrDefault(x => x.Name == name && x.Target == target) as MojXAttribute;
            if (attr == null)
            {
                attr = XA(name, "");
                attr.Target = target;
                Attributes.Add(attr);
            }
            return attr;
        }




    }
}