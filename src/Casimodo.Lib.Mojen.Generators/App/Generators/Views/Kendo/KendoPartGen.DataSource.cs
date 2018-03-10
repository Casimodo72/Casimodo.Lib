using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public class KendoDataSourceConfig
    {
        public MojType TypeConfig { get; set; }
        public string TransportType { get; set; } = "odata-v4";
        /// <summary>
        /// Indicates whether OData action (POST) methods shall be used, rather than the default (PUT) methods.
        /// </summary>
        public bool UseODataActions { get; set; }
        public MojHttpRequestConfig TransportConfig { get; set; }
        public MojViewProp[] InitialSortProps { get; set; }
        public string DataModelFactory { get; set; }
        public string ReadQueryFactory { get; set; }
        public bool CanCreate { get; set; }
        public bool CanModify { get; set; }
        public bool CanDelete { get; set; }
        public int PageSize { get; set; }
        public bool IsServerPaging { get; set; } = true;
    }

    public partial class KendoPartGen : WebPartGenerator
    {
        public void OPropValueFactory(string prop, object value)
        {
            var func = "create" + prop.FirstLetterToUpper();
            OB($"fn.{func} = function ()");
            O($"return this.{prop} || (this.{prop} = {MojenUtils.ToJsValue(value)});");
            End(";");
        }

        public void OOptionsFactory(string prop, Action options)
        {
            var func = "create" + prop.FirstLetterToUpper();
            OB($"fn.{func} = function ()");

            OB($"return this.{prop} || (this.{prop} =");
            options();
            End(");");

            End(";");
        }

        public void ODataSourceOptionsFactory(WebViewGenContext context, Action options)
        {
            OOptionsFactory("dataSourceOptions", options);
        }

        public void ODataSourceTransportOptions(WebViewGenContext context, KendoDataSourceConfig config)
        {
            // Fixup filter parameters
            // http://www.telerik.com/forums/guids-in-filters
            var mode = config.UseODataActions ? "'Action'" : "null";
            O($"parameterMap: function (data, type) {{ return kendomodo.parameterMapForOData(data, type, {mode}); }},");

            // Read
            var readUrl = config.ReadQueryFactory ?? $"'{config.TransportConfig.ODataSelectUrl}'";
            O($"read: {{ url: {readUrl} }},");

            // Create
            if (config.CanCreate)
            {
                O($"create: {{ url: '{config.TransportConfig.ODataCreateUrl}' }},");
            }

            // Update
            if (config.CanModify)
            {
                var url = string.Format(config.TransportConfig.ODataUpdateUrlTemplate, $"' + data.{config.TypeConfig.Key.Name} + '");
                var verb = config.UseODataActions ? "type: 'POST', " : "";

                O($"update: {{ {verb}url: function (data) {{ return '{url}'; }} }},");
            }

            // Delete
            if (config.CanDelete)
                O($"destroy: {{ url: function (data) {{ return '{config.TransportConfig.ODataDeleteUrl}(' + data.{config.TypeConfig.Key.Name} + ')'; }} }},");
        }

        public void ODataSourceOptions(WebViewGenContext context, KendoDataSourceConfig config)
        {
            var transport = config.TransportConfig;

            O($"type: '{config.TransportType}',");

            ODataSourceOrderBy(config);

            // Data schema
            OB("schema:");

            // Data model            
            O($"model: {config.DataModelFactory}");

            End(","); // Schema

            // Data transport
            OB("transport:");
            ODataSourceTransportOptions(context, config);
            End(","); // Transport

            O($"pageSize: {config.PageSize},");
            O($"serverPaging: {MojenUtils.ToJsValue(config.IsServerPaging)}, ");
            O("serverSorting: true,");
            O("serverFiltering: true,");
        }

        void ODataSourceOrderBy(KendoDataSourceConfig config)
        {
            // Apply initial sort.
            var props = config.InitialSortProps;
            if (props == null || !props.Any())
                return;

            // KABU TODO: Check output

            OXArr("sort", () =>
            {
                int i = 0;
                foreach (var prop in props)
                {
                    // Gen: "{ field: 'UserName', dir: 'asc' },"
                    Oo($"{{ field: '{prop.FormedTargetPath}', dir: '{prop.InitialSort.Direction.ToJs()}' }}");
                    if (++i < props.Length)
                        oO(",");
                    else
                        Br();
                }
            });
        }

        bool HasValidationConstraints(MojProp prop)
        {
            return prop.IsRequiredOnEdit || prop.Rules.Is;
        }

        public void ODataSourceModelOptions(MojProp[] props)
        {
            var key = props.FirstOrDefault(x => x.IsKey);
            if (key != null)
                O($"id: '{key.Name}',");

            OB("fields:");

            ODataSourceModelFieldsOptions(props);

            End(","); // Fields
        }

        void ODataSourceModelFieldsOptions(MojProp[] props) // WebViewGenContext context
        {
            // Available model options are: defaultValue, editable, nullable, parse,
            // type, from, validation.
            // See http://docs.telerik.com/kendo-ui/api/javascript/data/model#methods-Model.define            

            int i = 0;
            foreach (var prop in props)
            {
                StartBuffer();

                OB(prop.Name + ":");

                // Property type
                // Doc: {number | string | boolean | date}
                // Default is string
                string type = MojenUtils.ToJsType(prop.Type);
                if (type != "string")
                    O($"type: '{type}',");

                // Validation rules
                // Example: validation: { required: true, min: 1 }
                if (HasValidationConstraints(prop))
                {
                    var val = prop.Rules;
                    OXP("validation",
                        XP(prop.IsRequiredOnEdit, "required", true),
                        XP(prop.Type.IsNumber && val.Is && val.Min != null, "min", val.Min),
                        XP(prop.Type.IsNumber && val.Is && val.Max != null, "max", val.Max)
                    );
                }

                if (!prop.IsEditable)
                    O("editable: false,");

                // Default value
                if (prop.DefaultValues.Is)
                {
                    var @default = prop.DefaultValues.ForScenario("OnEdit", null).FirstOrDefault();
                    if (@default.Attr != null)
                    {
                        O("defaultValue: {0},", @default.Attr.Args.First().ToJsCodeString());
                    }
                    else if (@default.CommonValue != null)
                    {
                        if (@default.CommonValue == MojDefaultValueCommon.CurrentYear)
                        {
                            O("defaultValue: function(e) { return new Date().getFullYear() },");
                        }
                        else throw new MojenException($"Unhandled common default value kind '{@default.CommonValue}'.");
                    }
                    else if (@default.Value is string[])
                    {
                        // Multiline text.
                        string multilineText = (@default.Value as string[]).Join("\\n");
                        O($"defaultValue: \"{multilineText}\",");
                    }
                    else
                    {
                        O($"defaultValue: {MojenUtils.ToJsValue(@default.Value)},");
                    }
                }
                else if (prop.IsGuidKey && !prop.Type.CanBeNull)
                {
                    O("defaultValue: '{0}',", Guid.Empty);
                }
                else if (prop.Type.IsNumber && prop.Type.IsNullableValueType)
                {
                    // NOTE: This is a Kendo workaround, because otherwise Kendo sets nullable numbers to zero instead of NULL.
                    O("defaultValue: null,");
                }
                else if (!prop.Type.CanBeNull)
                {
                    throw new MojenException($"Property '{prop.Name}' cannot be null and has no default value defined.");
                }

                // End property
                if (++i < props.Length)
                    End(",");
                else
                    End();


                var text = BufferedText.CollapseWhitespace();
                EndBuffer();
                O(text);
            }
        }

        public void OMvcReadOnlyDataSource(string url, string parametersFunc)
        {
            // OData data source
            Oo(".DataSource(ds => ds");

            o(".Custom().Type(\"odata-v4\")");

            o(".ServerFiltering(true)");

            o(".Transport(transport => transport");

            // http://www.telerik.com/forums/guids-in-filters
            o(".ParameterMap(\"kendomodo.parameterMapForOData\")");

            o(".Read(read => read");

            o($".Url(\"{url}\")");
            if (!string.IsNullOrWhiteSpace(parametersFunc))
                o($".Data(\"{parametersFunc}\")");

            o(")"); // Read

            o(")"); // Transport

            oO(")"); // DataSource
        }


        public void oOKendoJsLookupDataSource(string url, string valueProp, string displayProp, bool async = true) //, string parametersFunc = null)
        {
            // OData data source
            oO($"kendomodo.oDataLookupValueAndDisplay('{url}', '{valueProp}', '{displayProp}', {MojenUtils.ToJsValue(async)});");
        }

        public void ODataFunction(string path, string func, string args, Action then)
        {
            O($"casimodo.oDataFunction(\"{path}\", \"{func}\", {args ?? "null"})");
            Push();
            OB(".then(function(value)");

            then();

            End(");");
            Pop();
        }

        public void ODataQuery(string query, string parameters, Action success)
        {
            OB($"kendomodo.query({query}, {parameters ?? "null"}, function(result)");

            success();

            End(");");
        }

        public void ODataQueryFirstOrDefault(string query, string parameters, Action success)
        {
            O($"kendomodo.odataQueryFirstOrDefault({query}, {parameters ?? "null"})");
            Push();
            OB(".then(function(result)");
            success();
            End(");");
            Pop();
        }
    }
}
