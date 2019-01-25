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
        public HardCodedKendoTagsEditorViewGen()
        {
            Lang = "C#";
        }

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

            var toTagsCollectionProp = type.GetProps()
                .First(x =>
                    x.IsNavigation &&
                    x.Type.IsCollection &&
                    x.Navigation.Reference.ToType.GetProps().Any(y =>
                        y.IsNavigation &&
                        !y.Type.IsCollection &&
                        y.Navigation.Reference.ToType.Name == "MoTag"));

            var linkType = toTagsCollectionProp.Navigation.Reference.ToType;
            var linkForeignKeyToOwner = toTagsCollectionProp.Reference.ForeignBackrefProp.ForeignKey;
            // Just select the other foreign key on the link type (there are always only to foreign keys).
            var linkForeignKeyToItem = linkType.GetProps().First(x => x.IsForeignKey && x != linkForeignKeyToOwner);
            var linkNavigationToItem = linkForeignKeyToItem.Navigation;

            var ownerTypeName = type.Name;
            var linkTypeName = linkType.Name;
            var itemTypeName = linkForeignKeyToItem.Reference.ToType.Name;

            g.O();
            g.O("[HttpPost]");
            g.OB("public async Task<IActionResult> UpdateTags(ODataActionParameters parameters)");
            g.O($"await DbCollectionOperations.UpdateUnidirM2MCollection<{ownerTypeName}, {linkTypeName}, {itemTypeName}>(");
            g.Push();

            g.OB($"new UnidirM2MCollectionOperationOptions<{ownerTypeName}, {itemTypeName}>");

            g.O("Db = _db.Context,");
            g.O("IsAutoSaveEnabled = true,");
            g.O($@"PropPath = $""{{nameof({ownerTypeName}.{toTagsCollectionProp.Name})}}.{{nameof({linkTypeName}.{linkNavigationToItem.Name})}}"",");
            g.O($@"ForeignKeyToOwner = ""{linkForeignKeyToOwner.Name}"",");
            g.O($@"ForeignKeyToItem = ""{linkForeignKeyToItem.Name}"",");

            g.OB("ValidateItem = (owner, item) =>");
            g.O("if (item.AssignableToTypeId != TypeIdentityHelper.GetTypeGuid(typeof({0})))",
                ownerTypeName);
            g.O("    ThrowBadRequest(\"The {0} is not assignable to this object.\");",
                itemTypeName);
            g.End();

            g.End(");");

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
                var name = view.TypeConfig.Name + ".Tags.unidirM2MList.editor.vm.generated";
                ScriptFilePath = BuildTsScriptFilePath(view, name);

                var context = KendoGen.InitComponentNames(new WebViewGenContext
                {
                    View = view
                });

                if (view.IsCustom || view.IsCustomView)
                    throw new Exception("'Custom' and 'Viewless' is not supported for this view.");

                TransportConfig = this.CreateODataTransport(view);

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

        public void GenerateViewModel(WebViewGenContext context)
        {
            OTsNamespace(WebConfig.ScriptUINamespace, (nscontext) =>
            {
                KendoGen.OBeginComponentViewModelFactory(context);
                O();
                OB("return new kmodo.UnidirM2MCollectionEditorForm(");

                // TODO: LOCALIZE
                var title = context.View.TypeConfig.DisplayName + " Markierungen";
                KendoGen.OViewModelOptions(context, title: title, dataType: false,
                    extend: () =>
                    {
                        // Hard-coded ID of the view's root HTML element.
                        // This ID does not change because we reusing a single piece of HTML for all Tags Editors.
                        O(@"viewId: 'f5fcba1a-44cc-4d30-ad78-640007a4b5a1',");

                        // The hard-coded ID of the MoTag grid/list view component.
                        O(@"sourceListId: '2760faee-dd1a-42f5-9c83-c9b5870c5f9e',");
                        O(@"targetListId: '2760faee-dd1a-42f5-9c83-c9b5870c5f9e',");

                        // Hard-coded Mo
                        O($@"targetContainerQuery: '{TransportConfig.ODataBaseUrl}/Query()?$select=Id&$expand=ToTags($select=Id;$expand=Tag($select=Id,DisplayName))',");

                        O(@"targetContainerListField: 'ToTags.Tag',");
                        O(@"saveBaseUrl: '{0}',", TransportConfig.ODataBaseUrl);
                        O(@"saveMethod: 'UpdateTags',");
                    });

                End(").init();");

                KendoGen.OEndComponentViewModelFactory(context);
            });
        }
    }
}