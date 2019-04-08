﻿using System;
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
        public string LabelClass { get; set; } = "control-label"; // "col-sm-3 col-xs-12 control-label";
        public string PropContainerClass { get; set; } = "col-sm-9 col-xs-12";

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

        

        

        public virtual void Define(WebViewGenContext context)
        {
            OPropContainerBegin = (c) => XB($"<div class='{PropContainerClass}'>");
            OPropContainerEnd = (c) => XE("</div>");
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
                        OMvcPartialView(cur.Name);
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

                            // TODO: REMOVE: OLabelContainerBegin(context);
                            ORunLabelCore(context, label.TextValue);
                            // TODO: REMOVE: OLabelContainerEnd(context);

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

                if (cur.VisibilityCondition is MojFormedType visibilityConditionObject)
                {
                    predicate = $" data-bind='visible: {GetBinding(visibilityConditionObject)}'";
                }
                else if (cur.VisibilityCondition is MojViewMode hideModes)
                {
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
            O($"<label class='{LabelClass}'>{text}</label>");
        }

        public void ORunLabelCore(WebViewGenContext context, string text)
        {
            context.PropInfo = null;
            OLabelContainerBegin(context);
            Attributes.Clear();
            ORunLabel(context, text);
            OLabelContainerEnd(context);
        }

        // TODO: REMOVE? Not used
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

            ElemClass(LabelClass, target: "label");

            Oo($"<label for='{vitem.PropPath}'{GetElemAttrs("label")}>");

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
