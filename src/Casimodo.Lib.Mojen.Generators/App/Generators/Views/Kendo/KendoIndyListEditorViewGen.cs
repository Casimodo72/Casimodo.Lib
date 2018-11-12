using System;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public partial class KendoIndyListEditorViewGen : KendoViewGenBase
    {
        bool EvalTagEditorViews(MojType type)
        {
            var views = App.GetItems<MojViewConfig>()
                .Where(x => x.Uses(this) && x.TypeConfig.Id == type.Id)
                .ToList();

            if (!views.Any())
                return false;

            if (views.Count > 1)
                throw new MojenException("More than one TagEditor defined for a single type.");

            return true;
        }

        string ScriptFilePath { get; set; }
        public string ListPropName { get; set; }

        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>()
                .Where(x => x.Uses(this)))
            {
                ListPropName = view.Group;

                var name = view.TypeConfig.Name + "." + ListPropName + ".indylist.editor.vm.generated";

                ScriptFilePath = BuildJsScriptFilePath(view, name);

                var context = KendoGen.InitComponentNames(new WebViewGenContext
                {
                    View = view
                });
                context.ComponentId = "indylist-editor-view-" + view.Id;

                if (view.IsCustom || view.IsCustomView)
                    throw new Exception("'Custom' and 'Viewless' is not supported for this view.");

                TransportConfig = this.CreateODataTransport(view);

                PerformWrite(view, () => GenerateView(context));

                PerformWrite(ScriptFilePath, () =>
                {
                    OScriptUseStrict();

                    GenerateViewModel(context);
                });

                RegisterComponent(context);
            }
        }

        public MojHttpRequestConfig TransportConfig { get; set; }

        public override MojenGenerator Initialize(MojenApp app)
        {
            base.Initialize(app);
            KendoGen.SetParent(this);
            return this;
        }

        public void GenerateView(WebViewGenContext context)
        {
            ORazorGeneratedFileComment();

            XB($"<div class='casimodo-dialog-toolbar' id='dialog-commands-{context.View.Id}'>");

            O("<button class='k-button cancel-button casimodo-dialog-button'>Abbrechen</button>");
            O("<button class='k-button ok-button casimodo-dialog-button'>OK</button>");

            XE("</div>");

            // Container element for the Kendo grid widget.
            XB($"<div id='{context.ComponentId}' class='component-root indylist-editor-view'>");

            O("<div class='indylist-editor-header'></div>");

            XB("<div class='indylist-editor-content'>");
            O("<div class='indylist-target-view component-root'></div>");
            O("<div class='indylist-source-view component-root'></div>");
            XE("</div>");

            XE("</div>");
        }

        public void GenerateViewModel(WebViewGenContext context)
        {
            KendoGen.OBeginComponentViewModelFactory(context);
            O();
            OB("var vm = new kmodo.IndyCollectionEditorForm(");

            var listProp = context.View.TypeConfig.GetProp(ListPropName);
            var title = listProp.DisplayLabel;
            KendoGen.OViewModelOptions(context, title: title, dataType: false,
                extend: () =>
                {
                    // Hard-coded ID of the view's root HTML element.
                    // This ID does not change because we reusing a single piece of HTML for all Tags Editors.
                    O("viewId: '{0}',", context.View.Id);

                    // The ID of the grid/list view component used for selection of list items.
                    O("sourceListId: '{0}',", context.View.ListComponentId);
                    O("targetListId: '{0}',", context.View.ListComponentId);

                    // KABU TODO: MAGIC: This assumes the properties "Id" and "DisplayName".
                    O(@"targetContainerQuery: '{0}/Query()?$select=Id&$expand={1}($select=Id,DisplayName)',",
                        TransportConfig.ODataBaseUrl,
                        ListPropName);

                    O(@"targetContainerListField: '{0}',", ListPropName);
                    O(@"saveBaseUrl: '{0}',", TransportConfig.ODataBaseUrl);
                    O(@"saveMethod: 'Update{0}',", ListPropName);
                });

            End(").init();");
            O();
            O("return vm;");
            KendoGen.OEndComponentViewModelFactory(context);
        }
    }
}