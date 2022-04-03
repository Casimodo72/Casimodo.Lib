using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Casimodo.Mojen.Blazorise;

public class BlazoriseFormEditorGen : BlazoriseTypeViewGen
{
    protected override void GenerateCore()
    {
        base.GenerateCore();

        foreach (MojViewConfig view in App.GetItems<MojViewConfig>()
            .Where(x => x.Uses(this)))
        {

            DataViewModelAccessor = view.TypeConfig.Name;

            var context = new WebViewGenContext
            {
                View = view,
                IsEditableView = true,
                ViewRole = "editor"
            };

            PerformWrite(
                context.View,
                () => GenerateView(context));
        }
    }

    public override void Define(WebViewGenContext context)
    {
        base.Define(context);
        //ReadOnlyGen.Define(context);
        //ReadOnlyGen.DataViewModelAccessor = null;

        OLabelContainerBegin = c =>
        {
            //var style = c.Cur.GetGroupLabelStyle();
            //XB($"<div class='{style ?? LabelContainerClass}'>");
        };

        OLabelContainerEnd = c =>
        {
            // XE("</div>");
        };

        OPropContainerBegin = c =>
        {
            XB($"<div class='{c.Cur.GetGroupPropStyle() ?? PropContainerClass}'>");
            XB($"<div class='km-input-group-container'>");
        };

        OPropContainerEnd = c =>
        {
            XE("</div>");
            XE("</div>");
        };
    }

    public override void BeginView(WebViewGenContext context)
    {
        base.BeginView(context);

        O($"@if ({context.View.TypeConfig.Name} == null) return;");
        O();
    }

    public override void AfterView(WebViewGenContext context)
    {
        base.AfterView(context);

        var type = context.View.TypeConfig;

        OBlazorCode(() =>
        {
            O("[Parameter, EditorRequired]");
            O($"public {type.Name} {type.Name} {{ get; set; }}");
        });
    }

    public override void OProp(WebViewGenContext context)
    {
        var info = context.PropInfo;
        var vprop = info.ViewProp;

        // TODO: UsedViewPropInfos.Add(info);

        if (info.ViewProp.IsEditable)
        {
            // Editor
            // TODO: Inline styles on Blazor components?
            if (vprop.Width != null)
                ElemStyle($"width:{vprop.Width}px !important");
            if (vprop.MaxWidth != null)
                ElemStyle($"max-width:{vprop.MaxWidth}px !important");

            OPropEditable(context);
        }
        else
        {
            // Read-only property.
            // TODO: IMPL
            //ReadOnlyGen.ElemClass("km-readonly-form-control");
            //ReadOnlyGen.OProp(context);
        }
    }

    public void OPropEditable(WebViewGenContext context)
    {
        if (OPropSelector(context))
            return;

        OPropEditableCore(context);
    }

    public void OPropEditableCore(WebViewGenContext context)
    {
        var vinfo = context.PropInfo;
        var vprop = vinfo.ViewProp;
        var ppath = vinfo.PropPath;
        var dprop = vinfo.TargetDisplayProp;
        var vpropType = vinfo.TargetDisplayProp.Type;

        // MVC jQuery validation : see https://www.blinkingcaret.com/2016/03/23/manually-use-mvc-client-side-validation/
        bool validationBox = true;

        ElemAttr("ElementId", ppath.Replace(".", "_"));

        ElemAttr("Name", ppath);

        // CustomElemStyle(context);

        // Add "form-control" class.
        // Except for Kendo's numeric boxes, which just break if using bootstrap's form-control class.
        //if (!vpropType.IsNumber)
        //{
        //    ElemClass("form-control");
        //}

        if (vprop.IsAutocomplete == false)
            ElemAttr("autocomplete", "false");

        // Enable MVC's unobtrusive jQuery validation.
        ElemAttr("data-val", true);

        if (vprop.CustomEditorViewName != null)
        {
            OTODO("Custum editor");
            // TODO: OMvcPartialView(vprop.CustomEditorViewName);
        }
        // NOTE: Enums are also numbers here, so ensure the enum handler comes first.
        else if (vpropType.IsEnum)
        {
            throw new MojenException("Enums are not supported.");
        }
        else if (vpropType.IsNumber)
        {
            OTODO("Number");
            ONumericInput(context);
            // TODO: OKendoNumericInput(context);
            validationBox = false;
        }
        else if (dprop.FileRef.Is)
        {
            throw new MojenException("Unsupported file reference property.");
        }
        else if (dprop.Reference.Is)
        {
            throw new MojenException("Unsupported reference property.");
        }
        else if (dprop.IsColor)
        {
            OTODO("Color picker");
            // TODO: OKendoColorPicker(context);
        }
        else if (vpropType.IsAnyTime)
        {
            OTODO("Date-time picker");
            ODateTimePicker(context);
        }
        else if (vpropType.IsTimeSpan)
        {
            OTODO("Date-time range picker");
            // TODO:  OKendoTimeSpanEditor(context);
        }
        else if (vpropType.IsString)
        {
            OTextInput(context);
        }
        else if (vpropType.IsBoolean)
        {
            Oo("<Switch TValue=bool");
            oBindValue(context);
            oO(" />");
            // TODO: OKendoCheckbox(context);
        }
        else
        {
            throw new MojenException("Unsupported editor property kind.");
        }

        // Validation message
        if (validationBox)
        {
            // OInvalidPropPlaceholder(context);
            // TODO: REMOVE: OValidationMessageElem(ppath);
        }
    }

    public void OTextInput(WebViewGenContext context)
    {
        var vprop = context.PropInfo.ViewProp;
        var dprop = context.PropInfo.Prop;
        var ppath = context.PropInfo.PropPath;

        if (!dprop.IsSpellCheck)
            ElemAttr("spellcheck", false);

        if (dprop.Type.IsMultilineString)
        {
            Oo($@"<MemoEdit");

            if (dprop.RowCount != 0)
            {
                ElemAttr("Rows", dprop.RowCount);
            }
            // TODO: ? if (dprop.ColCount != 0) ElemAttr("cols", dprop.ColCount);

            // TODO: IMPORTANT: Check whether Required and LocallyRequired works.

            oElemAttrs();
            OHtmlRequiredttrs(context, dprop);
            oBindValue(context);

            // TODO: Custom editor
            //if (vprop.UseCodeRenderer != null)
            //    ElemAttr("data-use-renderer", vprop.UseCodeRenderer);

            oO("></MemoEdit>");
            /*
                <textarea class="form-control form-control" cols="20" data-val="true" data-val-length="Das Feld &quot;Notizen&quot; muss eine Zeichenfolge mit einer maximalen Länge von 4096 sein." data-val-length-max="4096" id="Notes" name="Notes" rows="6" spellcheck="false">
                </textarea>
            */
        }
        else
        {
            var inputType = GetTextInputType(dprop.Type.AnnotationDataType);
            if (inputType != null)
                ElemAttr("Type", inputType);
            //else
            //    ElemAttr("type", "text");

            ElemClass("k-textbox");

            Oo($@"<TextEdit");
            // TODO: IMPORTANT: Check whether Required and LocallyRequired works.
            oElemAttrs();
            OHtmlRequiredttrs(context, dprop);
            oBindValue(context);

            // Postal code
            if (dprop.Type.AnnotationDataType == DataType.PostalCode)
            {
                o(@" data-val-length-min='5' data-val-length-max='5' data-val-length='Die Postleitzahl muss fünfstellig sein.'");
                o(@" data-val-regex='Die Postleitzahl muss eine fünfstellige Zahl sein.' data-val-regex-pattern='^\d{5}$'");
            }
            // Min/max length
            else
                oLengthValidationAttrs(context);

            oO("/>");
        }
    }

    void ONumericInput(WebViewGenContext context)
    {
        var vprop = context.PropInfo.ViewProp;
        var dprop = context.PropInfo.TargetDisplayProp;
        var ppath = context.PropInfo.PropPath;

        ElemAttr("TValue", vprop.Type.Name);

        Oo($"<NumericEdit");
        oBindValue(context, ppath);
        oElemAttrs();
        oO("/>");
    }

    void ODateTimePicker(WebViewGenContext context)
    {
        var vprop = context.PropInfo.ViewProp;
        var dprop = context.PropInfo.TargetDisplayProp;

        var time = vprop.DisplayDateTime ?? dprop.Type.DateTimeInfo;

        ElemAttr("data-display-name", vprop.DisplayLabel);

        //string role;
        //if (time.IsDateAndTime)
        //    role = "datetimepicker";
        //else if (time.IsDate)
        //    role = "datepicker";
        //else
        //    role = "timepicker";
        //ElemAttr("data-role", role);

        ElemAttr("TValue", vprop.Type.Name);

        if (time.IsDateAndTime)
        {
            ODateTimePickerCore(context, "DatePicker");
            ODateTimePickerCore(context, "TimePicker");
        }
        else if (time.IsDate)
            ODateTimePickerCore(context, "DatePicker");
        else
            ODateTimePickerCore(context, "TimePicker");
    }

    void ODateTimePickerCore(WebViewGenContext context, string component)
    {
        Oo($"<{component}");
        oBindValue(context, context.PropInfo.PropPath);
        oElemAttrs();
        oO("/>");
    }

    //void OKendoTimeSpanEditor(WebViewGenContext context)
    //{
    //    var vprop = context.PropInfo.ViewProp;
    //    var dprop = context.PropInfo.TargetDisplayProp;
    //    var ppath = context.PropInfo.PropPath;
    //    Oo("<div"); oElemAttrs(); oO(">");
    //    Push();
    //    OKendoNumericInput(context, ppath: ppath + ".Hours", min: 0, max: 23, validationElem: false);
    //    OKendoNumericInput(context, ppath: ppath + ".Minutes", min: 0, max: 59, validationElem: false);
    //    Pop();
    //    O("</div>");
    //    OInvalidPropPlaceholder(ppath + ".Hours");
    //    OInvalidPropPlaceholder(ppath + ".Minutes");
    //}

    public bool OPropSelector(WebViewGenContext context)
    {
        return context.PropInfo.ViewProp.IsSelector;
        //return
        //    OPropTagsSelector(context) ||
        //    OPropSnippetsEditor(context) ||
        //    OPropSequenceSelector(context) ||
        //    OPropLookupSelectorDialog(context) ||
        //    OPropGeoPlaceLookupSelectorDialog(context) ||
        //    OPropDropDownSelector(context);
    }

    void oBindValue(WebViewGenContext context, string binding = null)
    {
        o($@" @bind-Value={GetBinding(context)}");
    }

    void OHtmlRequiredttrs(WebViewGenContext context, MojProp prop)
    {
        if (prop.IsRequiredOnEdit)
        {
            o(" required");
            // KABU TODO: LOCALIZE
            o($@" data-val-required='{GetDisplayNameFor(context)}' ist erforderlich.'");
        }
    }

#pragma warning disable IDE1006 // Naming Styles
    void oLengthValidationAttrs(WebViewGenContext context)
#pragma warning restore IDE1006 // Naming Styles
    {
        var vprop = context.PropInfo.ViewProp;
        var dprop = context.PropInfo.Prop;
        var ppath = context.PropInfo.PropPath;

        var min = dprop.Rules.Min ?? vprop.Rules.Min;
        var max = dprop.Rules.Max ?? vprop.Rules.Max;

        if (min == null && max == null)
            return;

        if (min != null)
            o($@" data-val-length-min='{min}'");
        if (max != null)
            o($@" data-val-length-max='{max}'");

        // Validation error text.
        o($@" data-val-length='Feld “{GetDisplayNameFor(context)}” ");
        if (min != null)
            o($@"muss mindestens {min} Zeichen");
        if (max != null)
        {
            if (min != null)
                o(@" und ");
            o($@"darf maximal {max} Zeichen");
        }
        o(@" lang sein.'");
    }

#pragma warning disable IDE1006 // Naming Styles
    public void oAttr(string name, object value)
#pragma warning restore IDE1006 // Naming Styles
    {
        o($" {name}='{Moj.ToJsXAttrValue(value)}'");
    }

    string GetTextInputType(System.ComponentModel.DataAnnotations.DataType? type)
    {
        if (type == null)
            return null;

        // HTML input types:
        // color
        // date
        // datetime
        // datetime - local
        // email
        // month
        // number
        // range
        // search
        // tel
        // time
        // url
        // week
        if (_textInputTypeByDataType.TryGetValue(type.Value, out string result))
            return result;

        return null;
    }

    static readonly Dictionary<System.ComponentModel.DataAnnotations.DataType, string> _textInputTypeByDataType =
        new()
        {
            [DataType.EmailAddress] = "email",
            [DataType.PhoneNumber] = "tel",
            [DataType.Url] = "url",
            [DataType.DateTime] = "datetime",
            [DataType.Date] = "date",
            [DataType.Time] = "time",
            [DataType.Currency] = "number",
            [DataType.Password] = "password"
        };
}

