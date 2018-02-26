using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoPartGen : WebPartGenerator
    {
        public string GetPlainDisplayTemplate(MojViewProp prop, bool checkLastProp = false)
        {
            var sb = new StringBuilder();

            sb.o("#if("); GetNotNullExpressionTemplate(sb, prop, checkLastProp); sb.o("){#");

            sb.o("#:"); sb.o(prop.FormedNavigationTo.TargetPath); sb.o("#");

            sb.o("#}#");

            return sb.ToString();
        }

        void GetNotNullExpressionTemplate(StringBuilder sb, MojViewProp prop, bool checkLastProp = false)
        {
            // Example:
            // typeof Company !== 'undefined' && Company && Company.NameShort
            var steps = prop.FormedTargetPathNames.ToArray();
            var length = steps.Length - (checkLastProp ? 0 : 1);
            for (int i = 0; i < length; i++)
            {
                if (i == 0) sb.o($"typeof {steps[0]}!=='undefined'&&");

                for (int k = 0; k <= i; k++)
                {
                    sb.o($"{steps[k]}");

                    if (k < i) sb.o(".");
                }

                if (i < length - 1) sb.o("&&");
            }
        }

        public void OModalMessage(MojenGeneratorBase gen, string containerVar, string message)
        {
            WriteTo(gen, () =>
            {
                Oo($"var wnd = $('<div></div>').appendTo({containerVar}).kendoWindow(");
                OWindowOptions(new KendoWindowConfig
                {
                    Animation = false,
                    Width = 300,
                    IsModal = true,
                });
                oO(").data('kendoWindow');"); // Kendo window

                O($"wnd.content('<div style=\"margin: 12px\">{message}</div>');");

                O("kendomodo.setModalWindowBehavior(wnd);");

                O("wnd.center().open();");

            });
        }

        public void OWindowOptions(KendoWindowConfig window)
        {
            // See http://docs.telerik.com/KENDO-UI/api/javascript/ui/window

            var options = XBuilder.CreateAnonymous();

            XBuilder animation = null;
            if (window.Animation == null || (window.Animation as bool?) == true)
            {
                if (window.IsParentModal)
                    animation = options.Add("animation", "false");
                else
                    animation = options.Add("animation", "kendomodo.getDefaultDialogWindowAnimation()");
            }

#if (false)
            if (window.Open.Is)
                animation.O("open")
                    .O("effects", window.Open.Effects).End()
                    .O("duration", window.Open.Duration).End();

            if (window.Close.Is)
                animation.O("close")
                    .O("effects", window.Close.Effects).End()
                    .O("duration", window.Close.Duration).End();
#endif
            if (window.ContentUrl != null)
            {
                options.Add("content")
                    .Add("url", window.ContentUrl, text: true);
            }

            options.Add("modal", window.IsModal);
            options.Add("visible", window.IsVisible);
            if (window.Title != null) options.Add("title", window.Title, text: true);

            var width = window.Width ?? window.MinWidth ?? window.MaxWidth;
            if (width != null) options.Add("width", width);

            if (window.MinWidth != null) options.Add("minWidth", window.MinWidth);
            if (window.MaxWidth != null) options.Add("maxWidth", window.MaxWidth);

            var height = window.Height ?? window.MinHeight;
            if (height != null) options.Add("height", height);

            if (window.MinHeight != null) options.Add("minHeight", window.MinHeight);

            if (window.OnClosing != null) options.Add("close", window.OnClosing);
            if (window.OnDeactivated != null) options.Add("deactivate", window.OnDeactivated);

            OJsObjectLiteral(options.Elem, trailingNewline: false, trailingComma: false);
        }

        public void OStandaloneEditorViewModel(MojViewConfig view, string componentName = null)
        {
            // View model for standalone editor views.

            OJsImmediateBegin("space");

            var transportConfig = this.CreateODataTransport(view, view);

            var config = new KendoDataSourceConfig
            {
                TypeConfig = view.TypeConfig,
                TransportType = "odata-v4",
                UseODataActions = true,
                TransportConfig = transportConfig,
                ModelFactory = "createDataModel()",
                CanEdit = view.CanEdit,
                CanCreate = view.CanCreate,
                CanDelete = view.CanDelete,
                PageSize = 1
            };

            O($"var args = casimodo.ui.dialogArgs.consume('{view.Id}');");

            // KABU TODO: REVISIT: document.scriptElement does not work here, because
            // Chrome and Firefox will move the script elements from their original location to somewhere else.
            // Such a pity.
            O($"var $view = $('#view-{view.Id}');");
            // Create a dummy view model, because we don't use one.
            O("space.vm = {}");
            O();
            OB("function createDataModel ()");
            OB("return");
            GenerateDataSourceModel(transportConfig.ModelProps);
            End();
            End();

            O();
            OB("var ds = new kendo.data.DataSource(");
            ODataSource(config);
            End(");");

            O();
            O("kendomodo.initStandaloneEditorDialog($view, args, ds);");

            O();
            O($"ds.filter({{ field: '{config.TypeConfig.Key.Name}', operator: 'eq', value: args.value }});");

            OJsImmediateEnd(BuildNewComponentSpace(componentName));
        }

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public void OStandaloneEditableDetailsViewModel(WebViewGenContext context, string space)
        {
            // View model for standalone editor views.

            var view = context.View;

            OJsImmediateBegin("space");

            // View model factory function.
            O();
            OB($"space.createViewModel = function ()");
            O("if (space.vm) return space.vm;");
            O();
            OJsViewModelClass("ViewModel", extends: "kendomodo.EditableDetailsViewModel",
                construct: null,
                content: () =>
            {
                var transportConfig = this.CreateODataTransport(view, null);

                OB("fn.createDataModel = function ()");
                OB("return");
                GenerateDataSourceModel(transportConfig.ModelProps);
                End(";");
                End(";");

                // Data source factory.
                O();
                OB("fn.createDataSource = function ()");
                O("if (this.dataSource)");
                O("    return this.dataSource;");
                O();
                O("var self = this;");
                O();
                OB("this.dataSource = new kendo.data.DataSource(");
                ODataSource(new KendoDataSourceConfig
                {
                    TypeConfig = view.TypeConfig,
                    TransportType = "odata-v4",
                    UseODataActions = true,
                    TransportConfig = transportConfig,
                    ModelFactory = "this.createDataModel()",
                    CanEdit = false,
                    CanCreate = false,
                    CanDelete = false,
                    PageSize = 1
                });
                O("change: $.proxy(self.onDataSourceChanged, self)");
                End(");");
                O();
                O("return this.dataSource;");
                End(";");
            });
            O();
            OB("space.vm = new ViewModel(");
            O("space: space,");
            OJsViewModelConstructorOptions(context, isList: false);
            if (view.EditorView != null)
            {
                OB("editor:");
                O($"viewId: '{view.EditorView.Id}',");
                O($"url: '{view.EditorView.Url}',");
                O($"width: {MojenUtils.ToJsValue(view.EditorView.Width)},");
                O($"height: {MojenUtils.ToJsValue(view.EditorView.MinHeight)},");
                End();
            }
            End(");");
            O();
            O("return space.vm;");
            End(";");

            // KABU TODO: REMOVE
            //// Component factory function.
            //OB("space.createComponent = function(options)");
            //OB($"kendomodo.createStandaloneDetailsComponentOnSpace(");
            //O("space: space,");
            //O($"viewId: '{view.Id}',");
            //O($"title: '{view.TypeConfig.DisplayName}',");
            //if (view.EditorView != null)
            //{
            //    OB("editor:");
            //    O($"viewId: '{view.EditorView.Id}',");
            //    O($"url: '{view.EditorView.Url}',");
            //    O($"width: {MojenUtils.ToJsValue(view.EditorView.Width)},");
            //    O($"height: {MojenUtils.ToJsValue(view.EditorView.MinHeight)},");
            //    End();

            //}
            //End(");");
            //End(";");

            OJsImmediateEnd(BuildNewComponentSpace(space));
        }

        public void OJsViewModelConstructorOptions(WebViewGenContext context, bool isList)
        {
            var view = context.View;
            var title = isList ? view.TypeConfig.DisplayPluralName : view.TypeConfig.DisplayName;
            O($"title: '{title}',");
            O($"viewId: '{view.Id}',");
            O($"itemTypeName: '{view.TypeConfig.Name}',");
            O($"areaName: '{view.TypeConfig.PluralName}',");
            O($"isDialog: {MojenUtils.ToJsValue(view.Lookup.Is)},");
            O($"isAuthRequired: {MojenUtils.ToJsValue(view.IsAuthorizationNeeded)},");
            if (view.ItemSelection.IsMultiselect && view.ItemSelection.UseCheckBox)
                O("selectionMode: 'multiple',");
            O($"componentId: '{context.ComponentId}',");
        }

        public void OJsViewModelClass(string name, string extends, Action construct, Action content)
        {
            Guard.ArgNotNullOrWhitespace(name, nameof(name));
            Guard.ArgNotNullOrWhitespace(extends, nameof(extends));
            Guard.ArgNotNull(content, nameof(content));

            // Extend base component view model.
            OB($"var {name} = (function (_super)");

            O($"casimodo.__extends({name}, _super);");

            O();
            OB($"function {name}(options)");
            O("_super.call(this, options);");
            construct?.Invoke();
            End();

            O();
            O($"var fn = {name}.prototype;");

            if (content != null)
            {
                O();
                content();
            }

            O();
            O($"return {name};");

            End($")({extends});");
        }
    }
}
