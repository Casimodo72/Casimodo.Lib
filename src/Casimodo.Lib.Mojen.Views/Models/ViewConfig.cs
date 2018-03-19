using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public class ViewItemSelection
    {
        public bool IsEnabled { get; set; }

        public bool IsMultiselect { get; set; }

        public bool UseCheckBox { get; set; }

        public bool UseAllCheckBox { get; set; }
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

            // KABU TODO: REMOVE: Now each view will only hold the data it displays.
            //   I.e. grid view will not include properties of its details and editor views anymore.
            if (Lookup.Is || Standalone.Is)
                return Controller.BuildDataGraphForRead(new[] { this });
            else
                return Controller.BuildDataGraphForRead(Group);
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
        public static readonly MojLookupViewConfig None = new MojLookupViewConfig();

        public MojLookupViewConfig()
        {
            Sources = new List<MojLookupViewSource>();
        }

        public bool Is { get; set; }

        public MojMultiplicity Multiplicity { get; set; }

        public List<MojLookupViewSource> Sources { get; private set; }

        public List<MojProp> Parameters { get; set; } = new List<MojProp>();
    }

    public class MojStandaloneViewConfig : MojBase
    {
        public static readonly MojStandaloneViewConfig None = new MojStandaloneViewConfig();

        public MojStandaloneViewConfig()
        { }

        public bool Is { get; set; }
    }

    public class MojViewCustomControl
    {
        public string Type { get; set; }

        public string SubType { get; set; }

        public int Index { get; set; }

        public string HeaderCssClass { get; set; }

        public Dictionary<string, string> Attrs { get; set; } = new Dictionary<string, string>();
    }

    public class MojViewConfig : MojPartBase
    {
        public MojViewConfig()
        {
            CanCreate = true;
            CanEdit = true;
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

        public string Title { get; set; }

        public string Message { get; set; }

        public MojType TypeConfig { get; set; }

        public List<MojViewProp> Props { get; private set; } = new List<MojViewProp>();

        public List<MojViewCustomControl> CustomControls { get; set; } = new List<MojViewCustomControl>();

        public MojViewKindConfig Kind { get; set; } = new MojViewKindConfig();

        public bool IsPage
        {
            get { return Kind.Roles.HasFlag(MojViewRole.Page); }
        }

        public bool IsLookup
        {
            get { return Kind.Roles.HasFlag(MojViewRole.Lookup); }
        }

        public string MainRoleName
        {
            get
            {
                var roles = Kind.Roles;
                if (roles.HasFlag(MojViewRole.Page))
                    return "Page";
                else if (roles.HasFlag(MojViewRole.Lookup))
                    return "Lookup";
                else if (roles.HasFlag(MojViewRole.List))
                    return "List";
                else if (roles.HasFlag(MojViewRole.Editor))
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

                //if (Kind.RawAction == ActionName.Index)
                //    return (Group ?? "") + "Index";

                return (Group ?? "") + Kind.RawAction;
                //return (Group ?? "") + (Lookup.Is ? "Lookup" : "") + TypeConfig.Name;
            }
        }

        public string CustomControllerActionName { get; set; }

        public bool IsEditor
        {
            get { return Kind.Roles.HasFlag(MojViewRole.Editor); }
        }

        public MojViewConfig RootView { get; set; }

        public MojViewConfig EditorView { get; set; }

        public List<MojViewConfig> ContentViews { get; set; } = new List<MojViewConfig>();

        public bool CanCreate { get; set; }

        public bool CanEdit { get; set; }

        public bool CanDelete { get; set; }

        public bool IsViewless { get; set; }

        public MojStandaloneViewConfig Standalone { get; set; } = MojStandaloneViewConfig.None;

        public MojLookupViewConfig Lookup { get; set; } = MojLookupViewConfig.None;

        public bool NoView { get; set; }

        public int? Width { get; set; }
        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }

        public int? Height { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxHeight { get; set; }

        public bool IsCustom { get; set; }

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

        public ViewTemplate Template { get; private set; } = new ViewTemplate();

        public bool HasFactory { get; set; }

        public string Group { get; set; }

        public bool IsPartial { get; set; }

        public bool IsInline { get; set; }

        public ViewItemSelection ItemSelection { get; private set; } = new ViewItemSelection();

        public bool UseMVVM { get; set; }

        public bool IsAuthEnabled { get; set; } = true;
        public bool IsAuthAmbientForGroup { get; set; }
        public bool IsAuthAmbientOverwritten { get; set; }

        public List<MojAuthPermission> Permissions { get; set; } = new List<MojAuthPermission>();

        public bool IsFilterable { get; set; } = true;

        public bool IsGuidFilterable { get; set; }

        // KABU TODO: REMOVE: public List<MojViewActionConfig> CustomActions { get; set; } = new List<MojViewActionConfig>();

        public MojViewConfig InlineDetailsView { get; set; }

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

            // Mark all non-selectors with loose references as read-only.
            foreach (var prop in Props)
            {
                if (!prop.IsSelector && prop.FormedNavigationTo.FirstLooseStep != null)
                    prop.IsEditable = false;
            }

            // Find lookup views to be used for selection.
            foreach (var prop in Props.Where(x => x.Lookup.Is))
            {
                //var targetType = prop.FormedNavigationTo.TargetType;

                //if (targetType == null)
                //{
                //    if (prop.Reference.IsToMany)
                //        targetType = prop.Reference.ToType;
                //    else
                //        throw new MojenException($"Can't compute target type of lookup.");
                //}

                var viewType = prop.Lookup.TargetType;
                var viewId = prop.Lookup.ViewId;
                var viewGroup = prop.Lookup.ViewGroup; // ?? "Lookup";

                var lookupViews = app.GetItems<MojViewConfig>()
                    .Where(x =>
                        x.Lookup.Is &&
                        x.TypeConfig == viewType &&
                        (viewId == null || viewId == x.Id) &&
                        viewGroup == x.Group)
                    //(viewGroup == null || viewGroup == x.Group))
                    .ToArray();

                if (lookupViews.Count() != 1)
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

        // KABU TODO: REMOVE
        //public void GetLookupView(MojType lookupType)
        //{
        //    return GetItems<MojViewConfig>()
        //            .Where(x =>
        //                x.Lookup.Is &&
        //                x.TypeConfig == lookupType)
        //            .ToArray();
        //}
    }
}