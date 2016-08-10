using Casimodo.Lib;
using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoEditorGen : KendoTypeViewGenBase
    {
        public KendoDetails2Gen ReadOnlyGen { get; set; } = new KendoDetails2Gen();

        public KendoPartGen KendoGen { get; set; } = new KendoPartGen();

        public List<MojViewPropInfo> UsedViewPropInfos { get; set; } = new List<MojViewPropInfo>();

        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>()
                .Where(x => x.Uses(this)))
            {
                UsedViewPropInfos.Clear();

                if (!view.IsCustom)
                {
                    PerformWrite(view, () => GenerateView(new WebViewGenContext
                    {
                        View = view,
                        IsEditableView = true
                    }));
                }

                var dataViewModelGen = new WebDataEditViewModelGen();
                dataViewModelGen.Initialize(App);
                dataViewModelGen.PerformWrite(Path.Combine(GetViewDirPath(view), BuildViewModelFileName(view)), () =>
                {
                    dataViewModelGen.GenerateEditViewModel(view.TypeConfig, UsedViewPropInfos, view.Group);
                });
            }
        }

        public MojHttpRequestConfig TransportConfig { get; set; }

        public override void AfterView(WebViewGenContext context)
        {
            if (!context.View.Standalone.Is)
                return;

            // View model for standalone editor views.
            OScriptBegin();
            KendoGen.OStandaloneEditorViewModel(context.View);
            OScriptEnd();
        }

        public override MojenGenerator Initialize(MojenApp app)
        {
            base.Initialize(app);

            ReadOnlyGen.SetParent(this);
            KendoGen.SetParent(this);

            return this;
        }

        public override void Define(WebViewGenContext context)
        {
            base.Define(context);
            ReadOnlyGen.Define(context);
            ReadOnlyGen.DataViewModelAccessor = null;

            LabelClass = "control-label";
            LabelContainerClass = "col-sm-3 col-xs-12";
            OLabelContainerBegin = (c) =>
            {
                if (c.IsRunEditable) OB($"<div class='{LabelContainerClass}'>");
            };
            OLabelContainerEnd = (c) =>
            {
                if (c.IsRunEditable) OE("</div>");
            };

            OPropContainerBegin = (c) =>
            {
                if (c.IsRunEditable)
                    OB($"<div class='{PropContainerClass}'>"); // KABU TODO: Do I need this "data-container-for='{c.PropInfo.PropPath}'" ?
                else
                    OB($"<div class='{PropContainerClass}'>");
            };
            OPropContainerEnd = (c) => OE("</div>");
        }

        public override void BeginView(WebViewGenContext context)
        {
            ORazorGeneratedFileComment();

            var type = context.View.TypeConfig;
            ORazorUsing(type.Namespace, "Casimodo.Lib.Web");

            ORazorModel($"{context.View.Group ?? ""}{type.Name}Model");

            if (context.View.Standalone.Is)
            {
                OB($"<div class='k-edit-form-container' id='view-{context.View.Id}'>");
            }

            OB("<div class='form-horizontal component-root'>"); // container-fluid // style='width:95%;float:left'

            // Placeholder div for modal dialog windows.
            O($"<div class='{ModalDialogContainerDivClass}'></div>");

            // Validation error box.
            O("<ul class='validation-errors-box' id='validation-errors-box'></ul>");

            // Hidden entity key field.
            OHiddenInputFor(type.Key.Name);

            // Other hidden fields.
            foreach (var prop in context.View.Props.Where(x => x.HideModes == MojViewMode.All))
            {
                OHiddenInputFor(prop.Name);
            }

            // External fields. Those fields need to be included in the UsedViewPropInfos
            //  because they are intended to be edited via a user-provided custom template.
            foreach (var vprop in context.View.Props.Where(x => x.IsExternal))
            {
                var vinfo = vprop.BuildViewPropInfo();
                UsedViewPropInfos.Add(vinfo);
            }
        }

        public override void EndView(WebViewGenContext context)
        {
            OE("</div>");
            if (context.View.Standalone.Is)
            {
                OB("<div class='k-edit-buttons k-state-default'>");
                O("<a class='k-button k-button-icontext k-primary k-update k-state-disabled' href='#'><span class='k-icon k-update'></span>Speichern</a>");
                O("<a class='k-button k-button-icontext k-cancel' href='#'><span class='k-icon k-cancel'></span>Abbrechen</a>");
                OE("</div>");

                OE("</div>");
            }
        }

        public override void ORunBegin(WebViewGenContext context)
        {
            if (context.Cur.Directive == "custom-view") return;

            // Form group
            OB($"<div class='{GetFormGroupClass(context)}'>");
        }

        public override void ORunEnd(WebViewGenContext context)
        {
            if (context.Cur.Directive == "custom-view") return;

            OE("</div>");
        }

        string GetFormGroupClass(WebViewGenContext context)
        {
            var @class = "";
            if (context.IsRunEditable)
                @class = FormGroupClass;
            else
                @class = FormGroupReadOnlyClass;

            var hideProp = context.RunProps.Select(x => x.Prop).FirstOrDefault(x => x.HideModes != MojViewMode.None);
            if (hideProp != null)
            {
                @class += " " + hideProp.GetHideModesMarker();
            }

            return @class;
        }

        public override void OPropLabel(WebViewGenContext context)
        {
            var info = context.PropInfo;

            if (context.IsRunEditable)
            {
                // Editor label
                if (info.Prop.Type.IsBoolean)
                    // Labels of check-boxes are display right hand singe of the check-box.
                    return;

                Oo($"@Html.LabelFor(m => m.{info.PropPath}");

                // Show customized text if explicitely defined on the view property.
                if (info.CustomDisplayLabel != null)
                    o($", \"{info.CustomDisplayLabel}\"");

                if (!string.IsNullOrEmpty(LabelClass))
                    ElemClass(LabelClass);
                OMvcAttrs(false);

                oO(")");
            }
            else
            {
                // Read-only property label.
                ReadOnlyGen.OPropLabel(context);
            }
        }

        string GetDisplayNameFor(WebViewGenContext context)
        {
            var info = context.PropInfo;

            // Show customized text if explicitely defined on the view property.
            if (info.CustomDisplayLabel != null)
                return info.CustomDisplayLabel;

            return $"@(Html.DisplayNameFor(m => m.{info.PropPath}))";
        }

        public override void ORunLabel(WebViewGenContext context, string text)
        {
            if (context.IsRunEditable)
            {
                O($"<label class='{LabelClass}'>{text}</label>");
            }
            else
            {
                // Read-only run label.
                ReadOnlyGen.ORunLabel(context, text);
            }
        }

        public override void OProp(WebViewGenContext context)
        {
            var info = context.PropInfo;

            UsedViewPropInfos.Add(info);

            if (info.ViewProp.IsEditable)
            {
                // Editor
                OPropEditable(context);
            }
            else
            {
                // Read-only property.
                ReadOnlyGen.OProp(context);
            }
        }

        public class ComponentCascadeFromInfo
        {
            public MojFormedNavigationPathStep FirstLooseStep { get; set; }
            public MojProp ForeignKey { get; set; }
        }

        public ComponentCascadeFromInfo ComputeCascadeFrom(MojViewPropInfo info)
        {
            var result = new ComponentCascadeFromInfo();

            result.FirstLooseStep = info.ViewProp.CascadeFrom.FormedNavigationFrom.FirstLooseStep;
            if (result.FirstLooseStep == null)
                throw new MojenException("The cascade-from path must contain a loose reference property.");

            result.ForeignKey = result.FirstLooseStep.SourceProp.Reference.ForeignKey;

            return result;
        }

        public void OPropEditable(WebViewGenContext context)
        {
            if (OPropSelector(context))
                return;

            OPropEditableCore(context);
        }

        public void oAttr(string name, object value)
        {
            o($" {name}='{MojenUtils.ToJsXAttrValue(value)}'");
        }

        public void OPropEditableCore(WebViewGenContext context)
        {
            var type = context.View.TypeConfig;
            var info = context.PropInfo;
            var vprop = info.ViewProp;
            var prop = info.Prop;
            var propPath = info.PropPath;

            bool validationBox = true;

            CustomElemStyle(context);

            if (!prop.Type.IsNumber)
            {
                //ElemStyle("width:95%");
                // Kendo numeric boxes just break if using bootstrap's form-control class.
                ElemClass("form-control");
            }

            // NOTE: Enums are also numbers here, so ensure the enum handler comes first.
            if (prop.Type.IsEnum)
            {
                // .Name(\"{0}\")
                O($"@(Html.Kendo().DropDownListFor(m => m.{propPath}).DataValueField(\"Value\").DataTextField(\"Text\").ValuePrimitive(true)");
                Push();

                // KABU TODO: IMPORTANT: REMOVE? What's the point of having no NULL value in the UI?
                //if (!prop.IsRequiredOnEdit)
                O(".OptionLabel(\" \")");

                O(".BindTo(PickItemsHelper.ToSelectList<{0}>(nullable: {1}, names: true))",
                    prop.Type.NameNormalized,
                    MojenUtils.ToCsValue(prop.IsRequiredOnEdit));

                OMvcAttrs(context, kendo: true);
                OToClientTemplate();

                Pop();
                O(")");
            }
            else if (prop.Type.IsNumber)
            {
                ONumericInput(prop, propPath);
                validationBox = false;
            }
            // KABU TODO: REMOVE: We don't use the MVC wrapper anymore.
#if (false)
            else if (prop.Type.IsNumber)
            {
                Oo($"@(Html.Kendo().NumericTextBoxFor(m => m.{propPath})");

                // Format: http://docs.telerik.com/kendo-ui/framework/globalization/numberformatting
                // Format: http://stackoverflow.com/questions/15241603/formatting-kendo-numeric-text-box
                var format = "#.##"; // "{0:#.##}";
                if (prop.Type.IsInteger)
                {
                    // Float number
                    format = "#";
                }

                o($".Format(\"{format}\")");

                if (prop.Type.IsInteger)
                    o($".Decimals(0)");

                if (prop.Rules.Is)
                {
                    var constr = prop.Rules;
                    if (constr.Min != null)
                        o($".Min({constr.Min})");
                    if (constr.Max != null)
                        o($".Max({constr.Max})");
                }

                var defaultValue = prop.DefaultValues.ForScenario("OnEdit").WithCommon().FirstOrDefault();
                if (defaultValue != null)
                {
                    if (defaultValue.CommonValue != null)
                    {
                        if (defaultValue.CommonValue == MojDefaultValueCommon.CurrentYear)
                        {
                            o(".Value(DateTime.Now.Year)");
                        }
                        else throw new MojenException($"Unexpected common default value '{defaultValue.CommonValue}'.");
                    }
                    else throw new MojenException($"Unexpected default value object '{defaultValue.Value}'.");
                }

                OMvcAttrs(context, kendo: true);
                OToClientTemplate();
                oO(")");
            }
#endif
            // Uploadable image property
            else if (prop.FileRef.Is && prop.FileRef.IsUploadable && prop.FileRef.IsImage)
            {
                string name = context.View.TypeConfig.ClassName + prop.Alias;
                string alias = prop.Alias;
                string uploadTemplateId = name + "Template";

                // KABU TODO: IMPL currently disabled
                return;
#if (false)

                O("@Html.HiddenFor(m => m.{0})", propPath);
                // KABU TODO: This probably won't work with nested type properties.
                foreach (var relatedProp in prop.AutoRelatedProps) O("@Html.HiddenFor(m => m.{0})", relatedProp.Name);

                if (prop.FileRef.IsImage)
                {
                    // Image-thumbnail
                    OB("<div class='edit-image-thumbnail'>");
                    Oo($"<img id='{alias}ImageThumbnail' alt=''");
                    // KABU TODO: REMOVE: We don't have an URI property anymore.
                    //o($"data-bind='attr: {{ src: {alias}Uri }}'");
                    oO(" />");
                    OE("</div>");
                }

                // Kendo upload template.
                O();
                OB("<script id='{0}' type='text/x-kendo-template'>", uploadTemplateId);
                OB("<div class='file-wrapper'>");
                O("<div style='font-size: 0.9em; text-align:left'>#=name#</div>");
                OE("</div>");
                O("<span class='k-progress' style='height: 10px;'></span>");
                OE("</script>");

                // Kendo upload widget.
                O();
                O("@(Html.Kendo().Upload().Name(\"{0}\")", name);
                Push();
                O(".Multiple(false)");
                O(".Async(a => a.SaveUrl(\"api/UploadFile/{0}\").AutoUpload(true))", name);
                O(".ShowFileList(false)");
                O(".Messages(m => m.StatusUploaded(\" \").HeaderStatusUploaded(\" \").HeaderStatusUploading(\" \").StatusUploading(\" \"))");
                O(".Events(e => e.Success(\"kendomodo.onPhotoUploaded\").Error(\"kendomodo.onFileUploadFailed\").Remove(\"kendomodo.onFileUploadRemoving\"))");
                // Add the Kendo upload template defined above.
                O(".TemplateId(\"{0}\")", uploadTemplateId);

                // Accepted file types (by file extension).
                if (prop.FileRef.IsImage)
                    AddAttr("accept", ".png,.jpg");
                AddAttr("data-file-prop", alias);
                AddAttr("max-width", "100px");
                OMvcAttrs(true);

                OToClientTemplate();

                oO(")");
                Pop();
#endif
            }
            else if (prop.FileRef.Is)
            {
                throw new MojenException($"Unsupported filed reference property.");
            }
            else if (prop.Reference.Is)
            {
                throw new MojenException($"Unsupported reference property.");
            }
            else if (prop.IsColor)
            {
                Oo("@(Html.Kendo().ColorPickerFor(m => m.{0}).Opacity({1})",
                    propPath,
                    MojenUtils.ToCsValue(prop.IsColorWithOpacity));
                OToClientTemplate();
                oO(")");
            }
            else if (prop.Type.IsAnyTime)
            {
                var time = vprop.DisplayDateTime ?? prop.Type.DateTimeInfo;

                string kind;
                if (time.IsDateAndTime)
                    kind = "DateTime";
                else if (time.IsDate)
                    kind = "Date";
                else
                    kind = "Time";

                // KABU TODO: REVISIT: Kendo does not support DateTimeOffset pickers (yet?), only DateTime.                
                if (prop.Type.TypeNormalized == typeof(DateTimeOffset))
                {
                    // Fallback to generic editor.
                    Oo($"@(Html.EditorFor(m => m.{propPath}");
                    OMvcAttrs(context, kendo: false);
                    oO("))");
                }
                else
                {
                    O($"@(Html.Kendo().{kind}PickerFor(m => m.{propPath})");
                    OMvcAttrs(context, kendo: true);
                    OToClientTemplate();
                    oO(")");
                }
            }
            else if (prop.Type.IsTimeSpan)
            {
                OTimeSpanInput(prop, propPath);                
            }
            else if (prop.Type.IsString)
            {
                OStringEditor(context);
            }
            else
            {
                // Fallback to generic editor.
                Oo($"@(Html.EditorFor(m => m.{propPath}");
                OMvcAttrs(context, kendo: false);
                oO("))");
            }

            // Validation message
            if (validationBox)
                O($"@Html.ValidationMessageFor(m => m.{propPath})");
        }

        public void OStringEditor(WebViewGenContext context)
        {
            var prop = context.PropInfo.Prop;

            if (!prop.IsSpellCheck)
                ElemAttr("spellcheck", false);

            if (prop.Type.IsMultilineString)
            {
                Oo($"@(Html.TextAreaFor(m => m.{context.PropInfo.PropPath}");
                ElemClass("form-control");
                if (prop.RowCount != 0)
                    ElemAttr("rows", prop.RowCount);
                OMvcAttrs(context, kendo: false);
                oO("))");
            }
            else
            {
                var inputType = GetTextInputType(prop.Type.AnnotationDataType);
                if (inputType != null)
                    ElemAttr("type", inputType);

                O("@(Html.Kendo().TextBoxFor(m => m.{0})", context.PropInfo.PropPath);
                OMvcAttrs(context, kendo: true);
                OToClientTemplate();
                oO(")");
            }
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
            string result;
            if (_textInputTypeByDataType.TryGetValue(type.Value, out result))
                return result;

            return null;
        }

        static readonly Dictionary<System.ComponentModel.DataAnnotations.DataType, string> _textInputTypeByDataType =
            new Dictionary<System.ComponentModel.DataAnnotations.DataType, string>
            {
                [DataType.EmailAddress] = "email",
                [DataType.PhoneNumber] = "tel",
                [DataType.Url] = "url",
                [DataType.DateTime] = "datetime",
                [DataType.Date] = "date",
                [DataType.Time] = "time",
                [DataType.Currency] = "number"
            };

        void OToClientTemplate()
        {
            // if (IsClientTemplate) o(".ToClientTemplate()");
        }

        // Selectors ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public bool OPropSelector(WebViewGenContext context)
        {
            return
                OPropTagsSelector(context) ||
                OPropSnippetSelector(context) ||
                OPropSequenceSelector(context) ||
                OPropLookupSelectorDialog(context) ||
                OPropGeoPlaceLookupSelectorDialog(context) ||
                OPropDropDownSelector(context);
        }

        // Sequence selector ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~           

        void OSelectorControlTextDisplay(WebViewGenContext context)
        {
            var propPath = context.PropInfo.PropPath;

            // Div that looks like a text box; in order to display it as if it were a read-only text box.
            O($"<div class='input-selector-value'><span data-bind='text:{propPath}'{GetElemAttrs()}></span></div>");
        }

        void OSelectorControlButton(WebViewGenContext context)
        {
            var propPath = context.PropInfo.PropPath;

            OB("<span class='input-group-btn'>");
            OB($"<button class='btn btn-default selector-btn' id='editor-btn-for-{propPath}'{GetElemAttrs()}>");
            //if (!string.IsNullOrEmpty(text)) O($"<span>{text}</span>");
            O("<span class='glyphicon glyphicon-search'></span>");
            OE("</button>");
            OE("</span>");
        }

        void OSelectorControlInvisibleInput(WebViewGenContext context)
        {
            var prop = context.PropInfo.Prop;
            var propPath = context.PropInfo.PropPath;

            // Invisible input field for property binding and validation,            
            Oo($"<input id='{propPath}' name='{propPath}' data-bind='value:{propPath}' class='k-input k-valid'");
            o(" type ='text' style='display: none;' aria-disabled='false' aria-readonly='false' ");
            if (prop.IsRequiredOnEdit)
                // KABU TODO: LOCALIZE
                o($" data-val-required=\"Das Feld '{GetDisplayNameFor(context)}' ist erforderlich.\"");
            oO(" />");
        }

        public bool OPropTagsSelector(WebViewGenContext context)
        {
            var type = context.View.TypeConfig;
            var info = context.PropInfo;
            var vprop = info.ViewProp;
            var prop = info.Prop;
            var propPath = info.PropPath;

            if (!vprop.IsTagsSelector) return false;

            var targetType = vprop.Reference.ToType;
            var dialog = vprop.LookupDialog;

            OB("<div class='input-group'>");

            //OB("<div class='kmodo-tags-container'>");

            O($"<div class='kmodo-tags-listview' data-role='listview' data-bind='source: {propPath}' data-template='tag-template'/>");

            // Invisible input for binding & validation.
            OSelectorControlInvisibleInput(context);

            // Button for popping up the lookup dialog.            
            OSelectorControlButton(context);

            //OE("</div>"); // container

            OE("</div>"); // input-group

            OScriptBegin();
            O($"// Lookup view for {propPath}");
            OnSelectorButtonClick(context, () =>
            {
                O($"var $container = {JQuerySelectEditorContainer()};");
                O($"var item = $container.find('input').first().prop('kendoBindingTarget').source;");
                O($"var args = new casimodo.ui.DialogArgs('{dialog.Id}');");
                O($"casimodo.ui.dialogArgs.add(args);");

                // Fetch the partial view from server into a Kendo modal window.
                Oo($"var wnd = $('<div/>').appendTo($container).kendoWindow(");
                KendoGen.OWindowOptions(new KendoWindowConfig(dialog)
                {
                    IsModal = true,
                    OnClosing = new Action(() =>
                    {
                        // Closing event handler
                        oB($"function (e)");
                        OB("if (args.dialogResult === true)");
                        // Set value and fire the "change" event for the binding to pick up the new value.
                        O($"kendomodo.addEntityToObservableArray(item.{propPath}, args.item, '{targetType.Key.Name}');");
                        End();
                        End();
                    })
                });
                oO(").data('kendoWindow');"); // Kendo window

                O("kendomodo.setModalWindowBehavior(wnd);");

                O("wnd.center().open();");

                O($"wnd.refresh({{ url: '{dialog.Url}', cache: {MojenUtils.ToJsValue(dialog.IsCachedOnClient)} }});");

            });
            OScriptEnd();

            // Tag item template
            OKendoTemplateBegin("tag-template");
            OB("<div class='kmodo-tag-item'>");
            var firstProp = vprop.ContentView?.Props.FirstOrDefault();
            if (firstProp != null)
            {
                O($"<span>#:{firstProp.FormedNavigationTo.TargetProp.Name}#</span>");
            }
            OB("<a class='k-delete-button' href='\\\\#'>");
            O("<i class='remove glyphicon glyphicon-remove-sign'></i>");
            OE("</a>");
            OE("</div>");
            OKendoTemplateEnd();

            return true;
        }

        /*
         <div style="display: flex">
                        <div data-role="listview" data-bind="source: Tags" data-template="tag-template" style="flex: 2; display: flex; flex-flow: row wrap" />
                        <button class="k-button k-add-button">+</button>
                    </div>
                    <script type="text/x-kendo-template" id="tag-template">
                        <div style="display:flex; border: 1px solid gray; border-radius: 4px; margin: 3px">
                            <div>#:DisplayName#</div>
                            <a class="k-delete-button" href="\\#">
                                <i class="remove glyphicon glyphicon-remove-sign glyphicon-white"></i>
                            </a>
                        </div>
                    </script>
         */

        public bool OPropSequenceSelector(WebViewGenContext context)
        {
            var type = context.View.TypeConfig;
            var info = context.PropInfo;
            var vprop = info.ViewProp;
            var prop = info.Prop;
            var propPath = info.PropPath;

            // Sequence value and selector.
            if (!vprop.IsSelector) return false;

            var sprop = vprop.StoreOrSelf;

            if (!sprop.DbAnno.Sequence.Is &&
                !sprop.DbAnno.Sequence.IsDbSequence)
                return false;

            if (vprop.FormedNavigationTo.Is)
                throw new MojenException("Sequence value selectors do not support property navigation.");

            // Input group with sequence value (read-only) and a button.
            OB("<div class='input-group'>");

            // Readonly value display.
            //ElemClass("form-control");
            CustomElemStyle(context);
            OSelectorControlTextDisplay(context);

            // Invisible input for binding & validation.
            OSelectorControlInvisibleInput(context);

            // Button for executing the sequence generator.            
            ElemDataBindAttr($"enabled: is{propPath}SelectorEnabled");
            OSelectorControlButton(context);

            OE("</div>"); // input-group

            // Validation message
            O($"@Html.ValidationMessageFor(m => m.{propPath})");

            OScriptBegin();
            O($"// Sequence value generator for {propPath}");
            O($"var inputs = $(\"input[name='{propPath}']\");");
            OnSelectorButtonClick(context, () =>
            {
                O($"var item = inputs.first().prop('kendoBindingTarget').source;");
                O($"var args = [];");
                foreach (var per in sprop.DbAnno.Unique.GetParams())
                {
                    O($"args.push({{ name: '{per.Prop.Name}', value: item.{per.Prop.Name} }});");
                }
                var odata = App.Get<WebODataBuildConfig>();

                KendoGen.ODataFunction(
                    path: this.GetODataPath(type),
                    func: this.GetODataFunc(sprop.GetNextSequenceValueMethodName()),
                    args: "args",
                    then: () =>
                    {
                        // Set the acquired value & fire the property-changed event.
                        O($"inputs.val(value).change();");
                    });

            });
            OScriptEnd();

            return true;
        }

        // Lookup selector ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        string JQuerySelectEditorContainer()
        {
            return $"$(this).closest('.component-root')";
        }

        string JQuerySelectDialogContainer(string container)
        {
            if (container != null)
                return $"{container}.children('.modal-dialog-container').first()";
            return JQuerySelectEditorContainer();
        }

        // KABU TODO: MAGIC
        class GeoPlaceLookupWebViewConfig
        {
            public Guid Id { get; set; } = new Guid("c2383283-cb48-4ece-9066-667f5c623a95");
            public string Url { get; set; } = "/GoogleMap/Lookup";
            public string Title { get; set; } = "Adresse suchen";
            public int Width { get; set; } = 1000;
            public int Height { get; set; } = 700;
        }

        public bool OPropGeoPlaceLookupSelectorDialog(WebViewGenContext context)
        {
            var type = context.View.TypeConfig;
            var info = context.PropInfo;
            var vprop = info.ViewProp;
            var prop = info.Prop;
            var propPath = info.PropPath;

            if (!vprop.IsSelector) return false;
            if (!vprop.GeoPlaceLookup.Is) return false;

            var geoConfig = vprop.GeoPlaceLookup;

            // Input group
            OB("<div class='input-group'>");

            ElemClass("with-selector");
            OPropEditableCore(context);

            // Button for popping up the lookup dialog.
            OSelectorControlButton(context);

            OE("</div>"); // Input group

            OScriptBegin();
            O("// Geo place lookup dialog.");

            var dialog = new GeoPlaceLookupWebViewConfig();

            OnSelectorButtonClick(context, () =>
            {
                O($"var $container = {JQuerySelectEditorContainer()};");
                O($"var model = $container.find(\"input[name = '{propPath}']\").first().prop('kendoBindingTarget').source;");
                O($"var info = new kendomodo.GeoPlaceEditorInfo(model);");
                if (geoConfig.SourcePropMap != null)
                {
                    OB("info.map(");
                    foreach (var map in geoConfig.SourcePropMap.GetMappings())
                    {
                        O($"{map.Item1}: '{map.Item2}',");

                    }
                    End(");");
                }
                O($"var args = new casimodo.ui.DialogArgs('{dialog.Id}', info.PlaceInfo);");
                O($"casimodo.ui.dialogArgs.add(args);");

                var cachedWindow = geoConfig.IsViewCached
                    ? $"casimodo.run.{context.ComponentViewSpaceName}cachedDialogFor{propPath.Replace(".", "")}"
                    : "null";

                // Fetch the partial view from server into a Kendo modal window.
                Oo($"var wnd = {cachedWindow} || $('<div/>').appendTo($container).kendoWindow(");
                KendoGen.OWindowOptions(new KendoWindowConfig
                {
                    Title = dialog.Title,
                    Width = dialog.Width,
                    Height = dialog.Height,
                    IsModal = true,
                    OnDeactivated = geoConfig.IsViewCached ? null : KendoWindowConfig.DestructorFunction
                });
                oO(")"); // Kendo window
                O(".data('kendoWindow');");

                O("kendomodo.setModalWindowBehavior(wnd);");

                // Closing event handler
                OB("wnd.one('close', function(e)");
                OB("if (args.dialogResult === true)");
                // Set the address fields to the selected values.
                O("info.applyChanges();");
                End();
                End(");");

                if (geoConfig.IsViewCached)
                {
                    OB($"if ({cachedWindow} !== wnd)");
                    O($"{cachedWindow} = wnd;");
                    O($"wnd.refresh({{ url: '{dialog.Url}', cache: true }});");
                    End();
                }
                else
                {
                    O($"wnd.refresh({{ url: '{dialog.Url}', cache: false }});");
                }

                O("wnd.center().open();");
            });

            OScriptEnd();

            return true;
        }

        public bool OPropLookupSelectorDialog(WebViewGenContext context)
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
            var dialog = vprop.LookupDialog;

            // Input group
            OB("<div class='input-group'>");

            // Invisible input for binding & validation.
            OSelectorControlInvisibleInput(context);

            // Button for popping up the lookup dialog.
            OSelectorControlButton(context);

            OE("</div>"); // Input group

            OScriptBegin();
            O($"// Lookup dialog for {propPath}");

            ComponentCascadeFromInfo cascadeFrom = vprop.CascadeFrom != null ? ComputeCascadeFrom(info) : null;
            if (cascadeFrom != null)
                O($"// Cascading from {cascadeFrom.ForeignKey.Name}");

            OnSelectorButtonClick(context, () =>
            {
                O($"var inputs = $(\"input[name='{propPath}']\");");
                O($"var args = new casimodo.ui.DialogArgs('{dialog.Id}', inputs.first().val());");
                O($"var $container = {JQuerySelectEditorContainer()};");

                // Arguments to be passed to the lookup dialog.
                if (cascadeFrom != null)
                {
                    // There must be a reference in the lookup target type which references the same type.
                    var cascadeType = cascadeFrom.ForeignKey.Reference.ToType;
                    var reference = info.TargetType.FindReferenceWithForeignKey(to: cascadeType);
                    if (reference == null)
                        throw new MojenException("Lookup with cascade-from mismatch: " +
                            $"There is no reference to type '{cascadeType.ClassName}' in type '{info.TargetType.ClassName}' to be used for cascade-from.");

                    O($"var cascadeFromVal = inputs.first().prop('kendoBindingTarget').source.{cascadeFrom.ForeignKey.Name};");

                    // Notify & exit if the cascade-from field has not been assigned yet.                    
                    OB("if (!cascadeFromVal)");
                    // Notify
                    // KABU TODO: LOCALIZE
                    O($"kendomodo.showModalTextDialog($container, 'info', \"" +
                        $"Zuerst muss '{cascadeFrom.ForeignKey.DisplayLabel}' gesetzt werden, " +
                        $"bevor '{info.EffectiveDisplayLabel}' ausgewählt werden kann.\");");
                    // Exit
                    O("return;");
                    End();

                    // Filter by the cascade-from field & value.
                    O($"args.filters = [{{ field: '{reference.ForeignKey.Name}', value: cascadeFromVal, operator: 'eq' }}];");
                }

                if (vprop.CascadeFromScope.Is)
                {
                    // Filter using a property in the view model scope.
                    // Get the view model.
                    O($"var vm = $container.data('viewModel');");

                    // Get target info
                    var targetType = vprop.FormedNavigationTo.TargetType;
                    var targetProp = vprop.CascadeFromScope.TargetProp;
                    var targetPath = targetProp.FormedNavigationFrom.GetTargetPathFrom(targetType);

                    // Get source info
                    var sourceNavi = vprop.CascadeFromScope.SourceProp.FormedNavigationFrom;
                    var sourceProp = sourceNavi.TargetProp;
                    var sourceType = sourceNavi.Root.SourceType;
                    // NOTE: We are expecting the source type's name to be used as variable name.
                    var sourcePath = $"scopeVars.{sourceType.Name}.{sourceNavi.TargetPath}";

                    // Check value at path
                    O($"cascadeFromVal = casimodo.getValueAtPropPath(vm, '{sourcePath}');");
                    OB("if (!cascadeFromVal)");
                    // Notify
                    // KABU TODO: LOCALIZE
                    O($"kendomodo.showModalTextDialog($container, 'info', \"" +
                        $"Zuerst muss '{sourceProp.DisplayLabel}' gesetzt werden, " +
                        $"bevor '{info.EffectiveDisplayLabel}' ausgewählt werden kann.\");");
                    // Exit
                    O("return;");
                    End();


                    O($"var filter = {{ field: '{targetPath}', value: cascadeFromVal, operator: 'eq' }};");
                    O($"args.filters.push(filter);");
                }

                O($"casimodo.ui.dialogArgs.add(args);");

                // Fetch the partial view from server into a Kendo modal window.
                Oo($"var wnd = $('<div/>').appendTo($container).kendoWindow(");
                KendoGen.OWindowOptions(new KendoWindowConfig(dialog)
                {
                    IsModal = true,
                    OnClosing = new Action(() =>
                    {
                        // Closing event handler
                        oB($"function (e)");
                        OB("if (args.dialogResult === true)");
                        // Set value and fire the "change" event for the binding to pick up the new value.
                        O($"inputs.val(args.value).change();");
                        End();
                        End();
                    })
                });
                oO(").data('kendoWindow');"); // Kendo window

                O("kendomodo.setModalWindowBehavior(wnd);");

                O("wnd.center().open();");

                O($"wnd.refresh({{ url: '{dialog.Url}', cache: {MojenUtils.ToJsValue(dialog.IsCachedOnClient)} }});");

            });
            OScriptEnd();

            return true;
        }

        // Drop down selector ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public bool OPropDropDownSelector(WebViewGenContext context)
        {
            var type = context.View.TypeConfig;
            var info = context.PropInfo;
            var vprop = info.ViewProp;
            var prop = info.Prop;
            var propPath = info.PropPath;

            // The property to be used for display of the referenced value.
            var targetDisplayProp = info.TargetDisplayProp;

            var c = info.TargetDisplayProp;

            if (!vprop.IsSelector) return false;

            if (vprop.Lookup.Is) return false;

            // DropDown list            

            bool cascade = vprop.CascadeFrom != null;
            string cascadeParentForeignKeyName = null;
            string cascadeParentComponentId = null;
            string cascadeQueryParameterFunc = null;

            var targetType = prop.Reference.ToType;
            string key = "Value";
            string display = "Text";

            // NOTE: We now perform client queries data sets greater than extra small.
            bool clientQuery = cascade || targetType.DataSetSize != MojDataSetSizeKind.ExtraSmall;
            string clientQueryUrl = null;

            O($"<!-- Drop down selector for {propPath} -->");

            if (clientQuery)
            {
                // Key property to be selected from the target objects.
                key = prop.Reference.ToTypeKey.Name;

                var sortProps = targetType.GetODataOrderBy();

                // Display property to be selected from the target objects.
                if (targetDisplayProp != null)
                    display = targetDisplayProp.Name;
                else
                {
                    var pick = targetType.FindPick();
                    if (pick == null)
                        throw new MojenException($"No pick display property defined for reference property '{prop.Name}'.");

                    display = pick.DisplayProp;
                }

                sortProps = sortProps != null ? sortProps : display;

                // Build OData URL
                clientQueryUrl = $"{this.GetODataQueryFunc(prop.Reference.ToType)}()?$select={key},{display}&$orderby={sortProps}";
            }

            if (cascade)
            {
                throw new NotImplementedException("Cascade-from not implmemented yet.");

#pragma warning disable CS0162 // Unreachable code detected
                // Compute cascade information.
                var cascadeFrom = ComputeCascadeFrom(info);
                cascadeParentForeignKeyName = cascadeFrom.ForeignKey.Name;
                cascadeParentComponentId = cascadeParentForeignKeyName;

                O($"// Cascading from {cascadeParentForeignKeyName}");
#pragma warning restore CS0162
            }

            if (cascade && cascadeQueryParameterFunc != null)
            {
                // JS function that will query the lookup values based on the
                //   currently selected cascade-from source value.

                OB("<script>");
                O($"function {cascadeQueryParameterFunc}() {{");
                O("    return {");
                O($"     '$select': '{key},{display}'");
                O("    }");
                O("}");
                OE("</script>");
            }

            // DropDown list
            // See http://demos.telerik.com/aspnet-mvc/dropdownlist/index
            O($"@(Html.Kendo().DropDownList().Name(\"{propPath}\")");
            Push();

            O($".ValuePrimitive(true)");

            O($".DataValueField(\"{key}\").DataTextField(\"{display}\")");

            // KABU TODO: IMPORTANT: REMOVE? What's the point of having no NULL value in the UI?
            //if (!prop.IsRequiredOnEdit)
            O(".OptionLabel(\" \")");

            if (cascade)
            {
                O(".AutoBind(false)");
                O(".Enable(false)");
                O($".CascadeFrom(\"{cascadeParentComponentId}\")");
                O($".CascadeFromField(\"{cascadeParentForeignKeyName}\")");
            }

            if (clientQuery)
            {
                // Client data binding.
                // Generate Kendo data-source.
                KendoGen.OMvcReadOnlyDataSource(clientQueryUrl, cascadeQueryParameterFunc);
            }
            else
            {
                // KABU TODO: REMOVE? Not used anymore.

                // Server side data binding.
                // Example:
                // .BindTo(PickItemsContainer.GetCompanies("Name", nullable: true))
                var repository = targetType.PluralName;
                Oo($".BindTo(PickItemsContainer.Get{repository}(");

                // If applicable, display the specified target property.
                if (targetDisplayProp != null) o($"\"{targetDisplayProp.Name}\", ");

                // Nullable
                o($"nullable: {MojenUtils.ToCsValue(!prop.IsRequiredOnEdit)})");

                oO(")");
            }

            if (prop.IsRequiredOnEdit)
                ElemFlag("required");

            // AddClassAttr("kendo-force-validation");
            ElemClass("form-control");
            OMvcAttrs(context, kendo: true);

            OToClientTemplate();

            Pop();
            oO(")"); // DropDownList

            // Validation message
            O($"@Html.ValidationMessageFor(m => m.{propPath})");

            return true;
        }

        // Snippet selector ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public bool OPropSnippetSelector(WebViewGenContext context)
        {
            var type = context.View.TypeConfig;
            var info = context.PropInfo;
            var vprop = info.ViewProp;
            var prop = info.Prop;
            var propPath = info.PropPath;

            if (!vprop.Snippets.Is) return false;

            var dialog = App.Get<MojSnippetsEditorConfig>().View;

            // Input group with text editor and button.
            OB("<div class='input-group'>");

            // Text editor            
            ElemClass("form-control");
            ElemClass("with-selector");
            CustomElemStyle(context);
            OStringEditor(context);

            // Button for popping up the lookup dialog.            
            OSelectorControlButton(context);

            OE("</div>"); // input-group

            OScriptBegin();
            O($"// Snippet editor for {propPath}");
            OnSelectorButtonClick(context, () =>
            {
                O($"var inputs = $(\"textarea[name='{propPath}'], input[name='{propPath}']\");");

                O($"var args = new casimodo.ui.DialogArgs('{dialog.Id}', inputs.first().val());");
                O($"args.mode = '{vprop.Type.MultilineString.Mode}';");
                O("casimodo.ui.dialogArgs.add(args);");

                // Compute URL with parameters.
                var url = dialog.Url + "/?";
                int i = 0;
                foreach (var p in dialog.Lookup.Parameters)
                {
                    if (i++ > 0) url += "&";
                    url += $"{p.VName}={vprop.Snippets.Args[p.Name]}";
                }

                // Fetch the partial view from server into a Kendo modal window.
                Oo($"var wnd = $('<div/>').appendTo({JQuerySelectDialogContainer(null)}).kendoWindow(");
                KendoGen.OWindowOptions(new KendoWindowConfig(dialog)
                {
                    IsParentModal = context.View.IsModal,
                    IsModal = true,
                    OnClosing = new Action(() =>
                    {
                        // Closing event handler
                        oB($"function (e)");
                        OB("if (args.dialogResult === true)");
                        // Set value and fire the change event for the binding to pick up the new value.
                        O($"inputs.val(args.value).change();");
                        End();
                        End();
                    })
                });
                oO(").data('kendoWindow');"); // Kendo window

                O("kendomodo.setModalWindowBehavior(wnd);");

                O("wnd.center().open();");

                O($"wnd.refresh({{ url: '{url}', cache: {MojenUtils.ToJsValue(dialog.IsCachedOnClient)} }});");

            });
            OScriptEnd();

            return true;
        }

        void OnSelectorButtonClick(WebViewGenContext context, Action action)
        {
            var propPath = context.PropInfo.PropPath;

            // On button click...
            OB($"$('#editor-btn-for-{propPath}').click(function (e)");

            action();

            End(");"); // Click handler
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void OMvcAttrs(WebViewGenContext context, bool kendo)
        {
            var info = context.PropInfo;
            var prop = info.Prop;

            if (!kendo)
                ElemDataBindAttr(context);

            OMvcAttrs(kendo);
        }
    }
}