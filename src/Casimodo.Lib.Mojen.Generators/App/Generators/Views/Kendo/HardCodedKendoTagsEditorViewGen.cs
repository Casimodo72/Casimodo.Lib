using System;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public interface IMvcActionInjector
    {
        bool GenerateMvcActionFor(WebPartGenerator g, MojViewConfig view);
    }

    public interface IWebApiODataActionInjector
    {
        bool GenerateWebApiODataActionConfigFor(WebPartGenerator g, MojType type);
        bool GenerateWebApiODataActionFor(WebPartGenerator g, MojType type);
    }

    public partial class HardCodedKendoTagsEditorViewGen : KendoViewGenBase, IWebApiODataActionInjector
    {
        bool IWebApiODataActionInjector.GenerateWebApiODataActionConfigFor(WebPartGenerator g, MojType type)
        {
            if (!EvalTagEditorViews(type))
                return false;

            g.O();
            g.O($"action = {type.VName}.Collection.Action(\"UpdateTags\");");
            g.O($"action.Parameter<Guid>(\"id\");");
            g.O($"action.CollectionParameter<Guid>(\"itemIds\");");
            g.O($"action.Returns<int>();");

            return true;
        }

        bool IWebApiODataActionInjector.GenerateWebApiODataActionFor(WebPartGenerator g, MojType type)
        {
            if (!EvalTagEditorViews(type))
                return false;

            var ownerTypeName = type.Name;
            var itemTypeName = "MoTag";

            g.O();
            g.O("[HttpPost]");
            g.O("public IHttpActionResult UpdateTags(ODataActionParameters parameters)");
            g.Begin();
            g.O("_db.Context.Configuration.LazyLoadingEnabled = false;");
            g.O("_db.Context.Configuration.AutoDetectChangesEnabled = false;");
            g.O();
            g.O("if (this.UpdateIndependentCollection<{0}, {1}>(parameters, _db.Context, nameof({0}.Tags),",
                ownerTypeName, itemTypeName);
            g.Push();
            g.O("validateItem: (controller, owner, item) =>");
            g.Begin();
            g.O("if (item.AssignableToTypeId != TypeIdentityHelper.GetTypeGuid(typeof({0})))",
                ownerTypeName);
            g.O("    controller.ThrowBadRequest(\"The {0} is not assignable to this object.\");",
                itemTypeName);
            g.End("))");
            g.Pop();
            g.Begin();
            g.O("_db.SaveChanges();");
            g.End();
            g.O();
            g.O("return Ok(1);");
            g.End();

            return true;
        }

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

        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>()
                .Where(x => x.Uses(this)))
            {
                var name = view.TypeConfig.Name + ".Tags.indylist.editor.vm.generated";
                ScriptFilePath = BuildJsScriptFilePath(view, name);
                //ScriptVirtualFilePath = BuildJsScriptVirtualFilePath(view, name);

                var context = KendoGen.InitComponentNames(new WebViewGenContext
                {
                    View = view
                });

                if (view.IsCustom || view.IsViewless)
                    throw new Exception("'Custom' and 'Viewless' is not supported for this view.");

                TransportConfig = this.CreateODataTransport(view);

                //PerformWrite(view, () => GenerateView(context));

                PerformWrite(ScriptFilePath, () =>
                {
                    OScriptUseStrict();

                    GenerateViewModel(context);
                });

                //var dataViewModelGen = new WebDataEditViewModelGen();
                //dataViewModelGen.Initialize(App);

                //dataViewModelGen.PerformWrite(Path.Combine(GetViewDirPath(view), BuildEditorDataModelFileName(view)), () =>
                //{
                //    dataViewModelGen.GenerateEditViewModel(view.TypeConfig, UsedViewPropInfos, view.Group,
                //        isDateTimeOffsetSupported: false);
                //});

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

        public void GenerateViewModel(WebViewGenContext context)
        {
            KendoGen.OBeginComponentViewModelFactory(context);
            O();
            OB("var vm = new kendomodo.ui.FormIndyCollectionEditorViewModel(");

            // TODO: LOCALIZE
            var title = context.View.TypeConfig.DisplayName + " Markierungen";
            KendoGen.OViewModelOptions(context, title: title, dataType: false,
                extend: () =>
                {
                    // Hard-coded ID of the view's root HTML element.
                    // This ID does not change because we reusing a single piece of HTML for all Tags Editors.
                    O(@"viewId: '844ed81d-dbbb-4278-abf4-2947f11fa4d3',");

                    // The hard-coded ID of the MoTag grid/list view component.
                    O(@"sourceListId: '2760faee-dd1a-42f5-9c83-c9b5870c5f9e',");
                    O(@"targetListId: '2760faee-dd1a-42f5-9c83-c9b5870c5f9e',");

                    // Hard-coded Mo
                    O(@"targetContainerQuery: '{0}/Query()?$select=Id&$expand=Tags($select=Id,DisplayName)',",
                        TransportConfig.ODataBaseUrl);

                    O(@"targetContainerListField: 'Tags',");
                    O(@"saveBaseUrl: '{0}',", TransportConfig.ODataBaseUrl);
                    O(@"saveMethod: 'UpdateTags',");
                });

            End(").init();");
            O();
            O("return vm;");
            KendoGen.OEndComponentViewModelFactory(context);
        }
    }
}