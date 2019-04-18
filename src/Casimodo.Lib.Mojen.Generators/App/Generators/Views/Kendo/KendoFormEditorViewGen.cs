using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoFormEditorViewGen : KendoTypeViewGenBase
    {
        public KendoFormReadOnlyViewGen ReadOnlyGen { get; set; } = new KendoFormReadOnlyViewGen();

        public List<MojViewPropInfo> UsedViewPropInfos { get; set; } = new List<MojViewPropInfo>();

        public string ViewFilePath { get; set; }
        public string ViewFileName { get; set; }

        public string ScriptFilePath { get; set; }

        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>()
                .Where(x => x.Uses(this)))
            {
                UsedViewPropInfos.Clear();

                ScriptFilePath = BuildTsScriptFilePath(view, suffix: ".vm.generated");

                var context = KendoGen.InitComponentNames(new WebViewGenContext
                {
                    View = view,
                    IsEditableView = true,
                    ViewRole = "editor",
                    IsViewIdEnabled = true
                });

                if (view.IsCustom)
                    throw new Exception("'Custom' is not supported for editor views. Use 'Viewless' instead.");

                if (!view.IsCustomView)
                {
                    PerformWrite(view, () => GenerateView(context));
                }

                PerformWrite(ScriptFilePath, () =>
                {
                    KendoGen.OEditorViewModel(context);
                });

                var dataViewModelGen = new WebDataEditViewModelGen();
                dataViewModelGen.Initialize(App);

                dataViewModelGen.PerformWrite(Path.Combine(GetViewDirPath(view), BuildEditorDataModelFileName(view)), () =>
                {
                    dataViewModelGen.GenerateEditViewModel(view.TypeConfig, UsedViewPropInfos, view.Group,
                        isDateTimeOffsetSupported: false);
                });

                RegisterComponent(context);
            }
        }

        public MojHttpRequestConfig TransportConfig { get; set; }

        public override void AfterView(WebViewGenContext context)
        {
            // NOP
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

            OLabelContainerBegin = (c) =>
            {
                // if (c.IsRunEditable)
                var style = c.Cur.GetGroupLabelStyle();
                XB($"<div class='{style ?? LabelContainerClass}'>");
            };
            OLabelContainerEnd = (c) =>
            {
                // if (c.IsRunEditable)
                XE("</div>");
            };

            OPropContainerBegin = (c) =>
            {
                XB($"<div class='{c.Cur.GetGroupPropStyle() ?? PropContainerClass}'>");
                XB($"<div class='km-input-group-container'>");
            };
            OPropContainerEnd = (c) => { XE("</div>"); XE("</div>"); };
        }

        bool IsEffectiveStandaloneView(WebViewGenContext context)
        {
            return context.View.Standalone.Is;
        }

        public override void BeginView(WebViewGenContext context)
        {
            ORazorGeneratedFileComment();

            var type = context.View.TypeConfig;
            ORazorUsing(type.Namespace, "Casimodo.Lib.Web");

            ORazorModel($"{context.View.Group ?? ""}{type.Name}Model");

            CheckViewId(context.View);

            // NOTE: ignore min-width because this could make the view
            //   too wide for bootstrap's min screen width layout.
            //   responsive layout 
            var style = GetStyleAttr(GetViewStyles(context, (name) => name != "min-width"));

            if (IsEffectiveStandaloneView(context))
            {
                // NOTE: "k-edit-form-container" is a Kendo class.
                XB("<div class='k-edit-form-container'{0}>", GetViewHtmlId(context));
                XB($"<div class='form-horizontal component-root'{style}>");
            }
            else
            {
                XB($"<div class='form-horizontal component-root'{style}{GetViewHtmlId(context)}>");
            }

            // Validation error box.
            O("<ul class='km-form-validation-summary' style='display:none'></ul>");

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
            XE("</div>");
            if (IsEffectiveStandaloneView(context))
            {
                XB("<div class='k-edit-buttons k-state-default'>");
                O("<a class='k-button k-button-icontext k-primary k-update k-state-disabled' href='#'><span class='k-icon k-update'></span>Speichern</a>");
                O("<a class='k-button k-button-icontext k-cancel' href='#'><span class='k-icon k-cancel'></span>Abbrechen</a>");
                XE("</div>");

                XE("</div>");
            }
        }

        public override bool ORunBegin(WebViewGenContext context)
        {
            if (!base.ORunBegin(context))
                return false;

            // Form group
            XB($"<div class='{GetFormGroupClass(context)}'>");

            return true;
        }

        public override bool ORunEnd(WebViewGenContext context)
        {
            if (!base.ORunEnd(context))
                return false;

            XE("</div>");

            return true;
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
                @class += " " + hideProp.GetRemoveOnMarkerClasses();
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
                    // Labels of check-boxes are display right hand side of the check-box.
                    return;

                // ElemClass("km-readonly-prop-label", target: "label");
                base.OPropLabel(context);
            }
            else
            {
                // Read-only property label.
                // ReadOnlyGen.ElemClass("km-readonly-prop-label", target: "label");
                ReadOnlyGen.OPropLabel(context);
            }
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
            var vprop = info.ViewProp;

            UsedViewPropInfos.Add(info);

            if (info.ViewProp.IsEditable)
            {
                // Editor

                if (vprop.Width != null)
                    ElemStyle($"width:{vprop.Width}px !important");
                if (vprop.MaxWidth != null)
                    ElemStyle($"max-width:{vprop.MaxWidth}px !important");

                OPropEditable(context);
            }
            else
            {
                // Read-only property.
                ReadOnlyGen.ElemClass("km-readonly-form-control");
                ReadOnlyGen.OProp(context);
            }
        }

        public class ComponentCascadeFromInfo
        {
            public MojCascadeFromConfig Config { get; set; }
            public MojFormedNavigationPathStep FirstLooseStep { get; set; }
            public MojProp ForeignKey { get; set; }
        }

        List<ComponentCascadeFromInfo> BuildCascadeFromInfos(MojViewPropInfo info)
        {
            if (!info.ViewProp.CascadeFrom.Is)
                return null;

            var items = new List<ComponentCascadeFromInfo>();

            foreach (var cascade in info.ViewProp.CascadeFrom.Items)
            {
                var item = new ComponentCascadeFromInfo();

                item.Config = cascade;

                item.FirstLooseStep = cascade.FromType.FormedNavigationFrom.FirstLooseStep;
                if (item.FirstLooseStep == null)
                    throw new MojenException("The cascade-from path must contain a loose reference property.");

                item.ForeignKey = item.FirstLooseStep.SourceProp.Reference.ForeignKey;

                items.Add(item);
            }

            return items;
        }

        public void OPropEditable(WebViewGenContext context)
        {

            if (OPropSelector(context))
                return;

            OPropEditableCore(context);
        }

        public void oAttr(string name, object value)
        {
            o($" {name}='{Moj.ToJsXAttrValue(value)}'");
        }

        public void OPropEditableCore(WebViewGenContext context)
        {
            var type = context.View.TypeConfig;
            var vinfo = context.PropInfo;
            var vprop = vinfo.ViewProp;
            var ppath = vinfo.PropPath;
            var dprop = vinfo.TargetDisplayProp;
            var vpropType = vinfo.TargetDisplayProp.Type;

            // MVC jQuery validation : see https://www.blinkingcaret.com/2016/03/23/manually-use-mvc-client-side-validation/
            bool validationBox = true;

            CustomElemStyle(context);

            // Add "form-control" class.
            // Except for Kendo's numeric boxes, which just break if using bootstrap's form-control class.
            if (!vpropType.IsNumber)
            {
                ElemClass("form-control");
            }

            if (vprop.IsAutocomplete == false)
                ElemAttr("autocomplete", "false");

            // Enable MVC's unobtrusive jQuery validation.
            ElemAttr("data-val", true);

            if (vprop.CustomEditorViewName != null)
            {
                OMvcPartialView(vprop.CustomEditorViewName);
            }
            // NOTE: Enums are also numbers here, so ensure the enum handler comes first.
            else if (vpropType.IsEnum)
            {
                throw new MojenException("Enums are not supported.");
            }
            else if (vpropType.IsNumber)
            {
                OKendoNumericInput(context);
                validationBox = false;
            }
            // Uploadable image property
            else if (dprop.FileRef.Is && dprop.FileRef.IsImage &&
#pragma warning disable CS0618 // Type or member is deprecated
                dprop.FileRef.IsUploadable)
#pragma warning restore CS0618 // Type or member is deprecated
            {
                // NOTE: File upload is not supported.
                OFileUpload(context);
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
                OKendoColorPicker(context);
            }
            else if (vpropType.IsAnyTime)
            {
                OKendoDateTimePicker(context);
            }
            else if (vpropType.IsTimeSpan)
            {
                OKendoTimeSpanEditor(context);
            }
            else if (vpropType.IsString)
            {
                OKendoTextInput(context);
            }
            else if (vpropType.IsBoolean)
            {
                OKendoCheckbox(context);
            }
            else
            {
                // Fallback to generic editor.
                Oo($"@(Html.EditorFor(m => m.{ppath}");
                OMvcAttrs(context, kendo: false);
                oO("))");
            }

            // Validation message
            if (validationBox)
            {
                OInvalidPropPlaceholder(context);
                // TODO: REMOVE: OValidationMessageElem(ppath);
            }
        }

        // Selectors ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public bool OPropSelector(WebViewGenContext context)
        {
            return
                OPropTagsSelector(context) ||
                OPropSnippetsEditor(context) ||
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

            // Start new control action.
            context.CurControlAction.ControlIndex++;
            context.CurControlAction.CurrentId =
                $"view-action-{context.View.Id}-{context.CurControlAction.ControlIndex.ToString().PadLeft(2, '0')}-for-{propPath}";

            XB("<span class='input-group-btn'>");
            XB($"<button class='btn btn-default km-selector-btn' id='{context.CurControlAction.CurrentId}'{GetElemAttrs()}>");
            //if (!string.IsNullOrEmpty(text)) O($"<span>{text}</span>");
            O("<span class='glyphicon glyphicon-search'></span>");
            XE("</button>");
            XE("</span>");
        }

        void OSelectorControlInvisibleInput(WebViewGenContext context)
        {
            var vprop = context.PropInfo.ViewProp;
            var prop = context.PropInfo.Prop;
            var propPath = context.PropInfo.PropPath;

            // Invisible input field for property binding and validation,            
            Oo($"<input id='{propPath}' name='{propPath}' data-bind='value:{propPath}' class='k-input k-valid'");
            o(" type ='text' style='display: none;' aria-disabled='false' aria-readonly='false' ");

            if (prop.IsRequiredOnEdit)
                // KABU TODO: How to avoid having to specify the whole sentence here?
                //   We want to specify the display-name only.
                // KABU TODO: LOCALIZE
                o($" data-val-required=\"'{GetDisplayNameFor(context)}' ist erforderlich.\"");

            oO(" />");
        }

        void XBInputGroup()
        {
            XB("<div class='input-group' style='flex-grow:1'>");
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

            XBInputGroup();

            //XB("<div class='kmodo-tags-container'>");

            O($"<div class='kmodo-tags-listview' data-role='listview' data-bind='source: {propPath}' data-template='tag-template'/>");

            // Invisible input for binding & validation.
            OSelectorControlInvisibleInput(context);

            // Button for popping up the lookup dialog.            
            OSelectorControlButton(context);

            //OE("</div>"); // container

            XE("</div>"); // input-group

            OMvcScriptBegin();
            O($"// Lookup view for {propPath}");
            OnSelectorButtonClick(context, () =>
            {
                O($"const $container = {JQuerySelectEditorContainer()};");
                O($"const item = $container.find('input').first().prop('kendoBindingTarget').source;");

                throw new MojenException("modo.addEntityToObservableArray does not exist yet.");
#pragma warning disable
                KendoGen.OOpenDialogView(context, dialog,
                    // Set value and fire the "change" event for the binding to pick up the new value.
                    ok: () => O($"kmodo.addEntityToObservableArray(item.{propPath}, result.item, '{targetType.Key.Name}');"));
#pragma warning restore
            });
            OMvcScriptEnd();

            // Tag item template
            OKendoTemplateBegin("tag-template");
            XB("<div class='kmodo-tag-item'>");
            var firstProp = vprop.ContentView?.Props.FirstOrDefault();
            if (firstProp != null)
            {
                O($"<span>#:{firstProp.FormedNavigationTo.TargetProp.Name}#</span>");
            }
            XB("<a class='k-delete-button' href='\\\\#'>");
            O("<i class='remove glyphicon glyphicon-remove-sign'></i>");
            XE("</a>");
            XE("</div>");
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
            XBInputGroup();

            // Readonly value display.
            //ElemClass("form-control");
            CustomElemStyle(context);
            OSelectorControlTextDisplay(context);

            // Invisible input for binding & validation.
            OSelectorControlInvisibleInput(context);

            // Button for executing the sequence generator.            
            ElemDataBindAttr($"enabled: is{propPath}SelectorEnabled");
            OSelectorControlButton(context);

            XE("</div>"); // input-group

            // Validation message
            O($"@Html.ValidationMessageFor(m => m.{propPath})");

            OMvcScriptBegin();
            O($"// Sequence value generator for {propPath}");
            O($"const inputs = $(\"input[name='{propPath}']\");");
            OnSelectorButtonClick(context, () =>
            {
                O($"const item = inputs.first().prop('kendoBindingTarget').source;");
                O($"const args = [];");
                foreach (var per in sprop.DbAnno.Unique.GetParams())
                {
                    O($"args.push({{ name: '{per.Prop.Name}', value: item.{per.Prop.Name} }});");
                }
                var odata = App.Get<WebODataBuildConfig>();

                KendoGen.ODataFunction(
                    path: this.GetODataPath(type),
                    func: this.NamespaceQualifyODataFunc(sprop.GetNextSequenceValueMethodName()),
                    args: "args",
                    then: () =>
                    {
                        // Set the acquired value & fire the property-changed event.
                        O($"inputs.val(value).change();");
                    });

            });
            OMvcScriptEnd();

            return true;
        }

        // Lookup selector ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        string JQuerySelectEditorContainer()
        {
            return $"$(this).closest('.component-root')";
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
            XBInputGroup();

            ElemClass("km-with-selector");
            OPropEditableCore(context);

            // Button for popping up the lookup dialog.
            OSelectorControlButton(context);

            XE("</div>"); // Input group

            OMvcScriptBegin();
            O("// Geo place lookup dialog.");

            OnSelectorButtonClick(context, () =>
            {
                O($"const $container = {JQuerySelectEditorContainer()};");
                O($"const model = $container.find(\"input[name = '{propPath}']\").first().prop('kendoBindingTarget').source;");
                O($"const info = new kmodo.GeoPlaceEditorInfo(model);");
                if (geoConfig.SourcePropMap != null)
                {
                    OB("info.map(");
                    foreach (var map in geoConfig.SourcePropMap.GetMappings())
                    {
                        O($"{map.Item1}: '{map.Item2}',");

                    }
                    End(");");
                }

                KendoGen.OOpenGeoPlaceLookupView(context,
                    // Set value and fire the "change" event for the binding to pick up the new value.
                    ok: () => O("info.applyChanges();"),
                    options: (Action)(() =>
                    {
                        //O("cache: {0},", MojenUtils.ToJsValue(geoConfig.IsViewCached));
                        O("title: 'Adresse suchen',");
                        O("maximize: true,");
                        O("item: info.PlaceInfo");
                    }));
            });

            OMvcScriptEnd();

            return true;
        }

        public void OLookupSelectorReadOnlyText(WebViewGenContext context)
        {
            var vprop = context.PropInfo.ViewProp;
            CustomElemStyle(context);
            // TODO: We need Bootstrap 4 to use class "text-truncate".
            //   Currently I have to add that CSS selector explicitely in my apps.
            ElemClass("form-control km-lookup-display-form-control");
            var binding = GetBinding(vprop);
            O($@"<div data-bind='text:{binding},attr:{{title:{binding}}}'{GetElemAttrs()}></div>");
        }

        public void OInvalidPropPlaceholder(WebViewGenContext context)
        {
            OInvalidPropPlaceholder(context.PropInfo.PropPath);
        }

        public void OInvalidPropPlaceholder(string propPath)
        {
            O($"<span class='k-invalid-msg' data-for='{propPath}'></span>");
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
            XBInputGroup();

            // TODO: REVISIT: Add a custom container.
            //   Couldn't make the outcome look nice without an additional custom flexbox container.
            // TODO: REMOVE? XB("<div class='km-input-control-container'>");

            OLookupSelectorReadOnlyText(context);

            // Invisible input for binding & validation.
            OSelectorControlInvisibleInput(context);

            // TODO: REMOVE? XE("</div>"); // custom km-input-control-container

            // Button for popping up the lookup dialog.
            OSelectorControlButton(context);

            XE("</div>"); // input-group

            // Place outside of input-group, otherwise the button won't become
            //   visually attached to the text box.
            // Note that this will still be inside the km-input-group-container.
            OInvalidPropPlaceholder(context);

            OMvcScriptBegin();
            O($"// Lookup dialog for {propPath}");

            var cascadeFromInfos = BuildCascadeFromInfos(info);
            if (cascadeFromInfos != null)
                O($"// Cascading from fields");

            OnSelectorButtonClick(context, () =>
            {
                O($"const $inputs = $(\"input[name='{propPath}']\");");
                O($"const $container = {JQuerySelectEditorContainer()};");
                O("const options = {};");

                // Arguments to be passed to the lookup dialog.
                if (cascadeFromInfos?.Any() == true)
                {
                    O($"options.filters = [];");
                    O($"options.filterCommands = [];");
                    O("let cascadeFromVal = '';");
                    O();
                    foreach (var cascadeFromInfo in cascadeFromInfos)
                    {
                        O($"// Cascading from {cascadeFromInfo.ForeignKey.Name}");

                        // There must be a reference in the lookup target type which references the same type.
                        var cascadeType = cascadeFromInfo.ForeignKey.Reference.ToType;
                        var reference = info.TargetType.FindReferenceWithForeignKey(to: cascadeType);
                        if (reference == null)
                            throw new MojenException("Lookup with cascade-from mismatch: " +
                                $"There is no reference to type '{cascadeType.ClassName}' in type '{info.TargetType.ClassName}' to be used for cascade-from.");

                        O($"cascadeFromVal = $inputs.first().prop('kendoBindingTarget').source.{cascadeFromInfo.ForeignKey.Name};");

                        // Notify & exit if the cascade-from field has not been assigned yet.                    
                        OB("if (!cascadeFromVal)");
                        // Notify
                        // KABU TODO: LOCALIZE
                        var fromPropDisplayName = cascadeFromInfo.Config.FromPropDisplayName ??
                            cascadeFromInfo.ForeignKey.DisplayLabel;

                        O($"kmodo.openInstructionDialog(\"" +
                        $"Zuerst muss '{fromPropDisplayName}' gesetzt werden, " +
                        $"bevor '{info.EffectiveDisplayLabel}' ausgewählt werden kann.\");");
                        // Exit
                        O("return;");
                        End();

                        var targetType = reference.ForeignKey.Reference.ToType;
                        var isDeactivatable = cascadeFromInfo.Config.IsDeactivatable;
                        // Filter by the cascade-from field & value.
                        O($"options.filters.push({{ field: '{reference.ForeignKey.Name}', " +
                        "value: cascadeFromVal, operator: 'eq', " +
                        $"targetType: '{targetType.Name}', " +
                        $"targetTypeId: '{targetType.Id}', " +
                        $"deactivatable: {Moj.JS(isDeactivatable)} }});");

                        if (cascadeFromInfo.Config.IsDeactivatable)
                        {
                            O($"options.filterCommands.push({{ field: '{reference.ForeignKey.Name}', " +
                                $"value: cascadeFromVal, " +
                                $"deactivatable: {Moj.JS(isDeactivatable)}, " +
                                $"title: '{cascadeFromInfo.Config.Title}'}});");
                        }
                        O();
                    }
                }

                if (vprop.CascadeFromScope.Is)
                {
                    // Filter using a property in the view model scope.
                    // Get the view model.
                    O($"const vm = $container.data('viewModel');");

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
                    O($"cascadeFromVal = cmodo.getValueAtPropPath(vm, '{sourcePath}');");
                    OB("if (!cascadeFromVal)");
                    // Notify
                    // KABU TODO: LOCALIZE
                    O($"kmodo.openInstructionDialog(\"" +
                    $"Zuerst muss '{sourceProp.DisplayLabel}' gesetzt werden, " +
                    $"bevor '{info.EffectiveDisplayLabel}' ausgewählt werden kann.\");");
                    // Exit
                    O("return;");
                    End();

                    O($"const filter = {{ field: '{targetPath}', " +
                        "value: cascadeFromVal, operator: 'eq', " +
                        $"targetType: '{targetType}', targetTypeId: '{targetType.Id}', " +
                        $"deactivatable: {Moj.JS(false)} }};");

                    O($"options.filters.push(filter);");
                }

                KendoGen.OOpenDialogView(context, dialog,
                    // Set value and fire the "change" event for the binding to pick up the new value.
                    ok: () => O($"$inputs.val(result.value).change();"),
                    options: "options");
            });
            OMvcScriptEnd();

            return true;
        }

        // Drop down selector ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void OHtmlDataBindValue(WebViewGenContext context, string binding = null)
        {
            if (context.View.UseMVVM)
                o($@" data-bind='{binding ?? "value"}:{GetBinding(context)}");
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

            O($"<!-- Drop down selector for {propPath} -->");

            var targetType = prop.Reference.ToType;
            string key = "Value";
            string display = "Text";
            string odataQuery = null;

            // Cascade NOT-SUPPORTED
            bool cascade = vprop.CascadeFrom.Is;
            string cascadeParentForeignKeyName = null;
            string cascadeParentComponentId = null;
            string cascadeQueryParameterFunc = null;
            if (cascade)
            {
                // KABU TODO: Kendo's drop down list seems to cascade only form other drop down lists.
                // This is useless for us because we use custom lookup components which are not drop down lists.
                throw new NotImplementedException("Cascade-from not implmemented yet for drop down lists.");

#pragma warning disable CS0162 // Unreachable code detected
                // Compute cascade information.
                // var cascadeFrom = ComputeCascadeFromInfos(info);
                // cascadeParentForeignKeyName = cascadeFrom.ForeignKey.Name;
                // cascadeParentComponentId = cascadeParentForeignKeyName;

                O($"// Cascading from {cascadeParentForeignKeyName}");

                if (cascadeQueryParameterFunc != null)
                {
                    // JS function that will query the lookup values based on the
                    //   currently selected cascade-from source value.

                    XB("<script>");
                    O($"function {cascadeQueryParameterFunc}() {{");
                    O("    return {");
                    O($"     '$select': '{key},{display}'");
                    O("    }");
                    O("}");
                    XE("</script>");
                }
#pragma warning restore CS0162
            }

            // Key property to be selected from the target objects.
            key = prop.Reference.ToTypeKey.Name;

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

            // OData order by
            var odataOrderBy = targetType.GetODataOrderBy();
            odataOrderBy = odataOrderBy != null ? odataOrderBy : display;

            // Build OData query
            odataQuery = $"{this.GetODataQueryFunc(prop.Reference.ToType)}()?$select={key},{display}&$orderby={odataOrderBy}";

            // Drop down list component
            // See http://demos.telerik.com/aspnet-mvc/dropdownlist/index

            var displayName = GetDisplayNameFor(context);
            Oo($@"<input id='{propPath}' name='{propPath}' type='text'");
            o($@" class='form-control' data-display-name='{displayName}'");
            OHtmlDataBindValue(context);
            OHtmlRequiredttrs(context, prop);
            oElemAttrs();
            oO(" />");

            OInvalidPropPlaceholder(context);
            // TODO: REMOVE: OValidationMessageElem(propPath);

            OJsScript(() =>
            {
                var enable = true;
                var autoBind = true;
                if (cascade)
                {
                    enable = false;
                    autoBind = false;
                }
                var valuePrimitive = true;
                var optionLabel = " ";
                OJQueryOnDocReady(() =>
                {
                    OBegin($@"$('#{propPath}').kendoDropDownList(", () =>
                    {
                        // Options
                        Oo($@"autoBind: {Moj.JS(autoBind)},");
                        o($@"enable: {Moj.JS(enable)},");
                        o($@"valuePrimitive: { Moj.JS(valuePrimitive)},");
                        o($@"dataValueField: ""{key}"", dataTextField: ""{display}"",");
                        o($@"optionLabel: ""{optionLabel}"",");
                        if (cascade)
                        {
                            // ID of the parent drop down list.
                            o($@"cascadeFrom: ""{cascadeParentComponentId}"",");
                            o($@"cascadeFromField: ""{cascadeParentForeignKeyName}"",");
                        }
                        Br();
                        OBegin("dataSource:", () => KendoGen.OODataSourceReadOptions(context, odataQuery), ",");
                    }, ");");
                });
            });

            return true;
        }

        // Text snippets editor ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public bool OPropSnippetsEditor(WebViewGenContext context)
        {
            var type = context.View.TypeConfig;
            var info = context.PropInfo;
            var vprop = info.ViewProp;
            var dprop = info.TargetDisplayProp;
            var propPath = info.PropPath;

            if (!vprop.Snippets.Is) return false;

            var snippetsEditorView = App.Get<MojSnippetEditorConfig>().View;

            // Input group with text editor and button.
            XBInputGroup();

            // Text editor            
            ElemClass("form-control");
            ElemClass("km-with-selector");
            CustomElemStyle(context);
            OKendoTextInput(context);

            // Button for popping up the lookup dialog.            
            OSelectorControlButton(context);

            XE("</div>"); // input-group

            OMvcScriptBegin();
            O($"// Snippet editor for {propPath}");
            OnSelectorButtonClick(context, () =>
            {
                O($"const input = $(\"textarea[name='{propPath}'], input[name='{propPath}']\").first();");

                KendoGen.OOpenDialogView(context, snippetsEditorView,
                    options: new Action(() =>
                    {
                        O("mode: '{0}',", vprop.Type.MultilineString.Mode);

                        O("value: input.val(),");

                        if (snippetsEditorView.Parameters.Any())
                        {
                            OB("params:");
                            foreach (var p in snippetsEditorView.Parameters)
                                O("{0}: {1},", p.VName, Moj.JS(vprop.Snippets.Args[p.Name]));
                            End();
                        }
                    }),
                    ok: () =>
                    {
                        // Set value and fire the change event for the binding to pick up the new value.
                        O($"input.val(result.value).change();");
                    });
            });
            OMvcScriptEnd();

            return true;
        }

        void OnSelectorButtonClick(WebViewGenContext context, Action content)
        {
            // On button click...
            OB($"$('#{context.CurControlAction.CurrentId}').click(function (e)");

            content();

            End(");"); // Click handler
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        void OMvcAttrs(WebViewGenContext context, bool kendo)
        {
            if (!kendo)
                ElemDataBindAttr(context);

            OMvcAttrs(kendo);
        }
    }
}