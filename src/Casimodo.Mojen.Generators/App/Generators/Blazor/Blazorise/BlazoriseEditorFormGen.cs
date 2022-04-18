using Casimodo.Lib.Data;
using System.ComponentModel.DataAnnotations;

namespace Casimodo.Mojen.App.Generators.Blazor.Blazorise;

#nullable enable
public class BlazoriseEditorFormGen : BlazoriseFormGen
{
    protected override void GenerateCore()
    {
        base.GenerateCore();

        foreach (MojViewConfig view in App.GetItems<MojViewConfig>().Where(x => x.Uses(this)))
        {

            DataViewModelAccessor = view.TypeConfig.Name;

            var context = new WebViewGenContext
            {
                View = view,
                IsEditableView = true,
                ViewRole = "editor"
            };

            Write(
                context.View,
                () => GenerateView(context));
        }
    }

    public override void Define(WebViewGenContext context)
    {
        base.Define(context);

        // TODO:
        //ReadOnlyGen.Define(context);
        //ReadOnlyGen.DataViewModelAccessor = null;

        OBlockBegin = c =>
        {
            ORazorComment("BLOCK BEGIN");
        };

        OBlockEnd = c =>
        {
            ORazorComment("BLOCK END");
        };

        OLabelContainerBegin = c =>
        {
        };

        OLabelContainerEnd = c =>
        {
        };

        OPropContainerBegin = c =>
        {
            XB("<FieldBody>");
        };

        OPropContainerEnd = c =>
        {
            XE("</FieldBody>");
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

            foreach (var codeProp in _codeProps)
            {
                if (!OCodeProp(codeProp))
                {
                    throw new MojenException("No code section generator found.");
                }
            }
        });
    }

    bool OCodeProp(MojViewPropInfo codeProp)
    {
        return OPropLookupSelectorCode(codeProp);
    }

    public override void OProp(WebViewGenContext context)
    {
        var vprop = context.PropInfo.ViewProp;

        // TODO: ? UsedViewPropInfos.Add(info);

        // Editor
        // TODO: ? Inline styles on Blazor components?
        if (vprop.Width != null)
            StyleAttr($"width:{vprop.Width}px !important");
        if (vprop.MaxWidth != null)
            StyleAttr($"max-width:{vprop.MaxWidth}px !important");

        if (vprop.IsEditable && OPropSelector(context))
            return;

        var propInfo = context.PropInfo;
        // var ppath = propInfo.PropPath;
        var dprop = propInfo.TargetDisplayProp;
        var vpropType = propInfo.TargetDisplayProp.Type;

        bool validationBox = true;

        Attr("ElementId", GetElementId(propInfo));

        // TODO: REMOVE? Attr("Name", ppath);

        if (vprop.IsAutocomplete == false)
            Attr("autocomplete", "false");

        if (!vprop.IsEditable)
        {
            Attr("Disabled", true);
        }

        // TODO: REMOVE
        // Enable MVC's unobtrusive jQuery validation.
        // Attr("data-val", true);

        if (vprop.CustomEditorViewName != null)
        {
            ORazorTODO("Custom editor");
            // TODO: OMvcPartialView(vprop.CustomEditorViewName);
        }
        // NOTE: Enums are also numbers here, so ensure the enum handler comes first.
        else if (vpropType.IsEnum)
        {
            throw new MojenException("Enums are not supported.");
        }
        else if (dprop.FileRef.Is)
        {
            throw new MojenException("Unsupported file reference property.");
        }
        else if (dprop.Reference.Is)
        {
            throw new MojenException("Unsupported reference property.");
        }
        else if (vpropType.IsNumber)
        {
            ONumericInput(context);
            validationBox = false;
        }
        else if (dprop.IsColor)
        {
            ORazorTODO("Color picker");
            // TODO: OKendoColorPicker(context);
        }
        else if (vpropType.IsAnyTime)
        {
            ODateTimePicker(context);
        }
        else if (vpropType.IsTimeSpan)
        {
            ORazorTODO("Date-time range picker");
            // TODO: OKendoTimeSpanEditor(context);
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
        }
        else
        {
            throw new MojenException("This property is not supported on forms.");
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

        var max = dprop.Rules.Max ?? vprop.Rules.Max;
        if (max != null)
            Attr("MaxLength", max);

        if (!dprop.IsSpellCheck)
            Attr("spellcheck", false);

        if (dprop.Type.IsMultilineString)
        {
            Oo($@"<MemoEdit");

            oBindText(context);

            if (dprop.RowCount != 0)
            {
                Attr("Rows", dprop.RowCount);
            }

            // TODO: Columns? if (dprop.ColCount != 0) ElemAttr("cols", dprop.ColCount);

            oAttrs();
            OHtmlRequiredttrs(context, dprop);

            // TODO: Custom editor
            //if (vprop.UseCodeRenderer != null)
            //    ElemAttr("data-use-renderer", vprop.UseCodeRenderer);

            oO(">");
            if (vprop.IsEditable)
            {
                OValidationError();
            }
            O("</MemoEdit>");
        }
        else
        {
            var role = GetTextRole(dprop.Type.AnnotationDataType);
            if (role != null)
                Attr("Role", role);

            Oo($@"<TextEdit");
            oBindText(context);
            oAttrs();
            OHtmlRequiredttrs(context, dprop);

            if (vprop.IsEditable)
            {
                // TODO: Postal code
                if (dprop.Type.AnnotationDataType == DataType.PostalCode)
                {
                    //o(@" data-val-length-min='5' data-val-length-max='5' data-val-length='Die Postleitzahl muss fünfstellig sein.'");
                    //o(@" data-val-regex='Die Postleitzahl muss eine fünfstellige Zahl sein.' data-val-regex-pattern='^\d{5}$'");
                }
                else
                {
                    // Min/max length
                    oLengthValidationAttrs(context);
                }
            }

            oO(">");
            if (vprop.IsEditable)
            {
                OValidationError();
            }
            O("</TextEdit>");
        }
    }

    void OValidationError()
    {
        Push();
        O("<Feedback>");
        O("    <ValidationError />");
        O("</Feedback>");
        Pop();
    }

    void ONumericInput(WebViewGenContext context)
    {
        var vprop = context.PropInfo.ViewProp;
        var dprop = context.PropInfo.TargetDisplayProp;
        var ppath = context.PropInfo.PropPath;

        Attr("TValue", vprop.Type.Name);

        var min = dprop.Rules.Min ?? vprop.Rules.Min;
        if (min != null)
        {
            Attr("Min", min);
        }
        var max = dprop.Rules.Max ?? vprop.Rules.Max;
        if (max != null)
        {
            Attr("Max", max);
        }

        Oo($"<NumericEdit");
        oBindValue(context);
        oAttrs();
        oO(">");
        if (vprop.IsEditable)
        {
            OValidationError();
        }
        O($"</NumericEdit>");
    }

    void ODateTimePicker(WebViewGenContext context)
    {
        var vprop = context.PropInfo.ViewProp;
        var dprop = context.PropInfo.TargetDisplayProp;

        var time = vprop.DisplayDateTime ?? dprop.Type.DateTimeInfo;

        // TODO: REMOVE? Attr("data-display-name", vprop.DisplayLabel);

        //string role;
        //if (time.IsDateAndTime)
        //    role = "datetimepicker";
        //else if (time.IsDate)
        //    role = "datepicker";
        //else
        //    role = "timepicker";
        //ElemAttr("data-role", role);

        Attr("TValue", vprop.Type.Name);

        if (time.IsDateAndTime)
        {
            ODateTimePickerCore(context, "DatePicker", consumeAttrs: false);
            ODateTimePickerCore(context, "TimePicker");
        }
        else if (time.IsDate)
            ODateTimePickerCore(context, "DatePicker");
        else
            ODateTimePickerCore(context, "TimePicker");
    }

    void ODateTimePickerCore(WebViewGenContext context, string component, bool consumeAttrs = true)
    {
        Oo($"<{component}");
        oBindValue(context);
        oAttrs(consume: consumeAttrs);
        oO(">");
        if (context.PropInfo.ViewProp.IsEditable)
        {
            OValidationError();
        }
        O($"</{component}>");
    }

    // TODO:
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

    readonly List<MojViewPropInfo> _codeProps = new();

    public bool OPropSelector(WebViewGenContext context)
    {
        if (!context.PropInfo.ViewProp.IsSelector)
        {
            return false;
        }

        var wasGenerated =
            context.PropInfo.ViewProp.IsSelector &&
           //    OPropTagsSelector(context) ||
           //    OPropSnippetsEditor(context) ||
           //    OPropSequenceSelector(context) ||
           OPropLookupSelectorView(context)
        //    OPropGeoPlaceLookupSelectorDialog(context) ||
        //    OPropDropDownSelector(context)
        ;

        if (!wasGenerated)
        {
            ORazorTODO("Selectors + lookups");
        }


        return true;
    }

    bool OPropLookupSelectorView(WebViewGenContext context)
    {
        var type = context.View.TypeConfig;
        var info = context.PropInfo;
        var vprop = info.ViewProp;
        var prop = info.Prop;
        var propPath = info.PropPath;

        if (!vprop.IsSelector) return false;
        if (!vprop.Lookup.Is) return false;

        if (!prop.Reference.IsToOne)
            throw new MojenException($"Unsupported reference multiplicity '{prop.Reference.Multiplicity}'.");

        if (!prop.Reference.Binding.HasFlag(MojReferenceBinding.Loose))
            throw new MojenException($"Unsupported reference binding '{prop.Reference.Binding}'.");

        // Lookup dialog
        var lookupView = vprop.LookupDialog;
        var lookupViewFieldName = "_" + BuildLookupViewName(lookupView).FirstLetterToLower();
        var lookupComponentName = BuildLookupViewComponentName(lookupView);

        OButton("Open" + lookupView.Alias, text: "Select");

        O($"<{lookupComponentName} @ref={lookupViewFieldName} />");

        _codeProps.Add(context.PropInfo);

        return true;
    }

    bool OPropLookupSelectorCode(MojViewPropInfo codeProp)
    {
        if (!codeProp.ViewProp.Lookup.Is)
            return false;

        var vprop = codeProp.ViewProp;
        var prop = codeProp.Prop;
        var propPath = codeProp.PropPath;

        var lookupView = vprop.LookupDialog;
        var lookupViewName = BuildLookupViewName(lookupView);
        var lookupViewFieldName = "_" + lookupViewName.FirstLetterToLower();
        var lookupComponentName = BuildLookupViewComponentName(lookupView);

        O();
        O($"{lookupComponentName}? {lookupViewFieldName};");

        O();
        O($"Task Open{lookupViewName}()");
        Begin();
        O($"return {lookupViewFieldName}.Show(); ");
        End();

        return true;
    }

    void OButton(string onclick, string text)
    {
        O($"<button class='btn btn-secondary' type='button' @onclick={onclick} @onclick:stopPropagation>{text}</button>");
    }

    void oBindValue(WebViewGenContext context)
    {
        oBindCore(context, "Value");
    }

    void oBindText(WebViewGenContext context)
    {
        oBindCore(context, "Text");
    }

    void oBindCore(WebViewGenContext context, string propName)
    {
        if (context.PropInfo.ViewProp.IsEditable)
        {
            o($@" @bind-{propName}={GetBinding(context)}");
        }
        else
        {
            o($@" {propName}=@{GetBinding(context)}");
        }
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
        // TODO: IMPL?
        return;

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

    string? GetTextRole(DataType? type)
    {
        // Blazorise has a TextRole enum.
        return type != null && _textInputTypeByDataType.TryGetValue(type.Value, out string result)
            ? result
            : null;

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
    }

    static readonly Dictionary<DataType, string> _textInputTypeByDataType = new()
    {
        [DataType.EmailAddress] = "Email",
        [DataType.PhoneNumber] = "Telephone",
        [DataType.Url] = "Url",
        [DataType.Password] = "Password"
    };
}

