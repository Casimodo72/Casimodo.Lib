using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
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

        public override void Prepare()
        {
            base.Prepare();
        }

        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>()
                .Where(x => x.Uses(this) && !x.IsCustom))
            {
                if (view.IsCustom)
                    throw new MojenException("KendoGridView must not be custom.");

                Reset();

                KendoGen.BindEditorView<KendoFormEditorViewGen>(view);

                Options = view.GetGeneratorConfig<KendoGridOptions>() ?? new KendoGridOptions();
                TransportConfig = this.CreateODataTransport(view, null, Options.CustomQueryMethod);

                var context = new WebViewGenContext
                {
                    View = view,
                    UINamespace = GetJsScriptUINamespace(view)
                };
                KendoGen.InitComponentNames(context);
                context.ComponentId = "grid-" + view.Id;

                // Edit capabilities
                CanModify = !view.Kind.Roles.HasFlag(MojViewRole.Lookup) && view.EditorView != null && view.EditorView.CanModify;
                CanCreate = CanModify && view.EditorView != null && view.EditorView.CanCreate && Options.IsCreatable;
                CanDelete = CanModify && Options.IsDeletable == true || (view.EditorView != null && view.EditorView.CanDelete && (Options.IsDeletable ?? true));

                GridScriptFilePath = BuildJsScriptFilePath(view, suffix: ".vm.generated");
                GridScriptVirtualFilePath = BuildJsScriptVirtualFilePath(view, suffix: ".vm.generated");

                // Inline details              
                if (view.InlineDetailsView != null)
                {
                    InlineDetailsTemplateName = "grid-details-template-" + view.Id;
                    InlineDetailsViewVirtualFilePath = BuildVirtualFilePath(view.InlineDetailsView, name: "Details.Inline", partial: true);
                    InlineDetailsViewFilePath = BuildFilePath(view.InlineDetailsView, name: "Details.Inline", partial: true);
                }

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

            // View & style
            if (!context.View.IsViewless)
            {
                PerformWrite(context.View, () =>
                {
                    GenGridView(context);
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

            // KABU TODO: REMOVE
            //ORazorUsing("Casimodo.Lib", "Casimodo.Lib.Web",
            //    App.GetDataLayerConfig(context.View.TypeConfig.DataContextName).DataNamespace);

            O();
            if (context.View.IsLookup)
            {
                // Lookup view will have an ID assigned.
                if (string.IsNullOrWhiteSpace(context.View.Id)) throw new MojenException("The lookup view has no ID.");

                XB($"<div class='casimodo-dialog-toolbar' id='dialog-commands-{context.View.Id}'>");

                O("<button class='k-button cancel-button casimodo-dialog-button'>Abbrechen</button>");
                O("<button class='k-button ok-button casimodo-dialog-button'>OK</button>");

                XE("</div>");
            }

            // Container element for the Kendo grid widget.
            O($"<div id='{context.ComponentId}' class='component-root'></div>");

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
            var view = context.View;

            OUseStrict();

            KendoGen.OBeginComponentViewModelFactory(context);

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

                O();
                GenGridOptionsFactory(context);
            });

            // Create view model with options.
            O();
            OB("var vm = new ViewModel(");
            KendoGen.OViewModelOptions(context, isList: true);
            End(").init();");

            O();
            O("return vm;");

            KendoGen.OEndComponentViewModelFactory(context);
        }

        public void GenGridOptionsFactory(WebViewGenContext context)
        {
            // Grid options factory function.
            OB("fn.createComponentOptions = function()");

            // NOTE: We are using the internal data source factory function.
            OB("var options ="); GenGridOptions(context);
            End(";"); // Grid options object.
            O();
            // Apply override if applicable.
            O("if (this.createComponentOptionsOverride)");
            O("    options = this.createComponentOptionsOverride(options);");
            O();
            O("return options;");
            End(";"); // Grid options factory function.            
        }

        void GenGridOptions(WebViewGenContext context)
        {
            MojViewConfig view = context.View;

            O("editable: false,");

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

                                if (view.IsNavigatableTo)
                                    o("<button class='k-button kmodo-clear-guid-filter-command' style='display:none'>Navigation: Filter entfernen</button>");

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

            O("autoBind: false,");
            O("dataSource: this.createDataSource(),");

            // Columns ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~            
            O("columns: [");
            Push();

            GenGridColumns(context);

            // Edit button column.
            if (CanModify)
            {
                OB("");
                Oo("template: kendo.template(\"");

                KendoJsTemplate(() =>
                {
                    if (CanModify)
                    {
                        // Add an edit button.
                        // NOTE: This is a custom button which is not handled automatically
                        //   by the kendo.ui.Grid. The grid view model will attach
                        //   to this button and strat the edit operation manually.
                        o(@"<a class='k-button k-grid-custom-edit' href='#' style='display:none'><span class='k-icon k-edit'></span></a>");
                    }
                });

                oO("\")"); // End of template
                End();
            }

            Pop(); // Columns
            O("]");


        }

        void GenGridColumns(WebViewGenContext context)
        {
            var view = context.View;
            var index = 0;

            if (view.ItemSelection.UseCheckBox)
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

        bool HasColumnStyle(MojViewProp vprop)
        {
            return vprop.FontWeight != MojFontWeight.Normal;
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

            var view = context.View;
            var vinfo = vprop.BuildViewPropInfo(column: true);
            var propPath = vinfo.PropPath;
            var propAliasPath = vinfo.PropAliasPath;
            var dprop = vinfo.TargetDisplayProp;
            var vpropType = vinfo.TargetDisplayProp.Type;

            if (dprop.FileRef.Is && dprop.FileRef.IsImage)
                // KABU TODO: IMPL: Photos are currently disabled
                return;

            // Check references. Check the whole path for multiplicity of one.
            if (vprop.IsReferenced)
            {
                var path = vprop.GetFormedPath();
                foreach (var step in path.Steps)
                    if (!step.SourceProp.Reference.IsToOne)
                        throw new MojenException("The whole path must have a multiplicity of one.");
            }

            OB("");

            bool isEffectField = dprop.IsColor || dprop.FileRef.Is;

            O($"field: '{propPath}',");

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
                var label = vinfo.CustomDisplayLabel != null ? vinfo.CustomDisplayLabel : vprop.DisplayLabel;
                if (label == null)
                    throw new MojenException("No label found for grid column.");

                // Need to use " " if we don't want a column label, otherwise Kendo will
                // insist in displaying the field's name.
                if (string.IsNullOrWhiteSpace(label))
                    label = " ";

                O($"title: '{label}',");
            }

            // Format
            // KABU TODO: IMPORTANT: format decimals

            // Date time formatting.
            if (vpropType.IsAnyTime)
            {
                // KABU TODO: Should we use moment.js instead and use a template?

                // KABU TODO: LOCALIZE DateTime format.
                var format = "{0:";
                if (vpropType.DateTimeInfo.IsDate)
                    format += "dd.MM.yyyy ";
                if (vpropType.DateTimeInfo.IsTime)
                    format += "HH:mm:ss";
                // KABU TODO: Support milliseconds
                format += "}";

                O($"format: '{format}',");
            }

            if (vprop.IsHtml)
            {
                O($"encoded: false,");
            }

            // Column cell template
            // KABU TODO: Nest templates so that we also can have colored booleans, times, etc.

            string valueTemplate = null;
            string template = null;

            // Value template part.
            if (vpropType.IsBoolean)
            {
                valueTemplate = $"casimodo.toDisplayBool(data.get('{propPath}'))";
            }
            else if (vpropType.IsTimeSpan)
            {
                valueTemplate = $"casimodo.toDisplayTimeSpan(data.get('{propPath}'))";
            }

            // HTML template
            if (dprop.IsColor || dprop.IsColorWithOpacity)
            {
                // Color cell Kendo template.
                O($"width: 33,");

                template = $"#=kendomodo.getColorCellTemplate(data.get('{propPath}'))#";
            }
            // Image file reference
            else if (dprop.FileRef.Is && dprop.FileRef.IsImage)
            {
                O($"width: 70,");

                template = $"#=kendomodo.getShowPhotoCellTemplate(data.get('{propAliasPath}Uri))#";
            }
            else if (dprop.FileRef.Is)
            {
                // KABU TODO: What about other file references?
                throw new MojenException("This kind of file reference is not supported yet.");
            }
            else if (dprop.UseColor)
            {
                valueTemplate = valueTemplate ?? $"data.get('{propPath}' || '')";
                template = $"<div class='kmodo-cellcol'><div class='kmodo-cellmarker' style='background-color:#:data.get('{vprop.ColorProp.FormedTargetPath}')#'></div>#:data.get('{propPath}') || ''#</div>";
            }

            if (template == null && valueTemplate != null)
                template = $"#:{valueTemplate}#";

            if (template == null && vprop.IsReferenced)
                template = $"#: data.get('{propPath}') || '' #";

            if (vprop.IsLinkToInstance)
            {
                template = $"<span class='page-navi' " +
                    $"data-navi-part='{dprop.DeclaringType.Name}' " +
                    $"data-navi-id='#:data.get('{dprop.GetFormedNavigationPropPathOfKey()}')#'>" +
                    $"{template ?? $"#: data.get('{propPath}') || '' #"}" +
                    $"</span>";
            }

            if (template != null)
                template = Quote(template);

            // KABU TODO: IMPORTANT: Fix: Currently we hard-code all navigated-to properties to be sortable,
            //   because the view-property (which is a clone of the type's native property)
            //   is a reference property and is *not* sortable by default (set in MojClassPropBuilder).
            if (!vprop.IsSortable && !vprop.IsReferenced)
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
                Action cellTemplateBuild = null;

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

                    if (vinfo.TargetType.DataSetSize == MojDataSetSizeKind.ExtraSmall)
                    {
                        // Kendo DropDownList
                        dataSourceTextField = dprop.Name;
                        var nullable = vprop.Reference.IsToZero;
                        cellTemplateBuild = new Action(() =>
                        {
                            ob("function (args)");
                            O("kendomodo.gridReferenceFilterColTemplate(args, '{0}', '{1}', {2});",
                                dataSourceTextField,
                                dataSourceTextField,
                                MojenUtils.ToJsValue(nullable));
                            End(",");
                        });

                        dataSourceUrl = string.Format("{0}/{1}?$select={2}&$orderby={2}",
                                this.GetODataPath(vinfo.TargetType),
                                this.GetODataQueryFunc(false),
                                dprop.Name);
                    }
                    else
                    {
                        if (!dprop.Type.IsString)
                            throw new MojenException("AutoComplete column filters are intended for strings only.");

                        dataSourceTextField = dprop.Name;
                        @operator = "contains";
                        dataSourceAutoCompleteOperator = "contains";

                        if (vprop.IsReferenceLookupDistinct)
                        {
                            // Kendo's filterCell will use the view property for removing duplicates,
                            //   which does not work with our foreign data, so we need
                            //   to provide distinct values server-side.
                            dataSourceUrl = string.Format("{0}/{1}(On='{2}')?$select={2}&$orderby={2}",
                                this.GetODataPath(vinfo.TargetType),
                                this.GetODataQueryFunc(true, appendCall: false),
                                dprop.Name);
                        }
                        else
                        {
                            dataSourceUrl = string.Format("{0}/{1}?$select={2}&$orderby={2}",
                                this.GetODataPath(vinfo.TargetType),
                                this.GetODataQueryFunc(false),
                                dprop.Name);
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
                else if (cellTemplateBuild != null)
                {
                    Oo($"template: ");
                    cellTemplateBuild();
                }

                if (!showOperators)
                    O($"showOperators: false,");

                if (@operator != null)
                    O($"operator: '{@operator}',");

                if (multi)
                    O($"multi: true,");

                if (dataSourceAutoCompleteOperator != null)
                    O($"suggestionOperator: '{dataSourceAutoCompleteOperator}',");

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

            // See http://www.telerik.com/forums/client-template-%28hyper-link%29-on-foreign-key-column

            if (vprop.CustomTemplateName != null)
            {
                template = $"{App.Get<WebAppBuildConfig>().ScriptNamespace}.templates.getTemplate('{vprop.CustomTemplateName}')";
            }

            if (template != null)
                O($"template: {template},");

            if (HasPropAttributes(vprop, dprop))
            {
                string @class = "";
                if (vprop.FontWeight == MojFontWeight.Bold)
                    @class += " strong";

                // KABU TODO: MACIG hack
                if (dprop.Name == "ModifiedOn")
                    @class += " kmodo-grid-timestamp";

                OB("attributes:");

                if (!string.IsNullOrEmpty(@class))
                    O($"'class': '{@class}'");

                End(",");
            }

            End(","); // Column
        }

        bool HasPropAttributes(MojViewProp vprop, MojProp dprop)
        {
            return vprop.FontWeight == MojFontWeight.Bold ||
                // KABU TODO: MACIG hack
                dprop.Name == "ModifiedOn";
        }

        public string KendoDataGetOrEmpty(string path)
        {
            return $"data.get('{path}') || ''";
        }

        public string KendoDataGet(string path)
        {
            return $"data.get('{path}')";
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