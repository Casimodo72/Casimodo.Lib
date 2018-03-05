using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public class WebScriptGen : WebPartGenerator
    { }

    public class KendoPagerOptions
    {
        public bool UseRefresh { get; set; } = true;
        public bool UseInput { get; set; } = true;
        public bool UsePageSizes { get; set; } = true;
    }

    public class KendoGridOptions
    {
        // We need to use a custom OData method e.g. for querying of return Mos with IsDeleted and IsRecyclableDeleted.
        public string CustomQueryMethod { get; set; }

        public object Height { get; set; }

        public bool IsScrollable { get; set; }

        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Not used but keep though.
        /// </summary>
        internal bool IsDeferred { get; set; }

        public bool IsCreatable { get; set; } = true;

        public bool? IsDeletable { get; set; }

        public bool IsUsingLocalData { get; set; }

        public bool IsHeaderVisible { get; set; } = true;

        /// <summary>
        /// KABU TODO: Not implemented yet. We need to use CSS for hiding the column header.
        /// </summary>
        public bool IsColumnHeaderVisible { get; set; } = true;

        public bool IsPagerVisible { get; set; } = true;

        public bool IsServerPaging { get; set; } = true;

        public bool IsFilterOverCurrentDataEnabled { get; set; }

        public KendoPagerOptions Pager { get; set; } = new KendoPagerOptions();
    }

    /// <summary>
    /// Just an alias class for switching between the JS and MVC grid.
    /// </summary>
    public class KendoGridViewGen : KendoJsGridViewGen
    { }

    public abstract partial class KendoGridGenBase : KendoViewGenBase
    {
        public override MojenGenerator Initialize(MojenApp app)
        {
            base.Initialize(app);

            EditorGen.Initialize(app);
            EditorDataModelGen.Initialize(app);
            InlineDetailsGen.Initialize(app);
            ScriptGen.Initialize(app);
            KendoGen.SetParent(this);

            return this;
        }

        protected virtual void Reset()
        {
            Context = new WebViewGenContext();
            UseEntity = true;
            AutoCompleteFilters = new List<MojPropAutoCompleteFilter>();
            JsFuncs = new KendoWebGridEventsConfig();
        }

        public WebViewGenContext Context { get; set; }

        public DataLayerConfig DataConfig { get; set; }

        public bool UseEntity { get; set; }

        public MojViewConfig View { get; private set; }

        public bool UseClientTemplates { get; set; }

        public MojViewConfig InlineDetailsView { get; set; }

        public MojViewConfig EditorView { get; private set; }

        public WebScriptGen ScriptGen { get; set; } = new WebScriptGen();

        public KendoPartGen KendoGen { get; set; } = new KendoPartGen();

        public KendoWebDisplayTemplate2Gen InlineDetailsGen { get; set; } = new KendoWebDisplayTemplate2Gen();

        public string InlineDetailsTemplateName { get; set; }

        public WebDataEditViewModelGen EditorDataModelGen { get; set; } = new WebDataEditViewModelGen();

        public KendoEditorGen EditorGen { get; set; } = new KendoEditorGen();

        public string EditorTemplateName { get; set; }

        public bool CanEdit { get; set; }

        public bool CanCreate { get; set; }

        public bool CanDelete { get; set; }

        public bool HasToolbar { get; set; }

        public List<MojPropAutoCompleteFilter> AutoCompleteFilters { get; set; }

        public KendoWebGridEventsConfig JsFuncs { get; set; }

        public MojViewProp[] InitialSortProps { get; set; }

        public KendoGridOptions Options { get; set; } = new KendoGridOptions();

        public string DataSourceType { get; set; } = "odata-v4";

        public MojHttpRequestConfig TransportConfig { get; set; }

        public string ViewModelContainerElem { get; set; }

        /// <summary>
        /// KABU TODO: Currently not used.
        /// </summary>
        public string AutoCompletePartialViewName { get; set; } = "_KendoAutoComplete";

        public string InlineDetailsViewFileName { get; set; } = "Details.Inline";
        public string InlineDetailsViewFilePath { get; set; }
        public string InlineDetailsViewVirtualFilePath { get; set; }

        public string EditorViewFileName { get; set; } = "Editor";
        public string EditorViewFilePath { get; set; }
        public string EditorViewVirtualFilePath { get; set; }

        public string ViewModelScriptFilePath { get; set; }
        public string ViewModelScriptVirtualFilePath { get; set; }

        public string ViewModelExtensionScriptFilePath { get; set; }
        public string ViewModelExtensionScriptVirtualFilePath { get; set; }
        public string ViewModelExtensionClassName { get; set; }

        protected override void GenerateCore()
        {
            foreach (MojViewConfig view in App.GetItems<MojViewConfig>()
                .Where(x => x.Uses(this) && !x.IsCustom && x.Kind.Roles.HasFlag(MojViewRole.List)))
            {
                Reset();

                var context = Context = new WebViewGenContext { View = view };
                DataConfig = App.GetDataLayerConfig(context.View.TypeConfig.DataContextName);

                View = view;

                Options = view.GetGeneratorConfig<KendoGridOptions>() ?? new KendoGridOptions();

                // ID of component
                // KABU TODO: Introduce a component GUID when we need to generate nested grids.
                // NOTE: We can't use "." as separator because Kendo will convert "." to "_" anyway.
                // IMPORTANT: The formating with "_" is *needed* for the kendomodo JavaScript
                //   extract the type of object is being used -
                //   so **don't change** this:
                context.ComponentId = "grid-" + view.Id;
                context.ComponentName = $"grid{view.TypeConfig.Name}Items";
                context.ComponentViewModelName = context.ComponentName + "ViewModel";
                context.ComponentViewSpaceName = context.ComponentName + (View.Lookup.Is ? "Lookup" : "") + "Space";
                context.ComponentViewSpaceFactoryName = context.ComponentViewSpaceName + "Factory";
                context.ComponentViewModelContainerElemClass = ConvertToCssName(context.ComponentViewModelName + "Container");

                // NOTE: The grid will always have a toolbar, because we have a custom refresh button.
                HasToolbar = true;

                AutoCompleteFilters.AddRange(context.View.Props
                    .Select(x => x.AutoCompleteFilter)
                    .Where(x => x.IsEnabled)
                    .OrderBy(x => x.Position));

                // Inline details
                InlineDetailsView = view.InlineDetailsView;
                InlineDetailsTemplateName = "grid-detail-template-" + view.Id;
                if (InlineDetailsView != null)
                {
                    InlineDetailsViewVirtualFilePath = BuildVirtualFilePath(InlineDetailsView,
                            name: InlineDetailsViewFileName,
                            partial: true);

                    InlineDetailsViewFilePath = BuildFilePath(InlineDetailsView,
                            name: InlineDetailsViewFileName,
                            partial: true);
                }

                // Editor
                EditorView = view.EditorView;
                EditorTemplateName = "grid-popup-editor-template-" + view.Id;
                CanEdit = EditorView != null && EditorView.CanEdit;
                CanCreate = CanEdit && EditorView != null && EditorView.CanCreate && Options.IsCreatable;
                CanDelete = Options.IsDeletable == true || (EditorView != null && EditorView.CanDelete && (Options.IsDeletable ?? true));
                if (EditorView != null)
                {
                    EditorViewVirtualFilePath = BuildVirtualFilePath(EditorView,
                        name: EditorViewFileName,
                        partial: true);

                    EditorViewFilePath = BuildFilePath(EditorView,
                        name: EditorViewFileName,
                        partial: true);
                }

                // View model script file path
                var viewModelScriptSuffix = ".vm.generated";
                ViewModelScriptFilePath = BuildJsScriptFilePath(View, suffix: viewModelScriptSuffix);
                ViewModelScriptVirtualFilePath = BuildJsScriptVirtualFilePath(View, suffix: viewModelScriptSuffix);

                viewModelScriptSuffix = ".vm.extension";
                ViewModelExtensionScriptFilePath = BuildJsScriptFilePath(View, suffix: viewModelScriptSuffix);
                ViewModelExtensionScriptVirtualFilePath = BuildJsScriptVirtualFilePath(View, suffix: viewModelScriptSuffix);
                ViewModelExtensionClassName = BuildJsScriptFileName(View, suffix: viewModelScriptSuffix, extension: false)
                    .Split('.')
                    .Select(x => MojenUtils.FirstCharToUpper(x))
                    .Join("");

                // Generate grid.
                if (!context.View.IsCustom)
                {
                    if (context.View.IsViewModelOnly || context.View.IsViewless)
                        GenerateGridView(context);
                    else
                        PerformWrite(context.View, () => GenerateGridView(context));
                }

                // Generate inline details view
                if (InlineDetailsView != null && !InlineDetailsView.IsCustom)
                {
                    InlineDetailsGen.PerformWrite(InlineDetailsViewFilePath, () =>
                    {
                        InlineDetailsGen.GenerateView(new WebViewGenContext
                        {
                            IsEditableView = false,
                            IsModalView = View.IsModal,
                            View = InlineDetailsView
                        });
                    });
                }

                // Generate editor view.
                if (EditorView != null)
                {
                    EditorGen.UsedViewPropInfos.Clear();
                    // KABU TODO: IMPORTANT: BUG: EditorGen.UsedViewPropInfos will be empty if the view is custom.
                    //   Thus the data view model will be empty.

                    if (!EditorView.IsCustom)
                    {
                        EditorGen.PerformWrite(EditorViewFilePath, () =>
                        {
                            EditorGen.GenerateView(new WebViewGenContext
                            {
                                IsEditableView = CanEdit,
                                IsModalView = true,
                                View = EditorView
                            });
                        });
                    }

                    // Generate editor view's data view model.
                    EditorDataModelGen.PerformWrite(Path.Combine(GetViewDirPath(View), BuildViewModelFileName(EditorView)), () =>
                    {
                        EditorDataModelGen.GenerateEditViewModel(EditorView.TypeConfig, EditorGen.UsedViewPropInfos, EditorView.Group);
                    });
                }
            }
        }

        public abstract void GenerateGridView(WebViewGenContext context);

        public void OLookupDialogToolbar(WebViewGenContext context)
        {
            if (context.View.Kind.Roles.HasFlag(MojViewRole.Lookup))
            {
                // Lookup view will have an ID assigned.
                if (string.IsNullOrWhiteSpace(context.View.Id)) throw new MojenException("The lookup view has no ID.");

                OB($"<div class='casimodo-dialog-toolbar' id='dialog-commands-{context.View.Id}'>");

                O("<button class='k-button cancel-button casimodo-dialog-button'>Abbrechen</button>");
                O("<button class='k-button ok-button casimodo-dialog-button'>OK</button>");

                OE("</div>");
            }
        }
    }
}
