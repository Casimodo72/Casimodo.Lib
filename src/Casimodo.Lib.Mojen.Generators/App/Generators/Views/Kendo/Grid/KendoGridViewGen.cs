using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class WebScriptGen : WebPartGenerator
    { }

    public partial class CustomMainViewGen : WebViewGenerator
    {
        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>().Where(x => x.Uses(this)))
            {
                if (!view.IsCustom)
                    throw new MojenException("This view must be custom.");

                if (!view.Kind.Roles.HasFlag(MojViewRole.Index))
                    throw new MojenException("This view must have an index role.");

                var context = new WebViewGenContext
                {
                    View = view
                };
                RegisterComponent(context);
            }
        }
    }

    public partial class KendoGridViewGen : KendoViewGenBase
    {
        public KendoGridViewGen()
        {
            EditorGen = AddSub<KendoFormEditorViewGen>();
            ScriptGen = AddSub<WebScriptGen>();
            InlineDetailsGen = AddSub<KendoGridInlineDetailsViewGen>();
            EditorDataModelGen = AddSub<WebDataEditViewModelGen>();
        }

        void Reset()
        {
            TransportConfig = null;
            InlineDetailsTemplateName = null;
        }

        public WebScriptGen ScriptGen { get; set; }

        public KendoGridInlineDetailsViewGen InlineDetailsGen { get; set; }

        public string InlineDetailsTemplateName { get; set; }

        public WebDataEditViewModelGen EditorDataModelGen { get; set; }

        public KendoFormEditorViewGen EditorGen { get; set; }

        public bool CanCreate { get; set; }

        public bool CanModify { get; set; }

        public bool CanDelete { get; set; }

        public KendoGridOptions Options { get; set; }

        public MojHttpRequestConfig TransportConfig { get; set; }

        public string InlineDetailsViewFilePath { get; set; }
        public string InlineDetailsViewVirtualFilePath { get; set; }

        public string GridScriptFilePath { get; set; }
        public string GridScriptVirtualFilePath { get; set; }

        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>()
                .Where(x => x.Uses(this) && !x.IsCustom && x.Kind.Roles.HasFlag(MojViewRole.List)))
            {
                Reset();

                KendoGen.ImplicitelyBindEditorView<KendoFormEditorViewGen>(view);

                Options = view.GetGeneratorConfig<KendoGridOptions>() ?? new KendoGridOptions();
                TransportConfig = this.CreateODataTransport(view, null, Options.CustomQueryMethod);

                var context = new WebViewGenContext
                {
                    View = view,
                    UINamespace = GetJsScriptUINamespace(view)
                };
                KendoGen.InitComponentNames(context);
                context.ComponentId = "grid-" + view.Id;

                // Inline details              
                if (view.InlineDetailsView != null)
                {
                    InlineDetailsTemplateName = "grid-details-template-" + view.Id;
                    InlineDetailsViewVirtualFilePath = BuildVirtualFilePath(view.InlineDetailsView, name: "Details.Inline", partial: true);
                    InlineDetailsViewFilePath = BuildFilePath(view.InlineDetailsView, name: "Details.Inline", partial: true);
                }

                // Edit capabilities
                CanModify = !view.Kind.Roles.HasFlag(MojViewRole.Lookup) && view.EditorView != null && view.EditorView.CanEdit;
                CanCreate = CanModify && view.EditorView != null && view.EditorView.CanCreate && Options.IsCreatable;
                CanDelete = CanModify && Options.IsDeletable == true || (view.EditorView != null && view.EditorView.CanDelete && (Options.IsDeletable ?? true));

                GridScriptFilePath = BuildJsScriptFilePath(view, suffix: ".vm.generated");
                GridScriptVirtualFilePath = BuildJsScriptVirtualFilePath(view, suffix: ".vm.generated");

                // Generate grid.
                if (view.IsCustom)
                    throw new MojenException("KendoGridView must not be custom.");

                GenerateGrid(context);

                // Generate inline details view
                if (view.InlineDetailsView != null && !view.InlineDetailsView.IsCustom)
                {
                    InlineDetailsGen.PerformWrite(InlineDetailsViewFilePath, () =>
                    {
                        InlineDetailsGen.GenerateView(new WebViewGenContext
                        {
                            IsEditableView = false,
                            IsModalView = view.IsModal,
                            View = view.InlineDetailsView,
                            ViewRole = "inline-details",
                            // NOTE: inline views won't have an ID, because
                            // there will be multiple on a single page.
                            IsViewIdEnabled = false
                        });
                    });
                }

                RegisterComponent(context);
            }
        }

        void GenerateGrid(WebViewGenContext context)
        {
            ValidateView(context);

            // http://docs.telerik.com/kendo-ui/web/grid/appearance

            MojViewConfig view = context.View;
            MojType type = view.TypeConfig;
            string jsTypeName = FirstCharToLower(type.Name);
            string keyPropName = type.Key.Name;

            if (string.IsNullOrWhiteSpace(view.Id))
                throw new MojenException("View ID is missing.");

            // View & style
            if (!context.View.IsViewless)
            {
                PerformWrite(context.View, () =>
                {
                    GenGridView(context);

                    O();
                    GenGridStyle(context);
                });
            }

            // Script
            WriteTo(ScriptGen, () =>
                 ScriptGen.PerformWrite(GridScriptFilePath, () =>
                     GenGridScript(context)));
        }

        void GenGridView(WebViewGenContext context)
        {
            ORazorGeneratedFileComment();

            ORazorUsing("Casimodo.Lib", "Casimodo.Lib.Web",
                App.GetDataLayerConfig(context.View.TypeConfig.DataContextName).DataNamespace);

            O($"@{{ ViewBag.Title = \"{context.View.Title}\"; }}");

            O();
            if (context.View.Kind.Roles.HasFlag(MojViewRole.Lookup))
            {
                // Lookup view will have an ID assigned.
                if (string.IsNullOrWhiteSpace(context.View.Id)) throw new MojenException("The lookup view has no ID.");

                XB($"<div class='casimodo-dialog-toolbar' id='dialog-commands-{context.View.Id}'>");

                O("<button class='k-button cancel-button casimodo-dialog-button'>Abbrechen</button>");
                O("<button class='k-button ok-button casimodo-dialog-button'>OK</button>");

                XE("</div>");
            }

            // Container element for the Kendo grid widget.
            O($"<div id='{context.ComponentId}'></div>");

            // Details view Kendo template
            if (context.View.InlineDetailsView != null)
            {
                O();
                OKendoTemplateBegin($"{InlineDetailsTemplateName}");

                OMvcPartialView(InlineDetailsViewVirtualFilePath, kendoEscape: context.View.InlineDetailsView.IsEscapingNeeded);

                OKendoTemplateEnd();
            }
        }

        void GenGridScript(WebViewGenContext context)
        {
            OUseStrict();

            KendoGen.OBeginComponentSpace(context);

            O();
            GenGridViewModelFactory(context);

            O();
            GenGridOptionsFactory(context);

            KendoGen.OEndComponentSpace(context);
        }

        void GenGridViewModelFactory(WebViewGenContext context)
        {
            // View model factory          
            OB($"space.createViewModel = function (spaceOptions)");
            O("if (space.vm) return space.vm;");
            O();

            GenGridViewModelCore(context);

            O();
            O("return space.vm;");

            End(";"); // View model factory                        
        }

        void GenGridViewModelCore(WebViewGenContext context)
        {
            var view = context.View;

            KendoGen.OViewModelClass("ViewModel", extends: "kendomodo.ui.GridViewModel",
            constructor: () =>
            {
                O($"this.keyName = \"{context.View.TypeConfig.Key.Name}\";");
            },
            content: () =>
            {
                // OData read query URL factory
                KendoGen.OPropValueFactory("readQuery", TransportConfig.ODataSelectUrl);

                // Data model factory (used by the Kendo data source).
                O();
                KendoGen.ODataSourceModelFactory(context, TransportConfig);

                // Data source options factory.
                O();
                KendoGen.ODataSourceOptionsFactory(context, () =>
                    KendoGen.ODataSourceListOptions(context,
                        TransportConfig,
                        create: CanCreate,
                        modify: CanModify,
                        delete: CanDelete,
                        pageSize: Options.PageSize,
                        isServerPaging: Options.IsServerPaging,
                        initialSortProps: context.View.Props
                            .Where(x => x.InitialSort.Is)
                            .OrderBy(x => x.InitialSort.Index)
                            .ToArray()));

                KendoGen.OBaseFilters(context);

                // KABU TODO: REMOVE
                //if (context.View.EditorView != null)
                //    KendoGen.OViewModelOnEditing(context.View.EditorView, CanCreate);
            });

            // Create view model with options.
            O();
            OB("space.vm = new ViewModel(");
            KendoGen.OViewModelOptions(context, isList: true);
            End(").init();");
        }

        public void GenGridOptionsFactory(WebViewGenContext context)
        {
            // Grid options factory function.
            OB("space.createComponentOptions = function()");

            // NOTE: We are using the internal data source factory function.
            OB("var options ="); GenGridOptions(context);
            End(";"); // Grid options object.
            O();
            // Apply override if applicable.
            O("if (space.createComponentOptionsOverride)");
            O("    options = space.createComponentOptionsOverride(options);");
            O();
            O("return options;");
            End(";"); // Grid options factory function.            
        }

        void GenGridOptions(WebViewGenContext context)
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

            // KABU TODO: REMOVE: All events will now be atteched in the view model.
            // Grid events ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //O("// Events");
            //foreach (var handler in JsFuncs.EventHandlers)
            //{
            //    // E.g "save: kendomodo.onGridSaving,"
            //    var eve = FirstCharToLower(handler.ComponentEventName);
            //    O($"{eve}: $.proxy(space.vm.{handler.FunctionName}, space.vm),");
            //}

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
            if (context.View.InlineDetailsView != null)
            {
                O($"detailTemplate: kendo.template($('#{InlineDetailsTemplateName}').html()),");
            }

            // Editor template ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (CanModify)
            {
                // KABU TODO: LOCALIZE
                var title = $"{view.TypeConfig.DisplayName}";

                var editorView = context.View.EditorView;

                OXP("editable",

                    // Use pop up editor.
                    "mode: 'popup'", //  "incell", "inline" and "popup".

                    // NOTE: Without that Kendo will not display a deletion-confirmation dialog prior to deletion.
                    "confirmation: true",

                    // KABU TODO: REMOVE: We are not using the built in popup editor anymore.
                    //// Editor template
                    //$"template: kendo.template($('#{EditorTemplateName}').html())",

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

                        // KABU TODO: Can we put the following into its own helper function?
                        $"width: {MojenUtils.ToJsValue(editorView.Width)}",
                        $"minWidth: {MojenUtils.ToJsValue(editorView.MinWidth)}",
                        $"maxWidth: {MojenUtils.ToJsValue(editorView.MaxWidth)}",
                        $"height: {MojenUtils.ToJsValue(editorView.Height)}",
                        $"minHeihgt: {MojenUtils.ToJsValue(editorView.MinHeight)}",
                        $"maxHeihgt: {MojenUtils.ToJsValue(editorView.MaxHeight)}"
                    )
                );
            }

            // Tool bar ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // NOTE: The grid will now always have a toolbar, because we have a custom refresh button.
            if (Options.IsHeaderVisible)
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

                                // KABU TODO: REMOVE?
                                //if (view.CustomActions.Any())
                                //{
                                //    foreach (var action in view.CustomActions)
                                //    {
                                //        if (action.Kind == MojViewActionKind.Toggle)
                                //        {
                                //            o($"<button class='k-button custom-command' name='{action.Name}'");
                                //            if (!action.IsVisible)
                                //                o(" style ='display:none'");
                                //            o($">{action.DisplayName}</button>");
                                //        }
                                //        else throw new MojenException($"Unhandled view action kind '{action.Kind}'.");
                                //    }
                                //}

                                if (CanCreate)
                                {
                                    // Add a create (+) button.
                                    // NOTE: We are using a custom "create" button (k-grid-custom-add) instead of
                                    //   kendo grid's default button (k-grid-add).
                                    o("<a class='k-button k-grid-custom-add hide' href='#'><span class='k-icon k-add'></span></a>");
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

            GenGridColumns(context);

            // Edit button column ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (CanModify)
            {
                OB("");
                Oo("template: kendo.template(\"");

                KendoJsTemplate(() =>
                {
                    // KABU TODO: REMOVE wrapper, because we now have one button only.
                    // Avoid wrapping multiple buttons.
                    //o("<div style='white-space:nowrap'>");

                    if (CanModify)
                        // Add an edit button.
                        // NOTE: This is a custom button which is not handled automatically
                        //   by the kendo.ui.Grid. The grid view model will attach
                        //   to this button and strat the edit operation manually.
                        o(@"<a class='k-button k-grid-custom-edit' href='#' style='display:none'><span class='k-icon k-edit'></span></a>");

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
            O("dataSource: space.vm.createDataSource(),");
            // TOOD: REMOVE: O("dataSource: space.vm.createDataSourceOptions(),");
        }

        void GenGridColumns(WebViewGenContext context)
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

                GenGridCustomControlColumn(context, selector);
            }

            foreach (var vprop in view.Props)
            {
                var control = view.CustomControls.FirstOrDefault(x => x.Index == index);
                if (control != null)
                    GenGridCustomControlColumn(context, control);

                GenGridColumn(context, vprop);

                index++;
            }
        }

        void GenGridStyle(WebViewGenContext context)
        {
            var props = context.View.Props.Where(x => x.HideModes != MojViewMode.All).ToArray();

            O();

            XB("<style>");
            var pos = 0;
            foreach (var vprop in props)
            {
                pos++;
                vprop.VisiblePosition = pos;
                if (vprop.FontWeight == MojFontWeight.Bold)
                {
                    OB("{0}", GetGridColCssSelector(context, vprop));
                    O("font-weight: bold;");
                    End();
                }
            }
            XE("</Style>");
            /*
             <style>
# grid-393a3bc1-2e06-4516-8d29-220dde0668fe tr[role='row'] td[role='gridcell']:nth-child(5) {
        font-weight: bold;
    }
</style>
            */
        }

        string GetGridColCssSelector(WebViewGenContext context, MojViewProp vprop)
        {
            return $"#{context.ComponentId} tr[role='row'] > td[role='gridcell']:nth-child({vprop.VisiblePosition})";
        }

        void GenGridCustomControlColumn(WebViewGenContext context, MojViewCustomControl control)
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
                        O($"headerTemplate: \"<input id='cb-all-view-{view.Id}' class='k-checkbox all-list-items-selector' type='checkbox' /><label class='k-checkbox-label' for='cb-all-view-{view.Id}' />\",");
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

        void GenGridColumn(WebViewGenContext context, MojViewProp vprop)
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
                    // KABU TODO: ELIMINATE: Razor helpers are not allowed in component JS anymore.
                    throw new MojenException("Razor helpers are not allowed in component JS anymore.");
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

                    // KABU TODO: REMOVE: Now we always want to use OData even for small sized lists.
                    if (false && vinfo.TargetType.DataSetSize == MojDataSetSizeKind.ExtraSmall)
                    {
                        // KABU TODO: ELIMINATE: Razor helpers are not allowed in component JS anymore.
                        throw new MojenException("Razor helpers are not allowed in component JS anymore.");

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
    }
}