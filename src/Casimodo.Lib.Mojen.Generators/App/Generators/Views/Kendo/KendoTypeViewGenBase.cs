using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    public abstract class KendoTypeViewGenBase : KendoViewGenBase
    {
        public string FormGroupClass { get; set; } = "form-group";
        public string FormGroupReadOnlyClass { get; set; } = "form-group readonly";
        public string LabelContainerClass { get; set; } = "";
        public string LabelClass { get; set; } = "col-sm-3 col-xs-12 control-label"; // k-label label-field
        public string PropContainerClass { get; set; } = "col-sm-9 col-xs-12"; // k-edit-field editor-field

        public void GenerateView(WebViewGenContext context)
        {
            Define(context);

            BeginView(context);

            if (!context.View.Template.IsEmpty)
                OAny(context, context.View.Template.Root);

            EndView(context);

            AfterView(context);
        }

        public virtual void AfterView(WebViewGenContext context)
        {
            // NOP
        }

        public string DataViewModelAccessor { get; set; }

        public List<XAttribute> Attributes { get; set; } = new List<XAttribute>();

        public virtual void Define(WebViewGenContext context)
        {
            OPropContainerBegin = (c) => XB($"<div class='{PropContainerClass}'>");
            OPropContainerEnd = (c) => XE("</div>");
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

        public string GetElemAttrs()
        {
            string result = "";
            if (Attributes.Any())
                result = " " + Attributes.Select(x => $"{x.Name.LocalName}='{x.Value}'").Join(" ");

            Attributes.Clear();

            return result;
        }

        public void OHtmlElemAttrs()
        {
            if (Attributes.Any())
            {
                o(" ");
                o(Attributes.Select(x => $"{x.Name.LocalName}='{x.Value}'").Join(" "));
                o(" ");

                // KABU TODO: IMPORTANT: Shouldn't this be cleared here?
                Attributes.Clear();
            }
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

        public void ElemClass(string value)
        {
            var attr = GetOrCreateAttr("class");
            attr.Value = string.IsNullOrEmpty(attr.Value) ? value : attr.Value + " " + value;
        }

        public void ElemStyle(string value)
        {
            var attr = GetOrCreateAttr("style");
            attr.Value = string.IsNullOrEmpty(attr.Value) ? value : attr.Value + ";" + value;
        }

        public void ElemDataBindAttr(WebViewGenContext context)
        {
            if (context.View.UseMVVM)
            {
                GetOrCreateAttr("data-bind").Value = $"value:{GetBinding(context)}";
            }
        }

        public void ElemDataBindAttr(string expression)
        {
            var attr = GetOrCreateAttr("data-bind");
            attr.Value = string.IsNullOrEmpty(attr.Value) ? expression : attr.Value + ", " + expression;
        }

        public XAttribute GetOrCreateAttr(string name)
        {
            var attr = Attributes.FirstOrDefault(x => x.Name == name);
            if (attr == null)
            {
                attr = XA(name, "");
                Attributes.Add(attr);
            }
            return attr;
        }

        public string GetBinding(object item, bool alias = false)
        {
            // E.g. "item.FirstName" or just "FirstName" if DataViewModelAccessor is not assigned.
            var accessor = !string.IsNullOrWhiteSpace(DataViewModelAccessor) ? DataViewModelAccessor : "";
            string path = null;

            var context = item as WebViewGenContext;
            if (context != null)
                path = alias ? context.PropInfo.PropAliasPath : context.PropInfo.PropPath;

            var propTypePath = item as MojFormedType;
            if (propTypePath != null)
                path = alias ? propTypePath.FormedNavigationFrom.TargetAliasPath : propTypePath.FormedNavigationFrom.TargetPath;

            if (path == null)
                throw new MojenException($"Failed to build property binding path for item of type '{item.GetType().Name}'.");

            return $"{accessor}{path}";
        }

        IEnumerable<List<ViewTemplateItem>> FilterHiddenProps(IEnumerable<List<ViewTemplateItem>> runs, MojViewMode mode)
        {
            foreach (var run in runs)
            {
                var l = run.Where(x => x.Prop == null || !x.Prop.HideModes.HasFlag(mode)).ToList();
                if (l.Count != 0)
                    yield return l;
            }
        }

        void OAny(WebViewGenContext context, ViewTemplateItem item, bool next = true)
        {
            if (item == null)
                return;

            var prevItem = context.Cur;
            context.Cur = item;

            if (item.IsContainer)
            {
                if (OContainerBegin(context, item))
                {
                    OAny(context, item.Child);
                    OContainerEnd(context, item);
                }

                if (next)
                    OAny(context, item.Next);
            }
            else
            {
                var runs = item.GetRuns().ToArray();

                // Read-only views: filter out hidden items.
                if (context.View.Kind.Mode == MojViewMode.Read)
                    runs = FilterHiddenProps(runs, MojViewMode.Read).ToArray();

                if (runs.Any())
                {
                    bool useBlocks = runs.Any(run => run.Any(node => node.IsContainer));
                    bool inBlock = false;
                    bool inList = false;

                    foreach (var run in runs)
                    {
                        if (run.Any(x => x.IsContainer))
                        {
                            if (run.Count != 1) throw new MojenException("A container node must produce only a single run item.");

                            if (useBlocks && inBlock)
                            {
                                if (inList)
                                {
                                    OListEnd(context, item);
                                    inList = false;
                                }

                                OBlockEnd(context);
                                inBlock = false;
                            }

                            OAny(context, run.First(), next: false);

                            continue;
                        }

                        if (useBlocks && !inBlock)
                        {
                            if (inList)
                            {
                                OListEnd(context, item);
                                inList = false;
                            }

                            OBlockBegin(context);
                            inBlock = true;
                        }

                        var props = run.Where(x => x.Prop != null).ToList();

                        if (!inList)
                        {
                            OListBegin(context, item);
                            inList = true;
                        }

                        // Set run context
                        var prevRun = context.Run;
                        var prevRunProps = context.RunProps;
                        context.Run = run;
                        context.RunProps = props;

                        ORunBegin(context);

                        ORunCore(context, run);

                        ORunEnd(context);

                        // Restore previous run context
                        context.Run = prevRun;
                        context.RunProps = prevRunProps;
                    }

                    if (useBlocks && inBlock)
                    {
                        inBlock = false;
                        OBlockEnd(context);
                    }

                    if (inList)
                        OListEnd(context, item);
                }
            }

            context.Cur = prevItem;
        }

        public IEnumerable<ViewTemplateItem> CurrentRun { get; set; }

        public IEnumerable<ViewTemplateItem> CurrentRunProps
        {
            get { return CurrentRun.Where(x => x.Directive != "label" && x.Prop != null); }
        }

        void ORunCore(WebViewGenContext context, IEnumerable<ViewTemplateItem> run)
        {
            CurrentRun = run;
            ViewTemplateItem label = null;
            bool isPropRunStarted = false;
            foreach (var cur in run)
            {
                if (cur.IsContainer)
                {
                    OAny(context, cur);
                    continue;
                }

                if (cur.Directive == "custom-view")
                {
                    if (!context.IsElementHidden(cur.HideModes))
                    {
                        O($"@Html.Partial(\"_{cur.Name}\")");
                    }
                    continue;
                }

                if (cur.Directive == "label")
                {
                    label = cur;

                    continue;
                }

                if (cur.Directive == "append")
                {
                    if (label != null)
                    {
                        if (label.TextValue != null)
                        {
                            // Output label                            

                            OLabelContainerBegin(context);
                            ORunLabelCore(context, label.TextValue);
                            OLabelContainerEnd(context);

                            label = null;
                        }
                        else if (cur.Prop != null)
                        {
                            OPropLabelCore(context, cur);
                            label = null;
                        }
                    }

                    if (cur.TextValue != null)
                    {
                        // Output text
                        OText(context, cur);
                    }
                    else
                    {
                        // Output property

                        Attributes.Clear();
                        context.PropInfo = CreateViewPropInfo(context, cur);

                        if (!isPropRunStarted && !context.PropInfo.ViewProp.NoLabel)
                        {
                            isPropRunStarted = true;
                            OPropContainerBegin(context);
                        }

                        OProp(context);

                        context.PropInfo = null;
                    }
                    continue;
                }

                if (cur.Directive == "br")
                {
                    O("<br>");

                    label = null;

                    continue;
                }

                if (cur.Directive == "hr")
                {
                    O("<hr>");
                    Br();

                    label = null;

                    continue;
                }
            }

            if (isPropRunStarted)
            {
                Attributes.Clear();
                OPropContainerEnd(null);
            }
        }

        public virtual void BeginView(WebViewGenContext context)
        { }

        public virtual void EndView(WebViewGenContext context)
        { }

        public virtual void OListBegin(WebViewGenContext context, ViewTemplateItem cur)
        { }

        public virtual void OListEnd(WebViewGenContext context, ViewTemplateItem cur)
        { }

        public virtual string GetCssStyle(WebViewGenContext context, ViewTemplateItem cur)
        {
            var config = cur.Style as Style;
            if (config == null)
                return "";

            var modal = context.View.IsModal || context.IsModalView;

            var styles = new List<string>();

            if (config.BackColor != null) styles.Add("background-color:" + config.BackColor);

            if (!modal)
            {
                if (config.Width != null) styles.Add($"width:{config.Width}px");
                else
                {
                    if (config.MinWidth != null) styles.Add($"min-width:{config.MinWidth}px");
                    if (config.MaxWidth != null) styles.Add($"max-width:{config.MaxWidth}px");
                }
            }

            if (styles.Count == 0)
                return "";

            return $" style='{styles.Join(";")}'";
        }

        public virtual string GetColumnClasses(WebViewGenContext context, ViewTemplateItem cur)
        {
            var col = cur.Style as Style;

            if (col != null && col.Col != null)
                return col.Col;

            // Get number of columns
            int colNum = cur.Parent.GetChildren().Count(x => x.Directive == "column");
            //var modal = context.View.IsModal || context.IsModalView;

            // We are using bootstrap, so use 12 max cols.
            int span = 12 / colNum;

            return $"col-sm-{span}";
        }

        public virtual bool OContainerBegin(WebViewGenContext context, ViewTemplateItem cur)
        {
            if (cur.Directive == "grid")
            {
                //XB("<div class='container' style='width:100%'>");
                XB("<div class='row'>");
            }
            else if (cur.Directive == "column")
            {
                XB($"<div class='{GetColumnClasses(context, cur)}'{GetCssStyle(context, cur)}>");

                //XB($"<div class='col-lg-{span} col-md-{span} col-sm-6 col-xs-12'>"); //  style='vertical-align:top'
            }
            else if (cur.Directive == "group-box")
            {
                var @class = "panel panel-default";
                var predicate = "";

                if (cur.VisibilityCondition as MojFormedType != null)
                {
                    predicate = $" data-bind='visible: {GetBinding(cur.VisibilityCondition)}'";
                }

                if (cur.VisibilityCondition as MojViewMode? != null)
                {
                    var hideModes = (MojViewMode)cur.VisibilityCondition;

                    if (context.View.IsEditor)
                    {
                        foreach (var mode in hideModes.GetAtomicFlags())
                            @class += " remove-on-" + mode;
                    }

                    if (!context.View.IsEditor && hideModes.HasFlag(MojViewMode.Read))
                    {
                        // If read-only view and shall not be visible:
                        //   Skip this container entirely.
                        return false;
                    }
                }

                XB($"<div class='{@class}'{predicate}>");
                O("<div class='panel-heading'>{0}</div>", cur.TextValue);

                XB($"<div class='panel-body'{GetCssStyle(context, cur)}>");
            }

            return true;
        }

        public virtual void OContainerEnd(WebViewGenContext context, ViewTemplateItem cur)
        {
            if (cur.Directive == "grid")
            {
                XE("</div>"); // row
                //OE("</div>"); // container
            }
            else if (cur.Directive == "column")
            {
                XE("</div>");
            }
            else if (cur.Directive == "group-box")
            {
                XE("</div>"); // Group box content
                XE("</div>"); // Group box
            }
        }

        public bool IsRunSingleCustomView(WebViewGenContext context)
        {
            return context.Run.Count == 1 && context.Run.FirstOrDefault()?.Directive == "custom-view";
        }

        public virtual bool ORunBegin(WebViewGenContext context)
        {
            return !IsRunSingleCustomView(context);
        }

        public virtual bool ORunEnd(WebViewGenContext context)
        {
            return !IsRunSingleCustomView(context);
        }

        public MojViewPropInfo CreateViewPropInfo(WebViewGenContext context, ViewTemplateItem item)
        {
            var prop = item.Prop;
            var isInGroupBox = item.Parent != null && item.Parent.Directive == "group-box";

            return prop.BuildViewPropInfo(
                isGroupedByTarget: isInGroupBox,
                selectable:
                    context.IsEditableView &&
                    prop.IsEditable &&
                    prop.IsSelector);
        }

        public virtual void ORunLabel(WebViewGenContext context, string text)
        {
            O($"<label class='{LabelClass}'>{text}</label>");  //  class='k-label'
        }

        public void ORunLabelCore(WebViewGenContext context, string text)
        {
            context.PropInfo = null;
            OLabelContainerBegin(context);
            Attributes.Clear();
            ORunLabel(context, text);
            OLabelContainerEnd(context);
        }

        void OPropLabelCore(WebViewGenContext context, ViewTemplateItem cur)
        {
            var prop = cur.Prop;
            bool inGroupBox = cur.Parent != null && cur.Parent.Directive == "group-box";

            OLabelContainerBegin(context);

            context.PropInfo = CreateViewPropInfo(context, cur);

            Attributes.Clear();
            OPropLabel(context);

            context.PropInfo = null;

            OLabelContainerEnd(context);

        }

        public Action<WebViewGenContext> OBlockBegin { get; set; } = (context) => { };
        public Action<WebViewGenContext> OBlockEnd { get; set; } = (context) => { };

        public Action<WebViewGenContext> OLabelContainerBegin { get; set; } = (context) => { };

        public virtual void OPropLabel(WebViewGenContext context)
        {
            var vitem = context.PropInfo;

            Oo($"<label for='{vitem.PropPath}' class='{LabelClass}'>");

            o(GetDisplayNameFor(context));

            oO("</label>");
        }

        public string GetDisplayNameFor(WebViewGenContext context)
        {
            var info = context.PropInfo;

            // Show customized text if explicitely defined on the view property.
            if (info.CustomDisplayLabel != null)
                return info.CustomDisplayLabel;
            else
                // KABU TODO: This is never hit in our current project. 
                return $"@(Html.DisplayNameFor(m => m.{info.PropPath}))";
        }

        public Action<WebViewGenContext> OLabelContainerEnd { get; set; } = (context) => { };

        public Action<WebViewGenContext> OPropContainerBegin { get; set; } = (context) => { };
        public virtual void OProp(WebViewGenContext context)
        { }

        public Action<WebViewGenContext> OPropContainerEnd { get; set; } = (context) => { };

        public virtual void OText(WebViewGenContext context, ViewTemplateItem cur)
        {
            O($"<span>{cur.TextValue}</span>");

#if (false)
            var run = cur.GetRunRangeBefore(x => x.Prop != null).ToArray();
            if (run.Any())
            {
                Oo("#if (");
                ONotNull(run.Select(x => x.Prop));
                oO(") {{#{0}#}}# ", cur.TextValue);
            }
            else
            {
                oO(cur.TextValue);
            }
#endif
        }

        public void ElemStyleDefaultWidth()
        {
            ElemStyle("width:95%");
        }
    }
}
