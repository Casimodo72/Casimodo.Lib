using Casimodo.Lib;

namespace Casimodo.Mojen.App.Generators.Blazor.Blazorise;

public abstract class BlazoriseTypeViewGen : BlazoriseViewGen
{
    protected ViewModelLayerConfig ViewModelConfig { get; private set; }

    protected override void GenerateCore()
    {
        base.GenerateCore();

        ViewModelConfig = App.Get<ViewModelLayerConfig>();
    }

    public void GenerateView(WebViewGenContext context)
    {
        Define(context);

        BeginView(context);

        if (!context.View.Template.IsEmpty)
            OAny(context, context.View.Template.Root);

        EndView(context);

        AfterView(context);
    }

    public virtual void Define(WebViewGenContext context)
    {
        OPropContainerBegin = c => XB($"<div PROP_CONTAINER class='{PropContainerClass}'>");
        OPropContainerEnd = c => XE("</div>");
    }

    public virtual void BeginView(WebViewGenContext context)
    { }

    public virtual void OListBegin(WebViewGenContext context, ViewTemplateItem cur)
    { }

    public virtual void OListEnd(WebViewGenContext context, ViewTemplateItem cur)
    { }

    public Action<WebViewGenContext> OBlockBegin { get; set; } = context => { };
    public Action<WebViewGenContext> OBlockEnd { get; set; } = context => { };

    public Action<WebViewGenContext> OPropRunBegin { get; set; } = context => { };

    public string LabelContainerClass { get; set; } = "col-sm-3 col-xs-12";

    public Action<WebViewGenContext> OLabelContainerBegin { get; set; } = context => { };

    public string LabelClass { get; set; } = "";

    void OPropLabel(WebViewGenContext context, ViewTemplateItem cur)
    {
        var vprop = cur.Prop;
        bool inGroupBox = cur.Parent.Directive == "group-box";

        ClearAttrs("label");

        OLabelContainerBegin(context);

        context.PropInfo = CreateViewPropInfo(context, cur);

        OPropLabelCore(context);

        context.PropInfo = null;

        ClearAttrs("label");

        OLabelContainerEnd(context);
    }

    public virtual void OPropLabelCore(WebViewGenContext context)
    {
        ClassAttr(LabelClass, target: "label");

        Oo($"<Label For='{GetElementId(context.PropInfo)}'{GetAttrs("label")}>");

        o(GetDisplayNameFor(context));

        oO("</Label>");
    }

    public void ORunLabel(WebViewGenContext context, string text)
    {
        ClearAttrs("label");
        context.PropInfo = null;

        OLabelContainerBegin(context);

        ORunLabelCore(context, text);

        OLabelContainerEnd(context);

        ClearAttrs("label");
    }

    public virtual void ORunLabelCore(WebViewGenContext context, string text)
    {
        ClassAttr(LabelClass, target: "label");

        Oo($"<Label {GetAttrs("label")}>{text}</Label>");
    }

    public Action<WebViewGenContext> OLabelContainerEnd { get; set; } = context => { };

    public string RunTextClass { get; set; } = "km-run-text";

    public virtual void ORunText(WebViewGenContext context, ViewTemplateItem cur)
    {
        O($"<span class='{RunTextClass}'>{cur.TextValue}</span>");
    }

    public string PropContainerClass { get; set; } = "col-sm-9 col-xs-12";

    public Action<WebViewGenContext> OPropContainerBegin { get; set; } = context => { };

    public string FormGroupClass { get; set; } = "input-group";
    public string FormGroupReadOnlyClass { get; set; } = "input-group readonly";

    public virtual void OProp(WebViewGenContext context)
    { }

    public Action<WebViewGenContext> OPropContainerEnd { get; set; } = context => { };

    public Action<WebViewGenContext> OPropRunEnd { get; set; } = context => { };

    public virtual void EndView(WebViewGenContext context)
    { }

    public virtual void AfterView(WebViewGenContext context)
    {
        // NOP
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
            var runs = item.GetRuns().ToList();

            // Read-only views: filter out hidden items.
            if (context.View.Kind.Mode == MojViewMode.Read)
                runs = FilterHiddenProps(runs, MojViewMode.Read).ToList();

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

    public virtual bool OContainerBegin(WebViewGenContext context, ViewTemplateItem cur)
    {
        if (cur.Directive == "grid")
        {
            XB("<div class='row'>");
        }
        else if (cur.Directive == "column")
        {
            XB($"<div class='{GetColumnClasses(context, cur)}'{GetCssStyle(context, cur)}>");
        }
        else if (cur.Directive == "group-box")
        {
            // Card
            var @class = "card";
            var predicate = "";

            if (cur.VisibilityCondition is MojFormedType visibilityConditionObject)
            {
                // TODO: IMPL
                predicate = $" data-bind='visible: {GetBinding(visibilityConditionObject)}'";
            }
            else if (cur.VisibilityCondition is MojViewMode hideModes)
            {
                if (context.View.IsEditor)
                {
                    foreach (var mode in hideModes.GetAtomicFlags())
                    {
                        // TODO: IMPL
                        @class += " remove-on-" + mode;
                    }
                }

                if (!context.View.IsEditor && hideModes.HasFlag(MojViewMode.Read))
                {
                    // If read-only view and shall not be visible:
                    //   Skip this container entirely.
                    return false;
                }
            }

            XB($"<div class='{@class}'{predicate}>");

            // Card header
            O("<div class='card-header'>{0}</div>", cur.TextValue);

            // Card body
            XB($"<div class='card-body'{GetCssStyle(context, cur)}>");
        }

        return true;
    }

    public virtual void OContainerEnd(WebViewGenContext context, ViewTemplateItem cur)
    {
        if (cur.Directive == "grid")
        {
            XE("</div>"); // row
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

    public IEnumerable<ViewTemplateItem> CurrentRun { get; set; }

    public IEnumerable<ViewTemplateItem> CurrentRunProps
    {
        get { return CurrentRun.Where(x => x.Directive != "label" && x.Prop != null); }
    }

    public virtual bool ORunBegin(WebViewGenContext context)
    {
        return !IsRunSingleCustomView(context);
    }

    void ORunCore(WebViewGenContext context, List<ViewTemplateItem> run)
    {
        CurrentRun = run;
        ViewTemplateItem label = null;
        bool isPropRun = run.Any(x => x.Prop != null);
        bool isPropRunStarted = false;

        if (isPropRun)
        {
            OPropRunBegin(context);
        }

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
                    // TODO: Reference custom view.
                    throw new NotSupportedException("Custom blazor views are not supported yet.");
                    // OMvcPartialView(cur.Name);
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
                        ORunLabel(context, label.TextValue);

                        label = null;
                    }
                    else if (cur.Prop != null)
                    {
                        OPropLabel(context, cur);
                        label = null;
                    }
                }

                if (cur.TextValue != null)
                {
                    // Output text
                    ORunText(context, cur);
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

        if (isPropRun)
        {
            OPropRunEnd(context);
        }
    }

    public virtual bool ORunEnd(WebViewGenContext context)
    {
        return !IsRunSingleCustomView(context);
    }

    public bool IsRunSingleCustomView(WebViewGenContext context)
    {
        return context.Run.Count == 1 && context.Run.FirstOrDefault()?.Directive == "custom-view";
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

    public virtual string GetCssStyle(WebViewGenContext context, ViewTemplateItem cur)
    {
        if (cur.Style is not Style config)
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
        if (cur.Style is Style col && col.Col != null)
            return col.Col;

        // Get number of columns
        int colNum = cur.Parent.GetChildren().Count(x => x.Directive == "column");
        //var modal = context.View.IsModal || context.IsModalView;

        // We are using bootstrap, so use 12 max cols.
        int span = 12 / colNum;

        return $"col-sm-{span}";
    }

    public MojViewPropInfo CreateViewPropInfo(WebViewGenContext context, ViewTemplateItem item)
    {
        var prop = item.Prop;
        var isInGroupBox = item.Parent.Directive == "group-box";

        return prop.BuildViewPropInfo(
            isGroupedByTarget: isInGroupBox,
            selectable:
                context.IsEditableView &&
                prop.IsEditable &&
                prop.IsSelector);
    }

    public string GetDisplayNameFor(WebViewGenContext context)
    {
        var info = context.PropInfo;

        // Show customized text if explicitely defined on the view property.
        if (info.CustomDisplayLabel != null)
            return info.CustomDisplayLabel;
        else
        {
            throw new NotSupportedException("Display names via data attributes are not supported anymore.");
        }
    }
}
