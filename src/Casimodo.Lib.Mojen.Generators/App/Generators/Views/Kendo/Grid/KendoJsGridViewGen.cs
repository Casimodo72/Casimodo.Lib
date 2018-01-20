using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoJsGridViewGen : KendoGridGenBase
    {
        protected override void Reset()
        {
            base.Reset();

            UseClientTemplates = true;
        }

        public override void GenerateGridView(WebViewGenContext context)
        {
            ValidateView(context);

            // REMEMBER: Always escape "#" with "\#" in Kendo templates.

            // http://docs.telerik.com/kendo-ui/web/grid/appearance

            MojViewConfig view = context.View;
            MojType type = view.TypeConfig;
            string jsTypeName = FirstCharToLower(type.Name);
            string keyPropName = type.Key.Name;

            TransportConfig = this.CreateODataTransport(view, EditorView, Options.CustomQueryMethod);

            InitEvents(context);

            InitialSortProps = View.Props
                .Where(x => x.InitialSort.Is)
                .OrderBy(x => x.InitialSort.Index)
                .ToArray();

            // Grid
            GenerateGridViewCore(context);

            // Script
            GenerateScript(context);
        }

        void GenerateGridViewCore(WebViewGenContext context)
        {
            if (context.View.IsViewModelOnly || context.View.IsViewless)
                return;

            ORazorGeneratedFileComment();

            ORazorUsing("Casimodo.Lib", "Casimodo.Lib.Web",
                App.GetDataLayerConfig(context.View.TypeConfig.DataContextName).DataNamespace);

            O($"@{{ ViewBag.Title = \"{context.View.Title}\"; }}");

            O();
            OLookupDialogToolbar(context);

            // Container element for the Kendo grid widget.
            O($"<div id='{context.ComponentId}'></div>");

            // Details view Kendo template
            if (InlineDetailsView != null)
            {
                O();
                OKendoTemplateBegin($"{InlineDetailsTemplateName}");

                O("@Html.Partial(\"{0}\"){1}",
                    InlineDetailsViewVirtualFilePath,
                    (InlineDetailsView.IsEscapingNeeded ? ".ToKendoTemplate()" : ""));

                OKendoTemplateEnd();
            }

            // Editor view Kendo template
            if (EditorView != null)
            {
                O();
                OKendoTemplateBegin($"{EditorTemplateName}");

                O("@Html.Partial(\"{0}\"){1}",
                    EditorViewVirtualFilePath,
                    (EditorView.IsEscapingNeeded ? ".ToKendoTemplate()" : ""));

                OKendoTemplateEnd();
            }
        }

        public void GenerateScript(WebViewGenContext context)
        {
            WriteTo(ScriptGen, () =>
            {
                ScriptGen.PerformWrite(ViewModelScriptFilePath, () =>
                {
                    GenerateJsComponentSpaceScript(context);
                });
            });

            // View model extension script.
            GenerateViewModelExtensionJsScript(context);

            // NOTE: Writing to a dedicated script file does not always work,
            // because lookup - columns definitions need Razor functionality.
            // Thus, unfortunately, we have to put the component script into the cshtml file.
            if (!context.View.IsViewModelOnly && !context.View.IsViewless)
                GenerateComponentScript(context);
        }

        public void GenerateJsComponentSpaceScript(WebViewGenContext context)
        {
            if (View.HasFactory)
            {
                OJsImmediateBegin("factory");

                O();
                OB("factory.createSpace = function ()");

                O();
                O($"var space = {BuildComponentSpaceConstructor()};");
            }
            else
                OJsImmediateBegin("space");

            // Global data source accessor.
            O();
            OB($"space.getDataSource = function ()");
            O("return space.createViewModel().createDataSource();");
            End(";");

            GenerateJSViewModel(context);

            // Non-view-model functions.
            var funcs = JsFuncs.Functions.Where(x => !x.IsModelPart && x.Body != null).ToArray();
            if (funcs.Any())
            {
                O();
                foreach (var func in funcs)
                {
                    OB($"function {func.FunctionName} (e)");
                    func.Body(context);
                    End();
                }
            }

            if (context.View.IsViewless)
            {
                GenerateComponentOptionsFactory(context);
                GenerateComponentFactory(context);
            }

            if (View.HasFactory)
            {
                if (View.Lookup.Is)
                    throw new NotSupportedException("JS lookup views cannot have factories yet.");

                O();
                O("return space;");

                End(";"); // End factory function.

                OJsImmediateEnd(BuildNewComponentSpaceFactory(context.ComponentViewSpaceFactoryName));
            }
            else
            {
                // End namespace.
                if (View.Lookup.Is)
                {
                    // KABU TODO: IMPORTANT: Better make the space of lookups anonymous.
                    // KABU TODO: Remove bracktes which are here just to not modify the existing scripts.
                    OJsImmediateEnd($"(casimodo.run.{context.ComponentViewSpaceName} = {BuildComponentSpaceConstructor()})");
                }
                else
                    OJsImmediateEnd(BuildNewComponentSpace(context.ComponentViewSpaceName));
            }
        }

        public bool HasViewModelExtension
        {
            get
            {
                // Only if there are custom actions defined.
                if (!View.CustomActions.Any())
                    return false;

                return true;
            }
        }

        public void GenerateViewModelExtensionJsScript(WebViewGenContext context)
        {
            // Write an initial stub for the view model extension,
            // which is intended to be further manually edited.

            if (!HasViewModelExtension)
                return;

            // Only if the file does not exist already.
            if (System.IO.File.Exists(ViewModelExtensionScriptFilePath))
                return;

            WriteTo(ScriptGen, () =>
            {
                ScriptGen.PerformWrite(ViewModelExtensionScriptFilePath, () =>
                {
                    OJsNamespace(DataConfig.ScriptUINamespace, (nscontext) =>
                    {
                        OJsClass(nscontext.Current, ViewModelExtensionClassName,
                            extends: "casimodo.ui.ComponentViewModelExtensionBase",
                            args: "options");
                    });
                });
            });
        }

        public void GenerateComponentScript(WebViewGenContext context)
        {
            // NOTE: Writing to a dedicated script file for the component does not always work,
            // because the lookup-column definitions of the kendo grid need Razor functionality.
            // Thus, unfortunately, we *have to* put the component script into the cshtml file.

            O();
            if (HasViewModelExtension)
                OScriptReference(ViewModelExtensionScriptVirtualFilePath);
            OScriptReference(ViewModelScriptVirtualFilePath);

            O();
            OScriptBegin();

            // Begin component namespace.
            OB("(function (space)");

            // KABU TODO: Maybe we should throw an error if deferred and manual init was configured.            
            if (Options.IsDeferred)
            {
                // Workaround: MVVM bindings go terribly wrong when an other view model
                // overrides the grid's own view model.
                // See http://www.telerik.com/forums/grid-filter-row-weird-behaviour
                //O();
                OJsDocReadyBegin();
                OB("setTimeout(function()");
            }

            GenerateComponentOptionsFactory(context);
            GenerateComponentFactory(context);

            O();
            O($"if (space.options.isManualInit !== true) space.create();");
            O();

            if (Options.IsDeferred)
            {
                End(");"); // Timeout function

                OJsDocReadyEnd();
            }

            // End component namespace.
            End($")(casimodo.run.{context.ComponentViewSpaceName});");

            OScriptEnd();
        }

        public void GenerateComponentOptionsFactory(WebViewGenContext context)
        {
            // Grid options factory function.
            O();
            OB("space.createComponentOptions = function()");

            // NOTE: We are using the internal data source factory function.
            OB("var options ="); GenerateGridOptions(context);
            End(";"); // Grid options object.
            O();
            // Apply override if applicable.
            O("if (space.createComponentOptionsOverride)");
            O("    options = space.createComponentOptionsOverride(options);");
            O();
            O("return options;");
            End(";"); // Grid options factory function.            
        }

        public void GenerateComponentFactory(WebViewGenContext context)
        {
            // Grid factory function.
            O();
            OB($"space.createComponent = function (options)");

            var options = $"{{ space: space, viewId: '{View.Id}', componentId: '{context.ComponentId}', " +
                $"areaName: '{View.TypeConfig.PluralName}', componentOptions: options, " +
                $"isDialog: {MojenUtils.ToJsValue(View.Lookup.Is)}, isAuthNeeded: {MojenUtils.ToJsValue(View.IsAuthorizationNeeded)} }}";

            O($"kendomodo.createGridComponentOnSpace({options});");

            End(";"); // Grid factory function.            
        }

        void GenerateGridOptions(WebViewGenContext context)
        {
            MojViewConfig view = context.View;

            // Sorting ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            OXP("sortable",
                "allowUnsort: false",
                "mode: 'single'" // 'single' | 'multiple'
            );

            // Paging ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            if (Options.IsPagerVisible)
            {
                OXP("pageable",
                    $"refresh: {MojenUtils.ToJsValue(Options.Pager.UseRefresh)}",
                    $"input: {MojenUtils.ToJsValue(Options.Pager.UseInput)}",
                    $"pageSizes: {MojenUtils.ToJsValue(Options.Pager.UsePageSizes)}"
                );
            }

            if (Options.Height != null)
            {
                O($"height: {MojenUtils.ToJsValue(Options.Height)},");
            }

            O($"scrollable: {MojenUtils.ToJsValue(Options.IsScrollable)},");

            // Grid events ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            O("// Events");
            foreach (var handler in JsFuncs.ComponentEventHandlers)
            {
                // E.g "save: kendomodo.onGridSaving,"
                var eve = FirstCharToLower(handler.ComponentEventName);

                if (handler.IsModelPart)
                    O($"{eve}: $.proxy(space.vm.{handler.FunctionName}, space.vm),");
                else
                    O($"{eve}: {handler.FunctionName},");
            }

            // Row selection ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (view.ItemSelection.IsEnabled && !view.ItemSelection.UseCheckBox)
            {
                OXP("selectable",
                    $"mode: {(view.ItemSelection.IsMultiselect ? "'multiple'" : "'row'")}"
                );

                // "row" - the user can select a single row.
                // "cell" - the user can select a single cell.
                // "multiple, row" - the user can select multiple rows.
                // "multiple, cell" - the user can select multiple cells.                
            }

            // Expandable details template ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (InlineDetailsView != null)
            {
                O($"detailTemplate: kendo.template($('#{InlineDetailsTemplateName}').html()),");
            }

            // Editor template ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (CanEdit)
            {
                // KABU TODO: LOCALIZE
                var title = $"{view.TypeConfig.DisplayName}";

                OXP("editable",

                    // Use pop up editor.
                    "mode: 'popup'", //  "incell", "inline" and "popup".

                    // NOTE: Without that Kendo will not display a deletion-confirmation dialog prior to deletion.
                    "confirmation: true",

                    // Editor template
                    $"template: kendo.template($('#{EditorTemplateName}').html())",

                    // Editor window
                    XP("window",
                        $"title: '{title}'",
                        "animation: kendomodo.getDefaultDialogWindowAnimation()",
                        "open: kendomodo.onModalWindowOpening",
                        "activate: kendomodo.onModalWindowActivated",
                        // NOTE: We must not set the grid-popup-window's options close handler,
                        //   because the grid sets and uses it. We would brake the grid's own
                        //   way of closing the popup window.
                        // DONT: "close: kendomodo.onModalWindowClosed",
                        EditorView.Width != null ? $"width: {EditorView.Width}" : null,
                        EditorView.MinWidth != null ? $"minWidth: {EditorView.MinWidth}" : null,
                        EditorView.MaxWidth != null ? $"maxWidth: {EditorView.MaxWidth}" : null,
                        EditorView.MinHeight != null ? $"minHeihgt: {EditorView.MinHeight}" : null
                    )
                );
            }

            // Tool bar ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // NOTE: The grid will now always have a toolbar, because we have a custom refresh button.
            if (HasToolbar && Options.IsHeaderVisible)
            {
                OXArr("toolbar",
                    XObj(
                        // Toolbar template
                        XP("template", () =>
                        {
                            o($"kendo.template(\"");

                            KendoJsTemplate(() =>
                            {
                                o("<div class='toolbar'>");

                                if (view.IsExportableToPdf)
                                    o("<button class='k-button k-grid-pdf'><span class='k-icon k-i-pdf'></span></button>");

                                if (view.IsExportableToExcel)
                                    o("<button class='k-button k-grid-excel'><span class='k-icon k-i-excel'></span></button>");

                                // Add refresh button
                                o("<button class='k-button k-grid-refresh'><span class='k-icon k-i-refresh'></span></button>");

                                if (view.IsGuidFilterable)
                                    o("<button class='k-button kmodo-clear-guid-filter-command' style='display:none'>Filter entfernen</button>");

                                if (view.CustomActions.Any())
                                {
                                    foreach (var action in view.CustomActions)
                                    {
                                        if (action.Kind == MojViewActionKind.Toggle)
                                        {
                                            o($"<button class='k-button custom-command' name='{action.Name}'");
                                            if (!action.IsVisible)
                                                o(" style ='display:none'");
                                            o($">{action.DisplayName}</button>");
                                        }
                                        else throw new MojenException($"Unhandled view action kind '{action.Kind}'.");
                                    }
                                }

                                if (CanCreate)
                                {
                                    // Add a create (+) button.
                                    // NOTE: Escaping # needs 2 backslashes here.
                                    o("<a class='k-button k-grid-add hide' href='#'><span class='k-icon k-add'></span></a>");
                                }

                                o("</div>");
                            });

                            o("\")"); // Template
                        })
                    )
                );
            }

            // Export ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (view.IsExportableToPdf)
            {
                OXP("pdf",
                    $"fileName: '{view.TypeConfig.DisplayPluralName}'"
                );
            }

            if (view.IsExportableToExcel)
            {
                OXP("excel",
                    $"fileName: '{view.TypeConfig.DisplayPluralName}'"
                );
            }

            // Column menu
            O("columnMenu: false,");

            // Show filters in column headers
            if (view.IsFilterable)
            {
                OXP("filterable",
                    // "row" - the user can filter via extra row within the header.
                    // "menu" - the user can filter via the menu after clicking the filter icon.
                    // "menu, row" - the user can filter with both modes above enabled.
                    "mode: 'row'"
                );
            }
            else
            {
                O("filterable: false,");
            }

            // Columns ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~            
            O("columns: [");
            Push();

            GenerateColumns(context);

            // Edit button column ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (CanEdit)
            {
                OB("");
                Oo("template: kendo.template(\"");

                KendoJsTemplate(() =>
                {
                    // KABU TODO: REMOVE wrapper, because we now have one button only.
                    // Avoid wrapping multiple buttons.
                    //o("<div style='white-space:nowrap'>");

                    if (CanEdit)
                        // Add an edit (/) button.
                        o(@"<a class='k-button k-grid-edit' href='#'><span class='k-icon k-edit'></span></a>");

                    // KABU TODO: REMOVE: The delete button now resides on the editor view.
                    //if (CanDelete)                    
                    //    o(@"<a class='k-button k-grid-delete' href='#'><span class='k-icon k-delete'></span></a>");

                    //o("</div>");
                });

                oO("\")"); // End of template
                End();
            }

            Pop(); // Columns
            O("],");

            O("autoBind: false,");
            // KABU TODO: REMOVE: We don't auto bind anymore.
            //if (View.Lookup.Is)
            //    O("autoBind: false,");
            //else
            //    O("autoBind: space.options.isManualDataLoad ? false : true,");

            // NOTE: The data source options are created by the view model.
            O("dataSource: space.vm.createDataSourceOptions(),");
        }

        void GenerateColumns(WebViewGenContext context)
        {
            var view = context.View;
            var index = 0;

            if (view.ItemSelection.IsMultiselect && view.ItemSelection.UseCheckBox)
            {
                var selector = new MojViewCustomControl
                {
                    Type = "CheckBox",
                    SubType = "ListItemSelectorCheckBox"
                };

                if (view.ItemSelection.UseAllCheckBox)
                    selector.Attrs.Set("AllItemsSelector", "true");

                GenerateCustomControlColumns(context, selector);
            }

            foreach (var vprop in view.Props)
            {
                var control = view.CustomControls.FirstOrDefault(x => x.Index == index);
                if (control != null)
                    GenerateCustomControlColumns(context, control);

                GeneratePropColumn(context, vprop);

                index++;
            }
        }

        void GenerateCustomControlColumns(WebViewGenContext context, MojViewCustomControl control)
        {
            var view = context.View;

            OB("");

            if (control.Type == "CheckBox")
            {
                if (control.SubType == "ListItemSelectorCheckBox")
                {
                    O($"field: 'ListItemSelectorCheckBox',");

                    O("title: ' ',");

                    // Hide initially
                    O("hidden: true,");

                    O("width: 30,");

                    if (control.Attrs.FindOrDefault("AllItemsSelector") == "true")
                    {
                        O("headerAttributes: { 'class': 'all-list-items-selector' },");
                        O($"headerTemplate: \"<input id='cb-{view.Id}' class='k-checkbox all-list-items-selector' type='checkbox' /><label class='k-checkbox-label' for='cb-{view.Id}' />\",");
                    }

                    O("attributes: { 'class': 'list-item-selector' },");
                    O($"template: \"<input id='cb-#:Id#' class='k-checkbox list-item-selector' type='checkbox' /><label class='k-checkbox-label list-item-selector' for='cb-#:Id#' style='display:none'/>\",");

                    O("filterable: false,");
                    O("sortable: false");
                }
                else throw new MojenException($"Unexpected custom control sub type '{control.SubType ?? ""}'.");
            }
            else throw new MojenException($"Unexpected custom control type '{control.Type}' (SubType: '{control.SubType ?? ""}').");

            End(",");
        }

        void GeneratePropColumn(WebViewGenContext context, MojViewProp vprop)
        {
            if (vprop.HideModes == MojViewMode.All)
                return;

            var vinfo = vprop.BuildViewPropInfo(column: true);
            //var prop = info.Prop;
            var propPath = vinfo.PropPath;
            var propAliasPath = vinfo.PropAliasPath;

            if (vprop.FileRef.Is && vprop.FileRef.IsImage)
                // KABU TODO: IMPL: Photos are currently disabled
                return;

            string template = null;

            OB("");

            bool isEffectField = vprop.IsColor || vprop.FileRef.Is;

            O($"field: '{vinfo.PropPath}',");

            // KABU TODO: Envaluate custom column info.
            // E.g. attributes: {
            // See http://stackoverflow.com/questions/32111666/kendo-ui-grid-datasource-schema-model-fields-add-custom-attributes-to-fields

            // Column label
            if (isEffectField)
            {
                // Need to use " " if we don't want a column label, otherwise Kendo will
                // insist in displaying the field's name.
                O($"title: ' ',");
            }
            else
            {
                var label = vinfo.CustomDisplayLabel != null ? vinfo.CustomDisplayLabel : vinfo.ViewProp.DisplayLabel;
                if (label == null)
                    throw new MojenException("No label found for grid column.");

                // Need to use " " if we don't want a column label, otherwise Kendo will
                // insist in displaying the field's name.
                if (string.IsNullOrWhiteSpace(label))
                    label = " ";

                O($"title: '{label}',");
            }

            // KABU TODO: VERY IMPORTANT: FIX: We incorrectly operate on the vprop rather than the target prop.

            if (vprop.IsHtml)
            {
                O($"encoded: false,");
            }

            var iscolor = vprop.IsColor || vprop.IsColorWithOpacity;
            if (iscolor)
            {
                // Color cell Kendo template.
                O($"width: 33,");
                template = $"'#=kendomodo.getColorCellTemplate({propPath})#'";
            }

            // Image file reference
            if (vprop.FileRef.Is && vprop.FileRef.IsImage)
            {
                O($"width: 70,");
                template = "'#=kendomodo.getShowPhotoCellTemplate({propAliasPath}Uri)#'";
            }
            else if (vprop.FileRef.Is)
                // KABU TODO: What about other file references?
                throw new MojenException("This kind of file reference is not supported yet.");

            // Date time formatting.
            if (vprop.Type.IsAnyTime)
            {
                var format = "{0:";
                // KABU TODO: LOCALIZE DateTime format.
                if (vprop.Type.DateTimeInfo.IsDate)
                    format += "dd.MM.yyyy ";
                if (vprop.Type.DateTimeInfo.IsTime)
                    format += "HH:mm:ss";
                // KABU TODO: Support milliseconds
                format += "}";

                O($"format: '{format}',");

                // KABU TODO: REMOVE?
                /*
                var tmpl = "";
                if (vprop.Type.DateTimeInfo.IsDate)
                    tmpl += $"#=kendo.toString({vinfo.PropPath}, 'dd.MM.yyyy')#<br/>";
                if (vprop.Type.DateTimeInfo.IsTime)
                    tmpl += $"#=kendo.toString({vinfo.PropPath}, 'HH:mm:ss')#";
                // KABU TODO: Support milliseconds
                
                O($"template: \"<div>{tmpl}</div>\",");
                */

                // KABU TODO: REMOVE?
                // format: "{0:c}"
                //O("format: '@(Html.GetDateTimePattern(placeholder: true{0}{1}{2}))',",
                //    (vprop.Type.DateTimeInfo.IsDate == false ? ", date: false" : ""),
                //    (vprop.Type.DateTimeInfo.IsTime == false ? ", time: false" : ""),
                //    (vprop.Type.DateTimeInfo.DisplayMillisecondDigits > 0 ? ", ms: " + vprop.Type.DateTimeInfo.DisplayMillisecondDigits : ""));
            }
            // Check-box Kendo template.
            else if (vprop.Type.IsBoolean)
            {
                template = $"'#=casimodo.toDisplayBool({vprop.Name})#'";
                //o($".ClientTemplate(@\"<input type='checkbox' disabled #= {prop.Name} ? checked='checked' : '' # />\")");
            }
            else if (vprop.Type.IsTimeSpan)
            {
                template = $"'#=casimodo.toDisplayTimeSpan({vprop.Name})#'";
                //o($".ClientTemplate(@\"<input type='checkbox' disabled #= {prop.Name} ? checked='checked' : '' # />\")");
            }
            // Reference
            else if (vprop.Reference.IsToOne) // REMOVE: vprop.FormedNavigationTo.Is && 
            {
                // IMPORTANT NOTE:
                // Provide template, because Kendo will break if an itermediate property
                // in the property path is null.
                // Example:
                // template: "#if(Company!=null){##:Company.NameShort##}#",                
                template = $"\"{KendoGen.GetPlainDisplayTemplate(vprop, checkLastProp: true)}\"";
            }
            else if (vprop.Reference.Is)
            {
                throw new MojenException("This kind of reference is not supported.");
            }

            // KABU TODO: IMPORTANT: Currently we hard-code all navigated-to properties to be sortable,
            //   because the view-property (which is a clone of the type's native property)
            //   is a reference property and is *not* sortable by default (set in MojClassPropBuilder).
            if (!vprop.IsSortable && !vprop.FormedNavigationTo.Is)
                O("sortable: false,");

            if (!vprop.IsGroupable)
                O("groupable: false,");

            // Filterable ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (!vprop.IsFilterable)
            {
                O("filterable: false,");
            }
            else
            {
                OB("filterable:");

                // Filter grid cell
                OB("cell:");

                // Other filter cell options:
                // enabled: true,
                // delay: 1500

                string cellTemplate = null;

                // Specifies whether to show or hide the DropDownList with the operators.
                bool showOperators = false;

                string @operator = null;

                string dataSourceUrl = null;

                string dataSourceData = null;

                // Specifies the AutoComplete filter option. Possible values are same as the one 
                // for the AutoComplete filter option- "startswidht", "endswith", "contains".
                string dataSourceAutoCompleteOperator = null;

                string dataSourceTextField = null;

                // Use this options to enable the MultiCheck filtering support for that column.
                // WARNING: If you have enabled the columns.multi option and your Grid uses serverPaging 
                // (or ServerOperations(true) if using the MVC wrappers) you need to provide columns.filterable.dataSource. 
                // If columns.filterable.dataSource is not provided: bad performance.
                bool multi = false;

                if (vprop.Type.IsEnum)
                {
                    // Enums will be looked up based on static values.
                    dataSourceData = $"@(PickItemsHelper.ToJsArray<{vprop.Type.NameNormalized}>(names: true))";

                    // DropDownList template
                    var nullable = vprop.Type.CanBeNull;
                    cellTemplate = $"kendomodo.gridEnumFilterColTemplate{(nullable ? "Nullable" : "")}";

                    dataSourceTextField = "text";
                }
                else if (vprop.Reference.Is)
                {
                    // Reference

                    // The property to be used for display of the referenced value.
                    var targetDisplayProp = vinfo.TargetDisplayProp;

                    if (vinfo.TargetType.DataSetSize == MojDataSetSizeKind.ExtraSmall)
                    {
                        // Enums and extra small data-sets will be looked up based on static values.
                        // We'll use a DropDownList provided via the filterable cell template.

                        //@operator = "eq";

                        var repository = vinfo.TargetType.PluralName;
                        dataSourceData = $"@(PickItemsContainer.Get{repository}AsJsArray(\"{targetDisplayProp}\", id: false))";

                        // DropDownList template
                        var nullable = vprop.Reference.IsToZero;
                        cellTemplate = $"kendomodo.gridReferenceFilterColTemplate{(nullable ? "Nullable" : "")}";
                    }
                    else
                    {
                        if (!targetDisplayProp.Type.IsString)
                            throw new MojenException("AutoComplete column filters are intended for strings only.");

                        dataSourceTextField = targetDisplayProp.Name;
                        @operator = "contains";
                        dataSourceAutoCompleteOperator = "contains";

                        if (vprop.IsReferenceLookupDistinct)
                        {
                            // Kendo's filterCell will use the view property for removing duplicates,
                            //   which does not work with our foreign data, so we need
                            //   to provide distinct values server-side.
                            dataSourceUrl = string.Format("{0}/{1}(On='{2}')?$select={2}&$orderby={2}",
                                this.GetODataPath(vinfo.TargetType),
                                this.GetODataQueryFunc(true),
                                targetDisplayProp.Name);
                        }
                        else
                        {
                            dataSourceUrl = string.Format("{0}/{1}()?$select={2}&$orderby={2}",
                                this.GetODataPath(vinfo.TargetType),
                                this.GetODataQueryFunc(false),
                                targetDisplayProp.Name);
                        }
                    }
                }
                else
                {
                    // Native property

                    if (vprop.Type.IsString)
                    {
                        // Use "contains" as default operator for strings.
                        @operator = "contains";
                    }

                    // Example "odata/Contracts/Ga.Query()?$select=Number"
                    dataSourceUrl = TransportConfig.ODataFilterUrl + vprop.Name;
                }

                if (cellTemplate != null)
                    O($"template: {cellTemplate},");

                if (!showOperators)
                    O($"showOperators: false,");

                if (@operator != null)
                    O($"operator: '{@operator}',");

                if (multi)
                    O($"multi: true,");

                if (dataSourceAutoCompleteOperator != null)
                    O($"suggestionOperator: '{dataSourceAutoCompleteOperator}',");

                // KABU TODO: REVISIT: This does not work.
                //if (Options.IsFilterOverCurrentDataEnabled && dataSourceData == null && !vprop.FormedNavigationTo.Is)
                //{
                //    OB("ui: function(element)");
                //    OB("element.kendoAutoComplete(");
                //    O($"dataSource: dspace.vm.getDataSourceForProp('{vprop.Name}'),");
                //    O($"dataTextField: '{vprop.Name}'");
                //    End(")");
                //    End();
                //}
                //else if (Options.IsUsingLocalData && dataSourceData == null)
                //{
                //    // KABU TODO: REVISIT: Doesn't work. Don't use yet.
                //    OB("ui: function(element)");
                //    OB("element.kendoAutoComplete(");
                //    O("dataSource: dataSource,");
                //    O($"dataTextField: '{propPath}'");
                //    End(")");
                //    End();
                //}
                //else 

                if (dataSourceUrl != null || dataSourceData != null)
                {
                    // OData data source
                    OB("dataSource:");

                    if (dataSourceUrl != null)
                    {
                        O($"type: \"odata-v4\",");
                        O($"transport: {{ read: {{ url: \"{dataSourceUrl}\" }} }}");

                        // KABU TODO: REVISIT: May want to enable serverFiltering, so that
                        //   the values are always re-fetched from server.
                        //serverFiltering: true, 
                    }

                    if (dataSourceData != null)
                    {
                        O($"data: {dataSourceData},");
                    }

                    End(","); // DataSource

                    if (dataSourceTextField != null)
                        O($"dataTextField: '{dataSourceTextField}',");
                }

                End(); // Cell

                End(","); // Filterable
            }

            // KABU TODO: Move logic into MojViewProp
            if (vprop.Width != null && !isEffectField)
            {
                O("width: {0},", vprop.Width.Value);
            }

            // KABU TODO: Coloring a foreign key column based on other values does not work
            // because only the entity is in scope of the ClientTemplate - not the needed foreign display value.                
            // Such display values reside somewhere on the model.values of
            // the grid or the data-source itself and are only accessible if we
            // generate and use a JS function for finding the foreign key's display value,
            // which is quite a mess.
            // See http://www.telerik.com/forums/client-template-%28hyper-link%29-on-foreign-key-column
#if (false)
                if (vprop.ColorProp != null)
                {
                    var pathString = vprop.ColorProp.FormedNavigationPath.PathString;
                    o(".ClientTemplate(@\"<span style='color:#:{0}#'>#:{1}#</span>\")",
                        vprop.ColorProp.FormedNavigationPath.PathString,
                        prop.Name);
                }
#endif
            if (vprop.CustomTemplateName != null)
            {
                var dataConfig = App.GetDataLayerConfig(context.View.TypeConfig.DataContextName);
                template = $"{dataConfig.ScriptNamespace}.templates.getTemplate('{vprop.CustomTemplateName}')";
            }

            if (template != null)
                O($"template: {template},");

            End(","); // Column
        }

        void ValidateView(WebViewGenContext context)
        {
            // KABU TODO: REVISIT: Currently disabled because this barks at JobDefinition.StartOn
            //    and I currently don't know how to avoid this.
#if (false)
            var view = context.View;

            // Validate properties
            foreach (var prop in view.Model.GetProps(true, custom: true))
            {
                var defaultValueArg = prop.Attrs.GetDefaultValueArg();

                if (defaultValueArg == null && prop.Type != null && prop.Type.IsValueType &&
                    !prop.IsNullableValueType && !prop.IsKey && prop.Type != typeof(Guid))
                {
                    throw new MojenException(string.Format("The model prop '{0}' is not nullable and has no default value.", prop.Name));
                }
            }
#endif
        }


        void InterestingJavaScriptStuff()
        {
            // Kendo data source ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // dataSource.query:
            // Executes the specified query over the data items. Makes a HTTP request if bound to a remote service.
            // http://docs.telerik.com/kendo-ui/api/javascript/data/datasource#methods-query
            // Example:
            // dataSource.query( {
            //    sort: [ /* sort descriptors */], 
            //    group: [ /* group descriptors */ ], 
            //    page: dataSource.page(), 
            //    pageSize:
            //    dataSource.pageSize()
            // });
        }
    }
}