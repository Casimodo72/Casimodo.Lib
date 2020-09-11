using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoWebMvcGridViewGen
    {
        void GenerateDataSource(WebViewGenContext context)
        {
            O(".DataSource(ds => ds");
            Push();

            if (DataSourceType == "odata-v4")
                GenerateODataV4DataSource(context);
            else if (DataSourceType == "ajax")
                GenerateAjaxDataSource(context);

            Pop();
            O(")"); // DataSource
        }

        // KABU TODO: REVISIT: SignalR example: https://github.com/telerik/ui-for-aspnet-mvc-examples/blob/master/grid/signalR-bound-grid/KendoUIMVC5/Views/Home/Index.cshtml

        void GenerateODataV4DataSource(WebViewGenContext context)
        {
            var ds = DataSource;

            O($".Custom().Type(\"odata-v4\")");

            GenerateDataSourceInitialSort(InitialSortProps);

            GenerateDataSourceServerErrorHandler(DataSourceType);

            O(".Schema(schema => {");
            Push();

            // Data model
            O("schema.Model(x => {");
            Push();
            GenerateDataSourceModel(context, ds.ModelProps);
            Pop();
            O("});"); // Model

            Pop();
            O("})"); // Schema

            // Data transport
            O(".Transport(transport => {");
            Push();

            // Fixup filter parameters
            // http://www.telerik.com/forums/guids-in-filters
            O("transport.ParameterMap(\"kendomodo.fixODataV4FilterParameterMap\");");

            // Read                
            O($"transport.Read(read => read.Url(\"{ds.ODataReadUrl}\"));");

            // Create
            if (CanCreate && CanEdit)
            {
                O($"transport.Create(create => create.Url(\"{ds.ODataCrudUrl}\"));");
            }

            // Update
            if (CanEdit)
            {
                O("transport.Update(new {");
                Push();

                O("url = new Kendo.Mvc.ClientHandlerDescriptor {");
                // JS function that returns the ID of the given data item.                    
                // NOTE: There must be a line-break in the piece of JS code, otherwise the Kendo grid won't work - dunno why.
                O($"    HandlerName = @\"function(data) {{ return '/{ds.ODataCrudUrl}(' + data.{context.View.TypeConfig.Key.Name} + ')'; }}\" }}");

                Pop();
                O("});");
            }

            // Delete
            if (CanDelete)
            {
                O("transport.Destroy(new {");
                Push();

                O("url = new Kendo.Mvc.ClientHandlerDescriptor {");
                // JS function that returns the ID of the given data item.                    
                // NOTE: There must be a line-break in the piece of JS code, otherwise the Kendo grid won't work - dunno why.
                O($"    HandlerName = @\"function(data) {{ return '/{ds.ODataCrudUrl}(' + data.{context.View.TypeConfig.Key.Name} + ')'; }}\" }}");

                Pop();
                O("});");
            }

            Pop();
            O("})"); // Transport

            O($".PageSize({Options.PageSize})");
            O(".ServerPaging(true)");
            O(".ServerSorting(true)");
            O(".ServerFiltering(true)");
        }

        void GenerateAjaxDataSource(WebViewGenContext context)
        {
            var ds = DataSource;
            O(".Ajax()");

            GenerateDataSourceInitialSort(InitialSortProps);

            GenerateDataSourceServerErrorHandler(DataSourceType);

            // Data model
            O(".Model(x => {");
            Push();
            GenerateDataSourceModel(context, ds.ModelProps);
            Pop();
            O("})"); // Model

            O($".Read(read => read.Action(\"Get\", \"{ds.AjaxUrl}\"))");

            O($".Update(up => up.Action(\"Update\", \"{ds.AjaxUrl}\"))");

            O($".Create(create => create.Action(\"Create\", \"{ds.AjaxUrl}\"))");
        }

        void GenerateDataSourceInitialSort(IEnumerable<MojProp> initialSortProps)
        {
            // Apply initial sort.
            if (!initialSortProps.Any())
                return;

            O(".Sort(sort => {");
            Push();

            foreach (var prop in initialSortProps)
            {
                // Gen: "sort.Add(m => m.UserName).Ascending();"
                O($"sort.Add(m => m.{prop.Name}).{prop.InitialSort.Direction}();");
            }

            Pop();
            O("})");
        }

        void GenerateDataSourceModel(WebViewGenContext context, IEnumerable<MojProp> props)
        {
            var type = context.View.TypeConfig;

            foreach (var prop in props)
            {
                if (prop.IsKey)
                    O($"x.Id(m => m.{prop.Name});");

                Oo($"x.Field(\"{prop.Name}\", ");

                if (prop.Type.IsEnum)
                {
                    // WORKAROUND: Kendo doesn't understand OData v4 enums.
                    // So we explicitely need to make it believe it operates on a string.
                    o("typeof(string))");
                }
                else if (prop.Type.TypeNormalized == typeof(Guid)) // || prop.Reference.Cardinality.HasFlag(MojCardinality.One))
                {
                    // WORKAROUND: Kendo doesn't understand nullable GUIDs for foreign keys.
                    // So we explicitely need to make it believe it operates on a string.
                    o("typeof(string))");
                }
                else if (prop.Type.TypeNormalized == typeof(DateTimeOffset))
                {
                    // KABU TODO: IMPORTANT: WORKAROUND: Dunno which field format for DateTimeOffset I can use. 
                    // So for now I define DateTimeOffsets as DateTimes for Kendo.
                    string nullableMod = prop.Type.IsNullableValueType ? "?" : "";
                    o($"typeof(DateTime{nullableMod}))");
                }
                else
                {
                    o($"typeof({prop.Type.Name}))");
                }

                if (!prop.IsEditable)
                    o(".Editable(false)");

                var defaultValueArg = prop.Attrs.GetDefaultValueArg();
                if (defaultValueArg != null)
                {
                    o(".DefaultValue({0})", defaultValueArg.ToCodeString(parse: true));
                }
                else if (prop.IsGuidKey && prop.Type.TypeNormalized == typeof(Guid))
                {
                    o(".DefaultValue(Guid.Empty)");
                }
                else if (prop.Type.CanBeNull)
                {
                    o(".DefaultValue(null)");
                }

                oO(";");
            }
        }

        void GenerateDataSourceServerErrorHandler(string dskind)
        {
            // Data source events
            if (!CanEdit)
                return;

            if (dskind == "odata-v4")
            {
                O(".Events(events => events" +
                    // Displays server errors in the grid's pop up editor.
                    // Data source error handler: http://demos.telerik.com/aspnet-mvc/grid/editing-popup
                    ".Error(\"kendomodo.onServerErrorOData\")" +
                    // Reload and refresh the whole grid after an update was performed.
                    // We need this because otherwise computed properties won't be updated.
                    ".RequestEnd(\"kendomodo.onGridDataSourceRequestEndRefresh\"))");
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}