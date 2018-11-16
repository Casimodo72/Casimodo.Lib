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
        //public string GridScriptVirtualFilePath { get; set; }

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
                KendoGen.BindCustomTagsEditorView(view);

                Options = view.GetGeneratorConfig<KendoGridOptions>() ?? new KendoGridOptions();
                TransportConfig = this.CreateODataTransport(view,
                    customQueryBase: Options.CustomQueryBase,
                    customQueryMethod: Options.CustomQueryMethod);

                var context = new WebViewGenContext
                {
                    View = view,
                    UINamespace = GetJsScriptUINamespace(view)
                };
                KendoGen.InitComponentNames(context);
                context.ComponentId = "grid-" + view.Id;

                // Edit capabilities
                CanModify = view.CanModify && !view.Kind.Roles.HasFlag(MojViewRole.Lookup) && view.EditorView?.CanModify == true;
                CanCreate = CanModify && view.EditorView != null && view.EditorView.CanCreate && Options.IsCreatable;
                CanDelete = CanModify && Options.IsDeletable == true || (view.EditorView != null && view.EditorView.CanDelete && (Options.IsDeletable ?? true));

                GridScriptFilePath = BuildTsScriptFilePath(view, suffix: ".vm.generated");
                //GridScriptVirtualFilePath = BuildJsScriptVirtualFilePath(view, suffix: ".vm.generated");

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
            if (!context.View.IsCustomView)
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

            if (context.View.HasListItemContextMenu)
            {
                O();
                // KABU TODO: Currently just a hack.
                // Add a row context menu in order to edit the tags of the selected data item.
                O("<ul id='row-context-menu-{0}' style='text-wrap:none;min-width:150px;display:none'>",
                    context.ComponentId);
                Push();

                foreach (var cmd in context.View.ListItemCommands)
                {
                    O($"<li data-name='{cmd.Name}'>{cmd.DisplayName}</li>");
                }

                Pop();
                O("</ul>");
            }

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

            OTsNamespace(WebConfig.ScriptUINamespace, (nscontext) =>
            {
                KendoGen.OBeginComponentViewModelFactory(context);
                OB("return new kmodo.GridComponent(");
                KendoGen.OViewModelOptions(context, isList: true,
                    extend: () =>
                    {
                        O($"baseFilters: {KendoGen.BuildBaseFiltersArrayLiteral(context)},");

                        // OData read query URL
                        O($"readQuery: {MojenUtils.ToJsValue(TransportConfig.ODataSelectUrl)},");

                        OB("dataSourceOptions: (e) =>");
                        OB("return");
                        KendoGen.ODataSourceListOptions(context,
                            TransportConfig,
                            create: false,
                            modify: false,
                            delete: false,
                            pageSize: Options.PageSize,
                            isServerPaging: Options.IsServerPaging,
                            initialSortProps: context.View.Props
                                .Where(x => x.InitialSort.Is)
                                .OrderBy(x => x.InitialSort.Index)
                                .ToArray());
                        End(";");
                        End(",");

                        OB("dataModel: (e) =>");
                        OB("return");
                        KendoGen.ODataSourceModelOptions(context, TransportConfig.ModelProps);
                        End(";");
                        End(",");

                        OB("gridOptions: (e) =>");
                        OB("return");
                        GenGridOptions(context);
                        End(";");
                        End(",");
                    });
                End(").init();");

                //O();
                //O("return vm;");

                KendoGen.OEndComponentViewModelFactory(context);
            });
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
                // NOTE: Currently we support "multiple" or "row" only:
                O($"selectable: {(view.ItemSelection.IsMultiselect ? "'multiple'" : "'row'")},");
                // Kendo options:
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
                                o("<div class='km-grid-toolbar-content'>");

                                o("<div class='km-grid-tools-row'>");

                                o("<div class='km-grid-tools'>");

                                if (view.IsCompanyFilterEnabled)
                                    o("<div class='km-grid-tool-filter'><span class='icon-company'></span><div class='km-grid-company-filter-selector'></div></div>");

                                if (view.IsTagsFilterEnabled)
                                    //o("<div class='km-grid-tool-filter'><span class='icon-tag'></span><select class='km-grid-tags-filter-selector'></select></div>");
                                    o("<div class='km-grid-tool-filter'><span class='icon-tag'></span><div class='km-grid-tags-filter-selector'></div></div>");

                                if (view.IsNavigatableTo)
                                    o("<button class='k-button kmodo-clear-guid-filter-command' style='display:none'></button>");

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

                                o("</div>"); // tools left                              

                                o("<div class='km-grid-tools-right'>");

                                foreach (var command in context.View.CustomCommands)
                                {
                                    o($"<button type='button' class='k-button btn custom-command' data-command-name='{command.Name}'>{command.DisplayName}</button>");
                                }

                                // Add grid data export context menu.
                                if (context.View.IsExportableToPdf || context.View.IsExportableToExcel)
                                {
                                    o("<ul class='km-grid-tools-menu'>");
                                    o("<li><span></span><ul>");
                                    //  style='text-wrap:none;min-width:150px;display:none'

                                    if (context.View.IsExportableToExcel)
                                        // TODO: LOCALIZE
                                        o("<li data-name='ExportToExcel'>Exportieren nach Excel</li>");

                                    if (context.View.IsExportableToPdf)
                                        // TODO: LOCALIZE
                                        o("<li data-name='ExportToPdf'>Exportieren nach PDF</li>");
                                    
                                    o("</ul></li>");
                                    o("</ul>");
                                }


                                if (CanCreate)
                                {
                                    // Add a create (+) button.
                                    // NOTE: We are using a custom "create" button (k-grid-custom-add) instead of
                                    //   kendo grid's default button (k-grid-add).
                                    o("<a class='k-button k-grid-custom-add hide' style='margin-right:24px;margin-left:24px' href='#'><span class='k-icon k-add'></span></a>");
                                }

                                // Add refresh button
                                if (view.IsReloadable)
                                    o("<a class='k-button k-grid-refresh' href='#'><span class='k-icon k-i-refresh'></span></a>");

                                o("</div>"); // tools-right

                                o("</div>"); // tools-row
                                o("</div>"); // toolbar-content
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
            O("dataSource: e.sender.createDataSource(),");

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
                        O($"headerTemplate: kmodo.templates.get('AllRowsCheckBoxSelectorGridCell'),");
                    }

                    O("attributes: { 'class': 'list-item-selector' },");
                    O($"template: kmodo.templates.get('RowCheckBoxSelectorGridCell'),");

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
                // KABU TODO: IMPORTANT: Currently disabled.
                var path = vprop.GetFormedPath();
                var firstToManyPos = path.Steps.FindIndex(x => x.SourceProp.Reference.IsToMany);

                if (firstToManyPos != -1)
                {
                    if (string.IsNullOrEmpty(vprop.CustomTemplateName))
                        throw new MojenException("If no custom template is defined for a grid column, " +
                            "then the whole formed property path must have a multiplicity of one.");

                    // Build new effective property path:
                    //    The path must stop at the first reference with a multiplicity of "x-to-many".
                    var props = new List<string>();

                    foreach (var step in path.Steps)
                    {
                        props.Add(step.SourceProp.Name);

                        if (step.SourceProp.Reference.IsToMany)
                            break;

                        if (step.TargetProp != null)
                            props.Add(step.TargetProp.Name);
                    }

                    propPath = props.Join(".");
                }
                //foreach (var step in path.Steps)
                //    if (!step.SourceProp.Reference.IsToOne)
                //    {
                //        throw new MojenException("The whole path must have a multiplicity of one.");
                //    }
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
                valueTemplate = $"cmodo.toDisplayBool(data.get('{propPath}'))";
            }
            else if (vpropType.IsTimeSpan)
            {
                valueTemplate = $"cmodo.toDisplayTimeSpan(data.get('{propPath}'))";
            }

            // HTML template
            if (dprop.IsColor || dprop.IsColorWithOpacity)
            {
                // Color cell Kendo template.
                O($"width: 33,");

                template = $"#=kmodo.getColorCellTemplate(data.get('{propPath}'))#";
            }
            // Image file reference
            else if (dprop.FileRef.Is && dprop.FileRef.IsImage)
            {
                O($"width: 70,");

                template = $"#=kmodo.getShowPhotoCellTemplate(data.get('{propAliasPath}Uri))#";
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
            if (!vprop.IsSortable) // TODO: REMOVE: && !vprop.IsReferenced)
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

                string filterCellTemplate = null;
                Action filterCellTemplateBuild = null;

                // Specifies whether to show or hide the DropDownList with the operators.
                bool filterShowOperators = false;

                string filterOperator = null;

                string filterSataSourceUrl = null;

                string filterDataSourceData = null;

                // Specifies the AutoComplete filter option. Possible values are same as the one 
                // for the AutoComplete filter option- "startswidht", "endswith", "contains".
                string filterDataSourceAutoCompleteOperator = null;

                string filterDataSourceTextField = null;

                // Use this options to enable the MultiCheck filtering support for that column.
                // WARNING: If you have enabled the columns.multi option and your Grid uses serverPaging 
                // (or ServerOperations(true) if using the MVC wrappers) you need to provide columns.filterable.dataSource. 
                // If columns.filterable.dataSource is not provided: bad performance.
                bool filterMulti = false;

                if (vprop.Type.IsEnum)
                {
                    // KABU TODO: ELIMINATE: Razor helpers are not allowed in component JS anymore.
                    throw new MojenException("Razor helpers are not allowed in component JS anymore.");
                    // Enums will be looked up based on static values.
                    filterDataSourceData = $"@(PickItemsHelper.ToJsArray<{vprop.Type.NameNormalized}>(names: true))";

                    // DropDownList template
                    var nullable = vprop.Type.CanBeNull;
                    filterCellTemplate = $"kmodo.gridEnumFilterColTemplate{(nullable ? "Nullable" : "")}";

                    filterDataSourceTextField = "text";
                }
                else if (vprop.Reference.Is)
                {
                    // Reference

                    if (vinfo.TargetType.DataSetSize == MojDataSetSizeKind.ExtraSmall)
                    {
                        // Kendo DropDownList
                        filterDataSourceTextField = dprop.Name;
                        var nullable = vprop.Reference.IsToZero;
                        filterCellTemplateBuild = new Action(() =>
                        {
                            ob("function (args)");
                            O("kmodo.gridReferenceFilterColTemplate(args, '{0}', '{1}', {2});",
                                filterDataSourceTextField,
                                filterDataSourceTextField,
                                MojenUtils.ToJsValue(nullable));
                            End(",");
                        });

                        filterSataSourceUrl = string.Format("{0}/{1}?$select={2}&$orderby={2}",
                                this.GetODataPath(vinfo.TargetType),
                                this.GetODataQueryFunc(false),
                                dprop.Name);
                    }
                    else
                    {
                        if (!dprop.Type.IsString)
                            throw new MojenException("AutoComplete column filters are intended for strings only.");

                        filterDataSourceTextField = dprop.Name;
                        filterOperator = "contains";
                        filterDataSourceAutoCompleteOperator = "contains";

                        if (vprop.IsLookupDistinct)
                        {
                            filterSataSourceUrl = GetDistinctDataSourceReadUrl(context, vinfo.TargetType, dprop.Name);
                        }
                        else
                        {
                            filterSataSourceUrl = string.Format("{0}/{1}?$select={2}&$orderby={2}",
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
                        filterOperator = "contains";
                    }

                    if (vprop.IsLookupDistinct)
                    {
                        filterSataSourceUrl = GetDistinctDataSourceReadUrl(context, view.TypeConfig, dprop.Name);
                    }
                    else
                    {
                        // Example "odata/Contracts/Ga.Query()?$select=Number"
                        filterSataSourceUrl = TransportConfig.ODataFilterUrl + vprop.Name;
                    }
                }

                if (filterCellTemplate != null)
                    O($"template: {filterCellTemplate},");
                else if (filterCellTemplateBuild != null)
                {
                    Oo($"template: ");
                    filterCellTemplateBuild();
                }

                if (!filterShowOperators)
                    O($"showOperators: false,");

                if (filterOperator != null)
                    O($"operator: '{filterOperator}',");

                if (filterMulti)
                    O($"multi: true,");

                if (filterDataSourceAutoCompleteOperator != null)
                    O($"suggestionOperator: '{filterDataSourceAutoCompleteOperator}',");

                if (filterSataSourceUrl != null || filterDataSourceData != null)
                {
                    // OData data source
                    OB("dataSource:");

                    if (filterSataSourceUrl != null)
                    {
                        O($"type: \"odata-v4\",");
                        O($"transport: {{ read: {{ url: \"{filterSataSourceUrl}\" }} }}");

                        // KABU TODO: REVISIT: May want to enable serverFiltering, so that
                        //   the values are always re-fetched from server.
                        //serverFiltering: true, 
                    }

                    if (filterDataSourceData != null)
                    {
                        O($"data: {filterDataSourceData},");
                    }

                    End(","); // DataSource

                    if (filterDataSourceTextField != null)
                        O($"dataTextField: '{filterDataSourceTextField}',");
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
                var templateDataPropPath = "dataItem";
                if (vprop.CustomTemplatePropPath != null)
                    templateDataPropPath += "." + vprop.CustomTemplatePropPath;

                template = $"function(dataItem) {{ return kmodo.templates.get('{vprop.CustomTemplateName}')({templateDataPropPath}); }}";
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

        string GetDistinctDataSourceReadUrl(WebViewGenContext context, MojType type, string propName)
        {
            // KABU TODO: VERY IMPORTANT: FIX: If a custom read method was configured,
            //   then this will incorrectly use the default QueryDistinct read method.

            // Kendo's filterCell will use the view property for removing duplicates,
            //   which does not work with our foreign data, so we need
            //   to provide distinct values server-side.
            return string.Format("{0}/{1}(On='{2}')?$select={2}&$orderby={2}",
                this.GetODataPath(type),
                this.GetODataQueryFunc(distinct: true, appendCall: false),
                propName);
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