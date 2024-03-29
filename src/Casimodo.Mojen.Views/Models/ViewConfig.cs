﻿using Casimodo.Lib.Data;

namespace Casimodo.Mojen
{
    public class ViewItemSelection
    {
        public bool IsEnabled { get; set; }

        public bool IsMultiselect { get; set; }

        public bool UseCheckBox { get; set; }

        public bool UseAllCheckBox { get; set; }
    }

    public class MojViewCommand
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }

    public class MojControllerViewConfig : MojViewConfig
    {
        public MojControllerConfig Controller { get; set; }

        public override MojDataGraphNode[] BuildDataGraphForRead()
        {
            var views = new List<MojViewConfig> { this };
            if (InlineDetailsView != null)
                views.Add(InlineDetailsView);

            return Controller.BuildDataGraphForRead(views.ToArray());
        }
    }

    public class MojLookupViewSource
    {
        public MojViewConfig View { get; set; }

        public MojViewProp Prop { get; set; }

        public List<string> Filters { get; set; }
    }

    public class MojLookupViewConfig : MojBase
    {
        public static readonly MojLookupViewConfig None = new(false);

        MojLookupViewConfig(bool @is)
            : this()
        {
            Is = @is;
        }

        public MojLookupViewConfig()
        {
            Is = true;
            Sources = [];
        }

        public bool Is { get; private set; }

        public MojMultiplicity Multiplicity { get; set; }

        public List<MojLookupViewSource> Sources { get; private set; }

        public List<MojProp> Parameters { get; set; } = [];
    }

    public class MojStandaloneViewConfig : MojBase
    {
        public static readonly MojStandaloneViewConfig None = new(false);

        MojStandaloneViewConfig(bool @is)
        {
            Is = @is;
        }

        public MojStandaloneViewConfig()
        {
            Is = true;
        }

        public bool Is { get; private set; }
    }

    public class MojViewCustomControl
    {
        public string Type { get; set; }

        public string SubType { get; set; }

        public int Index { get; set; }

        public string HeaderCssClass { get; set; }

        public Dictionary<string, string> Attrs { get; set; } = [];
    }

    public class MojViewConfig : MojPartBase
    {
        public MojViewConfig()
        {
            CanCreate = true;
            CanModify = true;
            CanDelete = true;
        }

        public virtual MojDataGraphNode[] BuildDataGraphForRead()
        {
            return new MojDataGraphNode[0];
        }

        public string Id { get; set; }

        // KABU TODO: REMOVE? Never used.
        public string Name { get; set; }

        public string Alias { get; set; }

        public string Url { get; set; }

        public string VirtualFilePath { get; set; }

        public string Title { get; set; }

        public string ListComponentId { get; set; }

        public string GetDefaultTitle()
        {
            if (!string.IsNullOrEmpty(Title))
                return Title;

            if (IsPage || IsList || IsLookup)
                return TypeConfig.DisplayPluralName;
            else
                return TypeConfig.DisplayName;
        }

        public string Message { get; set; }

        public MojType TypeConfig { get; set; }

        public List<MojViewProp> Props { get; private set; } = [];

        public List<MojViewCustomControl> CustomControls { get; set; } = [];

        public MojViewKindConfig Kind { get; set; } = new MojViewKindConfig();

        public List<MojProp> Parameters { get; set; } = [];

        public string CustomPartName { get; set; }

        public string GetPartName()
        {
            if (CustomPartName != null)
                return CustomPartName;

            return TypeConfig.Name;
        }

        public bool IsPage
        {
            get { return Kind.Roles.HasFlag(MojViewRole.Page); }
        }

        public bool IsList
        {
            get { return Kind.Roles.HasFlag(MojViewRole.List); }
        }

        public bool IsDetails
        {
            get { return Kind.Roles.HasFlag(MojViewRole.Details); }
        }

        public bool IsEditor
        {
            get { return Kind.Roles.HasFlag(MojViewRole.Editor); }
        }

        public bool IsLookup
        {
            get { return Kind.Roles.HasFlag(MojViewRole.Lookup); }
        }

        public string CustomRoleName { get; set; }

        public string MainRoleName
        {
            get
            {
                if (CustomRoleName != null)
                    return CustomRoleName;

                var roles = Kind.Roles;
                if (IsPage)
                    return "Page";
                else if (IsLookup)
                    return "Lookup";
                else if (IsList)
                    return "List";
                else if (IsEditor)
                    return "Editor";
                else if (roles.HasFlag(MojViewRole.Details))
                    return "Details";

                throw new MojenException("Failed to compute main role for view.");
            }
        }

        public string FileName { get; set; }
        public string FileExtension { get; set; }

        /// <summary>
        /// The effective name (group name included) of the controller action.
        /// </summary>
        public string ControllerActionName
        {
            get
            {
                if (CustomControllerActionName != null)
                    return CustomControllerActionName;

                return (Group ?? "") + Kind.RawControllerAction;
            }
        }

        public string CustomControllerActionName { get; set; }

        public MojViewConfig RootView { get; set; }

        public MojViewConfig EditorView { get; set; }

        public MojViewConfig TagsEditorView { get; set; }

        public List<MojViewConfig> ContentViews { get; set; } = [];

        public List<MojViewCommand> CustomCommands { get; set; } = [];

        // TODO: REMOVE: public List<MojViewCommand> ListItemCommands { get; set; } = new List<MojViewCommand>();

        public bool CanCreate { get; set; }

        public bool CanModify { get; set; }

        public bool CanDelete { get; set; }

        public bool IsCustomSave { get; set; }

        public bool IsCustomSaveApi { get; set; }

        public bool IsCustomView { get; set; }

        public MojStandaloneViewConfig Standalone { get; set; } = MojStandaloneViewConfig.None;

        public MojLookupViewConfig Lookup { get; set; } = MojLookupViewConfig.None;

        public bool IsDialog { get; set; }

        public bool NoView { get; set; }

        public int? Width { get; set; }
        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }

        public int? Height { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxHeight { get; set; }
        public bool IsMaximized { get; set; }

        public bool IsCustom { get; set; }
        public bool IsCustomViewControllerMethod { get; set; }

        public bool IsSingle { get; set; }

        public bool IsModal { get; set; }

        public bool IsCachedOnClient { get; set; }

        public bool HasFilters
        {
            get { return IsFilteredByLoggedInPerson || SimpleFilter != null; }
        }

        public bool IsFilteredByLoggedInPerson { get; set; }

        public MexExpressionNode SimpleFilter { get; set; }

        public string FilteredByLoogedInPersonProp { get; set; }

        public bool IsForMobile { get; set; }

        public bool IsForDesktop { get; set; }

        public bool IsEscapingNeeded { get; set; } = true;

        public bool IsCustomODataCreateAction { get; set; }

        public bool IsCustomODataUpdateAction { get; set; }
        public bool IsCustomODataUpdateData { get; set; }

        public ViewTemplate Template { get; private set; } = new ViewTemplate();

        public string Group { get; set; }

        public bool IsPartial { get; set; }

        public bool IsInline { get; set; }

        public ViewItemSelection ItemSelection { get; private set; } = new ViewItemSelection();

        public bool UseMVVM { get; set; }

        public bool IsTaggable { get; set; }

        // TODO: REMOVE
        //public bool HasListItemContextMenu
        //{
        //    get { return IsTaggable || ListItemCommands.Count != 0; }
        //}

        public bool IsAuthEnabled { get; set; } = true;
        public bool IsAuthAmbientForGroup { get; set; }
        public bool IsAuthAmbientApplied { get; set; }
        public bool IsAuthAmbientOverwritten { get; set; }

        public bool HasNoApi { get; set; }
        public bool IsLocalData { get; set; }

        public bool IsFilterable { get; set; } = true;

        public bool? IsGlobalCompanyFilterEnabled { get; set; }

        public bool IsCompanyFilterEnabled { get; set; }

        public bool IsTagsFilterEnabled { get; set; }

        public bool IsNavigatableTo { get; set; }

        // KABU TODO: REMOVE: public List<MojViewActionConfig> CustomActions { get; set; } = new List<MojViewActionConfig>();

        public MojViewConfig InlineDetailsView { get; set; }

        public bool IsReloadable { get; set; } = true;

        public bool IsExportableToPdf { get; set; }

        public bool IsExportableToExcel { get; set; }

        public string CustomSelectFilter { get; set; }

        public MojViewProp FindSameProp(MojProp prop)
        {
            return Props.FirstOrDefault(x => x.Model.FormedTargetPath == prop.FormedTargetPath);
        }

        public bool Uses(AppPartGenerator generator)
        {
            if (generator == null) throw new ArgumentNullException("generator");
            return UsingGenerators.Any(x => x.Type == generator.GetType());
        }

        public override void Prepare(MojenApp app)
        {
            base.Prepare(app);

            // KABU TODO: REVISIT: Not sure if we can call Build() as many times as we want
            //   but we need to ensure that it has been called after any modification to the view.
            new MojViewBuilder(this).Build();

            if (IsEditor)
            {
                new MojViewBuilder(this).EnsureEditAuthControlPropsIfMissing();
            }

            // Mark all non-selectors with loose references as read-only.
            foreach (var prop in Props)
            {
                if (!prop.IsSelector && prop.FormedNavigationTo.FirstLooseStep != null)
                    prop.IsEditable = false;
            }

            // Find lookup views to be used for selection.
            foreach (var prop in Props.Where(x => x.Lookup.Is))
            {
                var viewType = prop.Lookup.TargetType;
                var viewId = prop.Lookup.ViewId;
                var viewAlias = prop.Lookup.ViewAlias;
                var viewGroup = prop.Lookup.ViewGroup;

                var lookupViews = app.GetItems<MojViewConfig>()
                    .Where(x =>
                        x.Lookup.Is &&
                        x.TypeConfig == viewType &&
                        (viewId == null || viewId == x.Id) &&
                        (viewAlias == null || viewAlias == x.Alias) &&
                        (viewGroup == x.Group || (viewGroup == null && (viewId != null || viewAlias != null)))
                    )
                    .ToArray();

                if (lookupViews.Length != 1)
                {
                    var err = $"for lookup-selector '{prop.FormedNavigationTo.TargetPath}' (Group: '{viewGroup ?? ""}', ID: '{viewId ?? ""}').";

                    if (lookupViews.Count() == 0)
                        throw new MojenException($"Lookup view not found {err}.");
                    else
                        throw new MojenException($"More than one lookup view found {err}.");
                }

                prop.LookupDialog = lookupViews.First();
            }
        }
    }
}