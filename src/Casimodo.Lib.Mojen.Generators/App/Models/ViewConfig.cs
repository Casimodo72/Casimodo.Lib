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
    }

    public class MojControllerViewConfig : MojViewConfig
    {
        public ControllerConfig Controller { get; set; }

        public override MojDataGraphNode[] BuildDataGraphForRead()
        {
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

        public MojCardinality Cardinality { get; set; }

        public List<MojLookupViewSource> Sources { get; private set; }

        public List<MojProp> Parameters { get; set; } = new List<MojProp>();
    }

    public class MojStandaloneViewConfig : MojBase
    {
        public static readonly MojStandaloneViewConfig None = new MojStandaloneViewConfig();

        public MojStandaloneViewConfig()
        { }

        public bool Is { get; set; }

        public List<MojProp> Parameters { get; set; } = new List<MojProp>();
    }

    public class MojViewCustomControl
    {
        public string Type { get; set; }

        public string SubType { get; set; }

        public int Index { get; set; }

        public string HeaderCssClass { get; set; }
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

        public string Url { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public MojType TypeConfig { get; set; }

        public List<MojViewProp> Props { get; private set; } = new List<MojViewProp>();

        public List<MojViewCustomControl> CustomControls { get; set; } = new List<MojViewCustomControl>();

        public MojViewKindConfig Kind { get; set; } = new MojViewKindConfig();

        public bool IsEditor
        {
            get { return Kind.Roles.HasFlag(MojViewRole.Editor); }
        }

        public MojViewConfig EditorView { get; set; }

        public bool CanCreate { get; set; }

        public bool CanEdit { get; set; }

        public bool CanDelete { get; set; }

        public bool IsViewModelOnly { get; set; }

        public bool IsViewless { get; set; }

        public MojStandaloneViewConfig Standalone { get; set; } = MojStandaloneViewConfig.None;

        public MojLookupViewConfig Lookup { get; set; } = MojLookupViewConfig.None;

        public bool NoView { get; set; }

        public int? Width { get; set; }

        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }

        public int? MinHeight { get; set; }
        public int? MaxHeight { get; set; }

        public bool IsCustom { get; set; }

        public bool IsSingle { get; set; }

        public bool IsModal { get; set; }

        public bool IsCachedOnClient { get; set; }

        public bool IsForMobile { get; set; }

        public bool IsForDesktop { get; set; }

        public bool IsEscapingNeeded { get; set; } = true;

        public ViewTemplate Template { get; private set; } = new ViewTemplate();

        public string FileName { get; set; }

        public string Group { get; set; }

        public bool IsPartial { get; set; }

        public ViewItemSelection ItemSelection { get; private set; } = new ViewItemSelection();

        public bool UseMVVM { get; set; }

        public bool IsAuthorizationNeeded { get; set; } = true;

        public bool IsFilterable { get; set; } = true;

        public bool IsGuidFilterable { get; set; }

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

        public void Prepare(MojenApp app)
        {
            // Mark all non-selectors with loose references as read-only.
            foreach (var prop in Props)
            {
                if (!prop.IsSelector && prop.FormedNavigationTo.FirstLooseStep != null)
                    prop.IsEditable = false;
            }

            // Find lookup views to be used for selection.
            foreach (var prop in Props.Where(x => x.Lookup.Is))
            {
                var targetType = prop.FormedNavigationTo.TargetType;
                var viewGroup = prop.Lookup.ViewGroup;
                var lookupViews = app.GetItems<MojViewConfig>()
                    .Where(x =>
                        x.Lookup.Is &&
                        x.TypeConfig == targetType &&
                        (viewGroup == null || viewGroup == x.Group))
                    .ToArray();

                if (lookupViews.Count() != 1)
                {
                    var err = $"for lookup-selector '{prop.FormedNavigationTo.TargetPath}' with group '{viewGroup ?? ""}'.";

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