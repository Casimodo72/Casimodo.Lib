using System;
using System.Linq;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;
using System.Collections.Generic;

namespace Casimodo.Lib.Mojen
{
    public class GeoPlaceSourcePropMap
    {
        public string Street { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string CountryStateId { get; set; }
        public string CountryId { get; set; }

        public IEnumerable<Tuple<string, string>> GetMappings()
        {
            if (Street != null) yield return Tuple.Create("Street", Street);
            if (ZipCode != null) yield return Tuple.Create("ZipCode", ZipCode);
            if (City != null) yield return Tuple.Create("City", City);
            if (CountryStateId != null) yield return Tuple.Create("CountryStateId", CountryStateId);
            if (CountryId != null) yield return Tuple.Create("CountryId", CountryId);
        }
    }

    public class GeoPlaceLookupConfig
    {
        public static readonly GeoPlaceLookupConfig None = new GeoPlaceLookupConfig { Is = false };
        public GeoPlaceLookupConfig()
        {
            Is = true;
        }

        public bool Is { get; set; }

        public bool IsViewCached { get; set; }

        public GeoPlaceSourcePropMap SourcePropMap { get; set; }
    }

    public class MojStringSubstringConfig
    {
        public static readonly MojStringSubstringConfig None = new MojStringSubstringConfig();
        public bool Is { get; set; }
        public int StartIndex { get; set; }
        public int? Length { get; set; }
    }

    public static class MojViewPropExtensions
    {
        public static MojViewPropInfo BuildViewPropInfo(this MojViewProp vprop,
            bool selectable = false,
            // KABU TODO: REMOVE: @lookupable is not used anywhere.
            // bool lookupable = false,
            bool column = false,
            bool isGroupedByTarget = true,
            bool alias = false,
            bool allowCollections = false)
        {
            var isEntity = vprop.DeclaringType.IsEntity();
            var path = vprop.FormedNavigationTo;

            if (!path.Is || !path.IsForeign)
            {
                return new MojViewPropInfo
                {
                    IsNative = true,
                    ViewProp = vprop,
                    PropPath = vprop.Name,
                    PropAliasPath = vprop.Alias,
                    // NOTE: Prop and TargetDisplayProp are *equal* in this case.
                    Prop = vprop,
                    TargetDisplayProp = vprop,
                    TargetType = vprop.DeclaringType,
                    CustomDisplayLabel = GetCustomDisplayLabel(vprop, vprop.DisplayLabel)
                };
            }

            if (path.TargetProp == null)
                throw new MojenException("The navigation path has no target property.");

            CheckPathSteps(path);

            if (selectable)
            {
                // For editable selectors:
                // In case the navigation path contains a loose reference,
                // we must stop at the first loose reference and use the foreign key instead.
                // E.g.    [Contract].BusinessContact(nested) -> Salutation(loose) -> DisplayName
                // becomes [Contract].BusinessContact(nested) -> SalutationId(loose)

                var step = path.FirstLooseStep;
                if (step == null)
                    throw new MojenException("A selectable view property must have a loose reference in its path.");

                if (step.TargetProp == null)
                    throw new MojenException("The navigation path must end after the first loose reference.");

                return BuildForeignKeyInfo(vprop, step);
            }

            var targetType = isEntity ? path.TargetType.StoreOrSelf : path.TargetType;
            var targetDisplayProp = isEntity ? path.TargetProp.StoreOrSelf : path.TargetProp;
            string display = "";

            if (column)
            {
                // Use the types display name.
                display = targetType.DisplayName;
            }
            else if (isGroupedByTarget)
            {
                display = targetDisplayProp.DisplayLabel;
            }
            else
            {
                // Use navigation steps for display.

                display = path.Steps
                    .Select(x => x.TargetType.DisplayName)
                    // Add target display prop if not pick-display.
                    .AddIfNotDefault(() => targetDisplayProp.IsPickDisplay ? null : targetDisplayProp.DisplayLabel)
                    .Distinct()
                    .Join(" - ");
            }

            return new MojViewPropInfo
            {
                IsForeign = true,
                ForeignDepth = path.Steps.Count,
                ViewProp = vprop,
                PropPath = path.TargetPath,
                PropAliasPath = path.TargetAliasPath,
                // NOTE: Prop and TargetDisplayProp are *equal* in this case.
                Prop = targetDisplayProp,
                TargetDisplayProp = targetDisplayProp,
                TargetType = targetType,
                CustomDisplayLabel = GetCustomDisplayLabel(vprop, display)
            };
        }

        static void CheckPathSteps(MojFormedNavigationPath path)
        {
            int depth = 0;
            foreach (var step in path.Steps)
            {
                if (step.SourceProp.Reference.IsToMany)
                {
                    // KABU TODO: IMPORTANT: Currently disabled due to experiments with MoTag 
                    //if (depth != 0 || !step.SourceProp.Reference.IsIdependent)
                    //    throw new MojenException("Collections in the navigation path are only alloed if they are native and independent.");
                }
                else if (!step.SourceProp.Reference.IsToOne)
                    throw new MojenException("Only references with multiplicity One or OneOrZero are allowed at this step in the navigation path.");

                depth++;
            }
        }

        static MojViewPropInfo BuildForeignKeyInfo(MojViewProp vprop, MojFormedNavigationPathStep step)
        {
            var depth = vprop.FormedNavigationTo.Steps.IndexOf(step) + 1;
            var isEntity = vprop.DeclaringType.IsEntity();

            var targetDisplayProp = isEntity ? step.TargetProp.StoreOrSelf : step.TargetProp;
            var targetType = isEntity ? step.TargetType.StoreOrSelf : step.TargetType;

            return new MojViewPropInfo
            {
                IsForeignKey = true,
                IsForeign = depth > 1,
                ForeignDepth = depth,
                ViewProp = vprop,
                PropPath = step.SourceProp.GetFormedForeignKeyPath(),
                PropAliasPath = step.SourceProp.GetFormedForeignKeyPath(true),
                // NOTE: Prop and TargetDisplayProp are *not* equal in this case.
                Prop = step.SourceProp,
                TargetDisplayProp = targetDisplayProp,
                TargetType = targetType,

                // If selector property then display the target type's display name.
                CustomDisplayLabel = GetCustomDisplayLabel(vprop, targetType.DisplayName)
            };
        }

        static string GetCustomDisplayLabel(MojViewProp vprop, string label)
        {
            if (vprop.DisplayLabel != null)
            // TODO: REMOVE: Always use the configured DisplayLabel.
            // && vprop.DisplayLabel != vprop.OrigTargetProp.DisplayLabel)
            {
                label = vprop.DisplayLabel;
            }
            else if (!string.IsNullOrEmpty(label) && label != vprop.DisplayLabel)
            {
                // NOP
            }
            else if (string.IsNullOrEmpty(label) && string.IsNullOrEmpty(vprop.DisplayLabel))
            {
                label = vprop.Name;
            }

            return label;
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojViewPropInfo
    {
        [DataMember]
        public MojViewProp ViewProp { get; set; }

        [DataMember]
        public MojProp Prop { get; set; }

        [DataMember]
        public string PropPath { get; set; }

        [DataMember]
        public string PropAliasPath { get; set; }

        [DataMember]
        public MojType TargetType { get; set; }

        [DataMember]
        public MojProp TargetDisplayProp { get; set; }

        [DataMember]
        public bool IsNative { get; set; }

        [DataMember]
        public int ForeignDepth { get; set; } = 0;

        [DataMember]
        public bool IsForeignKey { get; set; }

        [DataMember]
        public bool IsForeign { get; set; }

        [DataMember]
        public string CustomDisplayLabel { get; set; }

        public string EffectiveDisplayLabel
        {
            get { return CustomDisplayLabel ?? ViewProp.DisplayLabel; }
        }

        public override string ToString()
        {
            return PropPath;
        }
    }

    public enum MojFontWeight
    {
        Normal = 0,
        Bold = 1,
    }

    public static class MojViewModeExtensions
    {
        public static IEnumerable<MojViewMode> GetAtomicFlags(this MojViewMode value)
        {
            var items = new List<MojViewMode>();

            if (value.HasFlag(MojViewMode.Create))
                items.Add(MojViewMode.Create);

            if (value.HasFlag(MojViewMode.Read))
                items.Add(MojViewMode.Read);

            if (value.HasFlag(MojViewMode.Update))
                items.Add(MojViewMode.Update);

            return items;
        }
    }

    [Flags]
    public enum MojViewMode
    {
        None = 0,
        Create = 1 << 0,
        Read = 1 << 1,
        Update = 1 << 2,
        CreateUpdate = Create | Update,
        All = Create | Read | Update
    }

    public class MojSnippetEditorConfig : MojBase
    {
        public MojViewConfig View { get; set; }
    }

    public class MojPropSnippetsConfig
    {
        public static readonly MojPropSnippetsConfig None = new MojPropSnippetsConfig { Is = false };

        public bool Is { get; set; }

        //public string Name { get; set; }

        public Dictionary<string, object> Args { get; set; } = new Dictionary<string, object>();
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojViewProp : MojProp
    {
        public MojViewProp(MojViewConfig view, MojProp model)
        {
            View = view;
            Model = model;
            Name = Model.Name;
            AutoCompleteFilter = new MojPropAutoCompleteFilter();
        }

        public MojViewConfig View { get; set; }

        public MojDateTimeInfo DisplayDateTime { get; set; }

        public MojProp Model { get; private set; }

        public MojProp OrigTargetProp { get; set; }

        public MojViewProp ColorProp { get; set; }

        public int Position { get; set; }

        public int? Width { get; set; }

        // TODO: REMOVE: public MojProp ColorProp { get; set; }

        public bool IsHtml { get; set; }

        public string UseCodeRenderer { get; set; }

        public bool IsRenderedHtml { get; set; }

        public bool IsLookupDistinct { get; set; }

        public bool IsLinkToInstance { get; set; }

        public MexExpressionNode Predicate { get; set; }

        public MojPropAutoCompleteFilter AutoCompleteFilter { get; private set; }

        public MojCascadeFromConfigCollection CascadeFrom { get; set; } = MojCascadeFromConfigCollection.None;

        public bool IsHeader { get; set; }

        public bool IsLoggedInPerson { get; set; }

        public bool IsInputOnly { get; set; }

        public bool IsSelector { get; set; }
        public bool IsTagsSelector { get; set; }

        public GeoPlaceLookupConfig GeoPlaceLookup { get; set; } = GeoPlaceLookupConfig.None;

        public MojStringSubstringConfig StringSubstring { get; set; } = MojStringSubstringConfig.None;

        public string CustomTemplateName { get; set; }
        public string CustomTemplatePropPath { get; set; }
        public string CustomEditorViewName { get; set; }
        public string CustomViewName { get; set; }

        public MojLookupViewPropConfig Lookup { get; set; } = MojLookupViewPropConfig.None;

        public MojLookupCascadeFromScopeViewPropConfig CascadeFromScope { get; set; } = MojLookupCascadeFromScopeViewPropConfig.None;

        public MojViewConfig LookupDialog { get; set; }

        public MojPropSnippetsConfig Snippets { get; set; } = MojPropSnippetsConfig.None;

        public bool IsDataFilterText { get; set; }

        public MojViewMode HideModes { get; set; } = MojViewMode.None;

        public bool IsExternal { get; set; }

        public bool NoLabel { get; set; }

        public MojFontWeight FontWeight { get; set; }

        public int VisiblePosition { get; set; }

        /// <summary>
        /// In case of collection properties this will hold the collection item view.
        /// </summary>
        public MojViewConfig ContentView { get; set; }

        public string GetRemoveOnMarkerClasses()
        {
            return HideModes.GetAtomicFlags()
            .Select(mode => "remove-on-" + mode)
            .Join(" ");
        }

        /// <summary>
        /// Applies server side ordering regardless of the client's request.
        /// TODO: Implement CeqOrder
        /// </summary>
        public int OrderByIndex { get; set; }

        public override string ToString()
        {
            return FormedTargetPath;
        }
    }

    public class MojLookupViewPropConfig : MojBase
    {
        public static readonly MojLookupViewPropConfig None = new MojLookupViewPropConfig();

        public bool Is { get; set; }

        public MojType TargetType { get; set; }

        public string ViewId { get; set; }

        public string ViewGroup { get; set; }

        public string ViewAlias { get; set; }
    }

    public class MojLookupCascadeFromScopeViewPropConfig : MojBase
    {
        public static readonly MojLookupCascadeFromScopeViewPropConfig None = new MojLookupCascadeFromScopeViewPropConfig { Is = false };

        public MojLookupCascadeFromScopeViewPropConfig()
        {
            Is = true;
        }

        public bool Is { get; set; }

        public MojProp SourceProp { get; set; }

        public MojProp TargetProp { get; set; }
    }

    public class MojPropAutoCompleteFilter
    {
        public string PropName { get; set; }

        public bool IsEnabled { get; set; }

        public int Position { get; set; }
    }
}