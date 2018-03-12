using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoJsGridViewGen
    {
        void GenerateJSViewModelCore(WebViewGenContext context)
        {
            var view = context.View;

            KendoGen.OViewModelClass("ViewModel", extends: "kendomodo.ui.GridViewModel",
            constructor: () =>
            {
                O($"this.keyName = \"{context.View.TypeConfig.Key.Name}\";");
                if (HasViewModelExtension)
                    O($"this.extension = new {DataConfig.ScriptUINamespace}.{ViewModelExtensionClassName}({{ vm: this }});");
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
                        modify: CanEdit,
                        delete: CanDelete,
                        pageSize: Options.PageSize,
                        isServerPaging: Options.IsServerPaging,
                        initialSortProps: InitialSortProps));

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

        void GenerateJSViewModel(WebViewGenContext context)
        {
            // View model factory
            O();
            OB($"space.createViewModel = function (options)");
            O("if (space.vm) return space.vm;");
            O();

            GenerateJSViewModelCore(context);

            O();
            O("return space.vm;");

            End(";"); // View model factory                        
        }
    }
}