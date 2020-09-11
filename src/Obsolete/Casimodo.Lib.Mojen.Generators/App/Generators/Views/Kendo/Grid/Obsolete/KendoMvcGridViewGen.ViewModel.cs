using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoWebMvcGridViewGen
    {
        void GenerateGridEvents(WebViewGenContext context)
        {
            O(".Events(e => e");
            Push();

            foreach (var handler in JsFuncs.Handlers)
            {
                // E.g .Save("kendomodo.onGridSaving")

                var vm = handler.IsMPart ? context.ComponentViewModelName + "." : "";
                O($".{handler.ComponentEventName}(\"{vm}{handler.FunctionName}\")");
            }

            Pop();
            O(")");
        }

        void GenerateJSViewModel(WebViewGenContext context)
        {
            OScriptBegin();
            GenerateJSViewModelCore(context);
            OScriptEnd();
        }

        void GenerateJSViewModelCore(WebViewGenContext context)
        {
            var vm = context.ComponentViewModelName;
            var view = context.View;

            // View model variable.
            // KABU TODO: Use a dedicated namespace for view models. E.g. "geoassistant.session.vm.gridContractViewModel";
            O($"var {vm} = {vm}_Create();");

            // View model factory function ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            O();
            OB($"function {vm}_Create()");
            OB($"var self = kendo.observable(");            

            if (View.Lookup.Is)
            {
                OB("setArgs: function(args)");
                O("this.args = args;");
                O("this.args.buildResult = this.buildResult.bind(this);");
                End(",");

                OB("buildResult: function()");
                O("this.args.value = this.selected[this.keyName];");
                End(",");
            }

            O("item: null,");
            // O("    editItem: null");

            O($"keyName: \"{context.View.TypeConfig.Key.Name}\",");

            O("component: null,");

            O("selected: {},");

            // This is for lookup views.
            O("dialogResult: null,");

            O("expandedKeys: []");

            // Define main event handler functions and call each specific function.
            foreach (var item in JsFuncs.Handlers.Where(x => x.IsContainer))
            {
                OB($", {item.FunctionName}: function (e)");

                foreach (var func in item.BodyFunctions)
                {
                    if (func.IsMPart)
                        O($"self.{func.FunctionName}(e);");
                    else
                        O($"{func.FunctionName}(e);");
                }

                End(); // ".bind(this)");
            }

            // View model functions.
            foreach (var func in JsFuncs.Functions.Where(x => x.IsMPart && x.Body != null))
            {
                OB($", {func.FunctionName}: function (e)");
                func.Body(context);
                End(); // ".bind(this)");
            }            

            End(");"); // ViewModel

            O("return self;");

            End(); // ViewModel factory                       
            O();

            // Non-view-model functions.
            foreach (var func in JsFuncs.Functions.Where(x => !x.IsMPart && x.Body != null))
            {
                OB($"function {func.FunctionName} (e)");
                func.Body(context);
                End();
            }
        }

        void GenerateJSDocumentReady(WebViewGenContext context)
        {
            var vm = context.ComponentViewModelName;
            var type = context.View.TypeConfig;

            // Document-ready function ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            OScriptBegin();

            OB("$(function ()");

            if (Options.IsDeferred)
            {
                // Workaround: MVVM bindings go terribly wrong when an other view model
                // overrides the grid's own view model.
                // See http://www.telerik.com/forums/grid-filter-row-weird-behaviour
                O();
                OB("setTimeout(function()");
                O();
                O("@(Html.Kendo().DeferredScripts(false))");
            }

            O();
            O($"var componentElem = $('#{context.ComponentId}');");
            O($"var component = componentElem.data('kendoGrid');");

            // Fixup grid's data source model.
            O();
            string usedPropNames = DataSource.ModelProps.Select(x => x.Name).Join(",");
            //string requiredPropNames = DataSource.ModelProps.Where(x => x.IsRequiredOnEdit).Select(x => x.Name).Join(",");
            O($"kendomodo.fixupDataSourceModel(component, '{type.QualifiedClassName}', '{usedPropNames}');");

            // Bind to view model
            O();
            O($"var vm = {vm};");

            O($"vm.component = component;");

            // Bind our custom refresh button
            O();
            O("componentElem.find('.k-grid-toolbar .k-grid-refresh').each(function() {");
            O("    $(this).click(function(e) {");
            O("        vm.component.dataSource.read();");
            O("    });");
            O("    return false;");
            O("});");

            // View model binding function.
            // NOTE: Currently not used.
#if (false)
            if (context.View == ???)
            {
                O();
                O($"kendo.bind(componentElem, vm);");
            }
#endif

            // Lookup dialog ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            if (View.Lookup.Is)
            {
                O();
                O("// Set dialog arguments.");
                O($"var id = '{View.Id}';");
                O($"vm.setArgs(casimodo.ui.dialogArgs.consume(id));");
                O("// Init OK/close buttons.");
                O("kendomodo.initDialogActions($('#' + id), vm.args);");
                O("// Apply filters.");
                O("kendomodo.applyGridDialogArgs(space.component, vm.args);");
            }

            if (Options.IsDeferred)
            {
                End(");"); // Timeout function
            }

            End(");"); // Document read function

            OScriptEnd();
        }
    }
}