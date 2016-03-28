using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{    
    public partial class KendoWebMvcGridViewGen : KendoWebGridGenBase
    {
        public override void GenerateGridView(WebViewGenContext context)
        {
            ValidateView(context);

            // REMEMBER: Always escape "#" with "\#" in Kendo templates.

            // http://docs.telerik.com/kendo-ui/web/grid/appearance

            MojViewConfig view = context.View;
            MojType type = view.TypeConfig;
            string jsTypeName = FirstCharToLower(type.Name);
            string keyPropName = type.Key.Name;

            DataSource = InitDataSource(context);

            InitEvents(context);

            InitialSortProps = type.GetProps(custom: true)
                .Where(x => x.InitialSort != null)
                .OrderBy(x => x.InitialSort.Index)
                .ToArray();

            ORazorUsing("Casimodo.Lib", "Casimodo.Lib.Web",
                App.GetDataContext(view.TypeConfig.DataContextName).DataNamespace);

            O($"@{{ ViewBag.Title = \"{view.Title}\"; }}");

            // IMPORTANT NOTE: The JS view model must be positioned before the grid,
            //   otherwise the JavaScript event handlers will not be found when
            //   injecting views via ajax.
            GenerateJSViewModel(context);

            // Add a dummy view model container.
            O();
            OLookupDialogToolbar(context);
            //O($"<div class='casimodo-lookup-view-model-container {context.ComponentViewModelContainerElemClass}'/>");

            // Gen: @(Html.Kendo().Grid<Person>()
            O();
            O($"@(Html.Kendo().Grid<{view.TypeConfig.ClassName}>()");

            Push();

            O($".Name(\"{context.ComponentId}\")");

            if (Options.IsDeferred)
            {
                // Workaround: MVVM bindings go terribly wrong when an other view model
                // overrides the grid's own view model.
                // See http://www.telerik.com/forums/grid-filter-row-weird-behaviour
                O(".Deferred()");
            }

            // Sorting
            O(".Sortable(sortable => sortable.AllowUnsort(true).SortMode(GridSortMode.MultipleColumn))");

            // Paging
            O(".Pageable(x => x.Refresh(true).Input(true).PageSizes(true))");

            NonUsedGridOptions();

            // Grid events ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            GenerateGridEvents(context);

            // Row selection ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (view.ItemSelection.IsEnabled)
            {
                O(".Selectable(selectable => selectable.Mode(GridSelectionMode.{0}))",
                    (view.ItemSelection.IsMultiselect ? "Multiple" : "Single"));
            }

            // Expandable details template ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (InlineDetailsView != null)
            {
                O($".ClientDetailTemplateId(\"{InlineDetailsTemplateName}\")");
            }

            // Editor template ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (CanEdit)
            {
                O(".Editable(edit => edit");
                Push();
                // Use pop up editor.
                O(".Mode(GridEditMode.PopUp)");
                // Use pop up editor template.
                // Note: the editor's HTML sits in its own partial view file.
                O($".TemplateName(\"{EditorTemplateName}\").Window(w => w.Title(\"Bearbeiten\").Width({EditorView.Width}))");
                Pop();
                O(")");
            }

            // Tool bar ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (HasToolbar)
            {
                O(".ToolBar(toolbar => toolbar.Template(");
                Push();
                O("@\"<text><div class='toolbar'>\"");

                if (view.IsExportableToPdf)
                    O("+ \"<button class='k-button k-grid-pdf'><span class='k-icon k-i-pdf'></span></button>\"");

                if (view.IsExportableToExcel)
                    O("+ \"<button class='k-button k-grid-excel'><span class='k-icon k-i-excel'></span></button>\"");

                // Add refresh button
                O("+ \"<button class='k-button k-grid-refresh'><span class='k-icon k-i-refresh'></span></button>\"");

                if (CanCreate && CanEdit)
                {
                    // Add a create (+) button.
                    O("+ \"<a class='k-button k-grid-add' href='#'><span class='k-icon k-add'></span></a>\"");
                }

                // KABU TODO: REVISIT: Currently we don't use custom auto-complete filters anymore.
                // Auto complete filters.
                foreach (var filter in AutoCompleteFilters)
                {
                    // Use a template (partial view) for the auto-complete filters.
                    O("+ Html.Partial(\"{0}\", Html.LookupInfo<{1}>(prop: \"{2}\", route: \"{3}\"))",
                        AutoCompletePartialViewName,
                        view.TypeConfig.ClassName,
                        filter.PropName,
                        view.TypeConfig.PluralName);
                }

                O("+ @\"</div></text>\")");

                Pop();
                O(")");
            }

            // Export ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (view.IsExportableToPdf)
            {
                O(".Pdf(pdf => pdf");
                Push();
                O(".FileName(\"{0}.pdf\")", view.TypeConfig.DisplayPluralName);
                Pop();
                O(")");
            }

            if (view.IsExportableToExcel)
            {
                O(".Excel(excel => excel");
                Push();
                O(".FileName(\"{0}.xlsx\")", view.TypeConfig.DisplayPluralName);
                Pop();
                O(")");
            }

            O(".ColumnMenu()");

            // Show filters in column headers
            O(".Filterable(filter => filter.Mode(GridFilterMode.Row))");

            // Columns ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            O(".Columns(c => {");
            Push();
            foreach (var vprop in view.Props)
            {
                GeneratePropColumn(vprop);
            }

            // Edit / delete button column ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (CanEdit || CanDelete)
            {
                // Kendo template for buttons.
                Oo("c.Template(t => { }).ClientTemplate(@\"");
                Push();

                // Avoid wrapping multiple buttons.
                o("<div style='white-space:nowrap'>");

                if (CanEdit)
                    // Add an edit (/) button.
                    o(@"<a class='k-button k-grid-edit' href='\#'><span class='k-icon k-edit'></span></a>");

                // KABU TODO: CLARIFY whether we want a single
                //   central delete button in the grid's header instead.
                if (CanDelete)
                    // Add a delete (x) button.
                    o(@"<a class='k-button k-grid-delete' href='\#'><span class='k-icon k-delete'></span></a>");

                o("</div>");

                Pop();
                oO("\");"); // End of template
            }

            Pop(); // Columns
            O("})");

            // If lookup view: don't auto bind.
            if (View.Kind.Roles.HasFlag(MojViewRole.Lookup))
            {
                O(".AutoBind(false)");
            }

            // Kendo data source ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            GenerateDataSource(context);

            Pop();
            O(")"); // Grid                        

            // JavaScript
            GenerateJSDocumentReady(context);

            // Details view Kendo template ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (InlineDetailsView != null)
            {
                O();
                OKendoTemplateBegin($"{InlineDetailsTemplateName}");
                O("@Html.Partial(\"_Details\").ToKendoTemplate()");
                OKendoTemplateEnd();
            }
        }

        void GeneratePropColumn(MojViewProp vprop)
        {
            var info = vprop.BuildViewPropInfo(lookupable: true);
            //var prop = info.Prop;
            var propPath = info.PropPath;
            var propAliasPath = info.PropAliasPath;

            var iscolor = vprop.IsColor || vprop.IsColorWithOpacity;
            if (iscolor)
            {
                // Color cell Kendo template.
                O($"c.Template(t => {{ }}).Width(33).ClientTemplate(@\"#= kendomodo.getColorCellTemplate({propPath})#\");");
                return;
            }

            // Image file reference
            if (vprop.FileRef.Is && vprop.FileRef.IsImage)
            {
                O($"c.Template(t => {{ }}).Width(70).ClientTemplate(@\"#= kendomodo.getShowPhotoCellTemplate({propAliasPath}Uri)#\");");
                return;
            }
            // KABU TODO: What about other file references?

            // Foreign key prop / navigation prop.
            if (vprop.Reference.Cardinality.HasFlag(MojCardinality.One) && vprop.FormedNavigationTo.Is)
            {
                // Navigation property to entity (or model).

                // The PickItemsContainer is a container for queryable entity (or model) repositories.
                var repository = info.TargetType.PluralName;
                var targetProp = info.TargetProp.Name;
                var nullable = (vprop.Reference.ForeignKey.Type.IsNullableValueType ? "true" : "false");
                Oo($"c.ForeignKey(p => p.{propPath}, PickItemsContainer.Get{repository}(\"{targetProp}\", nullable: {nullable}), \"Value\", \"Text\")");

                //Oo("c.ForeignKey(p => p.{0}, PickItemsContainer.Get{1}(\"{2}\", nullable: {3}), \"Value\", \"Text\")",
                //    prop.Reference.ForeignKey.Name,
                //    prop.Reference.ToType.PluralName,
                //    prop.FormedNavigationTo.TargetProp.Name,
                //    (prop.Reference.ForeignKey.Type.IsNullableValueType ? "true" : "false"));                
            }
            else if (vprop.Reference.Is)
            {
                throw new MojenException("This reference kind is not supported.");

                // Foreign key to entity (or model).
                //if (vprop.Reference.ToType.FindPick() == null)
                //    throw new MojenException($"The target type '{vprop.Reference.ToType.ClassName}' does not define pick properties.");
                //Oo("c.ForeignKey(p => p.{0}, PickItemsContainer.Get{1}(nullable: {2}), \"Value\", \"Text\")",
                //    vprop.Name,
                //    vprop.Reference.ToType.PluralName,
                //    (vprop.Type.IsNullableValueType ? "true" : "false"));
            }
            else if (vprop.Type.IsEnum)
            {
                // Kendo specific: We need to define enums as foreign keys of type string.
                Oo("c.ForeignKey(p => p.{0}, PickItemsHelper.ToSelectList<{1}>(nullable: {2}, names: true))",
                    vprop.Name,
                    vprop.Type.NameNormalized,
                    (vprop.Type.IsNullableValueType ? "true" : "false"));
            }
            else
            {
                Oo("c.Bound(m => m.{0})", vprop.Name);
            }

            // Date time formatting.
            if (MojenUtils.IsDateTimeOrOffset(vprop.Type.TypeNormalized))
            {
                o(".Format(Html.GetDateTimePattern(placeholder: true{0}{1}{2}))",
                    (vprop.Type.DateTimeInfo.IsDate == false ? ", date: false" : ""),
                    (vprop.Type.DateTimeInfo.IsTime == false ? ", time: false" : ""),
                    (vprop.Type.DateTimeInfo.DisplayMillisecondDigits > 0 ? ", ms: " + vprop.Type.DateTimeInfo.DisplayMillisecondDigits : ""));
            }

            // Check-box Kendo template.
            if (vprop.Type.IsBoolean)
            {
                o($".ClientTemplate(@\"#= casimodo.toDisplayBool({vprop.Name})#\")");
                //o($".ClientTemplate(@\"<input type='checkbox' disabled #= {prop.Name} ? checked='checked' : '' # />\")");
            }

            if (!vprop.IsSortable)
                o(".Sortable(false)");

            if (!vprop.IsGroupable)
                o(".Groupable(false)");

            // Filterable
            o(".Filterable(");
            if (vprop.IsFilterable)
            {
                if (vprop.Reference.Is)
                {
                    o("true");
                }
                else
                {
                    o("filter => filter");

                    // Filter grid cell
                    o(".Cell(cell => cell");
                    if (vprop.Type.IsString)
                    {
                        // Use "contains" as default operator for strings.
                        o(".Operator(\"contains\")");
                    }

                    // KABU TODO: Move into a data-source generator method.
                    // OData data source
                    o(".DataSource(ds => ds");
                    o(".Custom()");
                    o(".Type(\"odata-v4\")");
                    o($".Transport(transport => transport.Read(read => read.Url(\"{DataSource.ODataFilterUrl}{vprop.Name}\")))");
                    o(")"); // DataSource

                    o(")"); // Cell
                }
            }
            else
            {
                o("false");
            }
            o(")"); // Filterable

            // KABU TODO: Move logic into MojViewProp
            if (vprop.Width != null && !iscolor && !vprop.FileRef.Is)
            {
                o(".Width({0})", vprop.Width.Value);
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

            oO(";");
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

        void NonUsedGridOptions()
        {
            // Options not used (yet?):

            // .ClientRowTemplate(clientRowTemplate)
            // See http://stackoverflow.com/questions/15835384/kendo-ui-grid-mvc-looping-collection-of-entity-view-model-on-clientrowtemp
            // Example:            
            // @{
            //    Func<Grid<MyModel>, string> clientRowTemplate = @<div class="order-info">
            //        <div class="order-info-items cell">
            //            #if (data.OrderItemList) { #
            //                # for (var i in data.OrderItemList) { #
            //                    #if (data.OrderItemList[i].ID) { #
            //                        <img src="#= data.OrderItemList[i].ImageUrl #" alt="#= data.OrderItemList[i].ItemName #" width="100" height="100" />
            //                    # } #
            //                # } #
            //            # } #
            //        </div>
            //    </div>.ToString();
            // }

            // .Filterable() //x => x.Mode(GridFilterMode.Row))

            // .Selectable(x => x.Mode(GridSelectionMode.Single))

            // .Scrollable()

            // .Events(ev => ev.Save("save"))

            // .Filterable(filter =>
            // {
            //     filter.Operators(op => op.ForDate(x => x.IsGreaterThan(">=")));
            // })

            // .RowAction(row =>
            // I think RowAction can only be used together with server data binding.            
            // Example:
            // .RowAction(row =>
            // {
            //    if (row.Index == 0)
            //    {
            //        row.DetailRow.Expanded = true;
            //    }
            //    else
            //    {
            //        var requestKeys = Request.QueryString.Keys.Cast<string>();
            //        var expanded = requestKeys.Any(key => key.StartsWith("Orders_" + row.DataItem.EmployeeID) ||
            //            key.StartsWith("OrderDetails_" + row.DataItem.EmployeeID));
            //        row.DetailRow.Expanded = expanded;
            //    }
            // })
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