using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    public enum MojTypeKind
    {
        Entity = 0,
        Model = 1,
        Enum = 2,
        Complex = 3,
        Interface = 4
    }

    public static class MojTypePropsExtensions
    {
        public static IEnumerable<MojProp> OrderByDerivedFirst(this IEnumerable<MojProp> props)
        {
            var types = props.Select(x => x.DeclaringType).DistinctBy(x => x).OrderBy(x => x, DerivedFirstComp).ToArray();

            foreach (var type in types)
                foreach (var prop in props.Where(x => x.DeclaringType == type))
                    yield return prop;
        }

        public static IEnumerable<Tuple<MojType, MojProp>> OrderByDerivedFirst(this IEnumerable<Tuple<MojType, MojProp>> items)
        {
            var types = items.Select(x => x.Item1).DistinctBy(x => x).OrderBy(x => x, DerivedFirstComp).ToArray();

            foreach (var type in types)
                foreach (var item in items.Where(x => x.Item1 == type))
                    yield return item;
        }

        static readonly TypeDerivedFirstComp DerivedFirstComp = new TypeDerivedFirstComp();

        class TypeDerivedFirstComp : IComparer<MojType>
        {
            public int Compare(MojType x, MojType y)
            {
                return x == y ? 0 : x.Is(y) ? -1 : 1;
            }
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojType : MojPartBase
    {
        static readonly PluralizationService _pluralizer = PluralizationService.CreateService(CultureInfo.GetCultureInfo("en"));

        public static string Pluralize(string text)
        {
            if (TryPluralize("Info", ref text)) return text;
            if (TryPluralize("Kind", ref text)) return text;

            return _pluralizer.Pluralize(text);
        }

        static bool TryPluralize(string endsWith, ref string text)
        {
            if (!text.EndsWith(endsWith))
                return false;

            text = text + "s"; // text.Substring(0, text.LastIndexOf(endsWith)) + endsWith + "s";

            return true;
        }

        public static MojType CreateEntity(string name)
        {
            var entity = new MojType(name);
            entity.Kind = MojTypeKind.Entity;

            return entity;
        }

        public static MojType CreateModel(string name)
        {
            var entity = new MojType(name);
            entity.Kind = MojTypeKind.Model;
            entity.NoDataContract = true;

            return entity;
        }

        public static MojType CreateComplexType(string name)
        {
            var entity = new MojType(name);
            entity.Kind = MojTypeKind.Complex;

            return entity;
        }

        public static MojType CreateEnum(string name)
        {
            var enu = new MojType(name);
            enu.Kind = MojTypeKind.Enum;
            enu.IsObservable = false;
            enu.TableName = null;

            return enu;
        }

        public static MojType CreateInterface(string name)
        {
            var iface = new MojType(name);
            iface.Kind = MojTypeKind.Interface;
            iface.IsObservable = false;
            iface.TableName = null;

            return iface;
        }

        static MojType()
        {
            AutoMapper.Mapper.Initialize(c =>
                c.CreateMap(typeof(MojType), typeof(MojType)));
        }

        MojType()
        { }

        MojType(string name)
        {
            InitName(name);
            ClassName = name;
        }

        public MojType[] AccessPath { get; set; }

        public void InitName(string name)
        {
            Name = name;
            DisplayName = name;
            InitPluralName(Pluralize(name));
        }

        public void InitPluralName(string pluralName)
        {
            PluralName = pluralName;
            TableName = pluralName;
        }

        public bool WasGenerated { get; set; }

        [DataMember]
        public Guid? Id { get; set; }

        [DataMember]
        public MojTypeKind Kind { get; set; }

        public bool IsOfKind(params MojTypeKind[] kinds)
        {
            return kinds.Contains(Kind);
        }

        [DataMember]
        public string OutputDirPath { get; set; }

        [DataMember]
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                _vName = MojenUtils.FirstCharToLower(value);
            }
        }
        string _name;

        /// <summary>
        /// Name to be used for variables (first char of name to lower).
        /// </summary>
        public string VName
        {
            get { return _vName; }
        }
        string _vName;

        [DataMember]
        public string PluralName { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public string DisplayPluralName { get; set; }

        [DataMember]
        public string ClassName { get; set; }

        public string VClassName
        {
            get { return MojenUtils.FirstCharToLower(ClassName); }
        }

        public string QualifiedClassName
        {
            get
            {
                return "" + (string.IsNullOrWhiteSpace(Namespace) ? "" : Namespace + ".") + ClassName;
            }
        }

        [DataMember]
        public string BaseClassName { get; set; }

        [DataMember]
        public MojType BaseClass { get; set; }

        public bool HasBaseClass
        {
            get { return BaseClass != null || BaseClassName != null; }
        }

        public string EffectiveBaseClassName
        {
            get { return BaseClass != null ? BaseClass.ClassName : BaseClassName; }
        }

        /// <summary>
        /// Only applicable to enums.
        /// </summary>
        [DataMember]
        public bool IsFlagsEnum { get; set; }

        [DataMember]
        public bool NoDataContract { get; set; }

        /// <summary>
        /// Information about a previous type version. Used to generate mapping code
        /// between this and the previous type version (e.g. for data migration scenarios).
        /// </summary>
        [DataMember]
        public MojVersionMapping VerMap { get; set; } = MojVersionMapping.None;

        [DataMember]
        public List<MojInterface> Interfaces { get; private set; } = new List<MojInterface>();

        [DataMember]
        public bool IsTenant { get; set; }

        [DataMember]
        public bool IsODataOpenType { get; set; }

        [DataMember]
        public string Namespace { get; set; }

        [DataMember]
        public string DataContextName { get; set; }

        [DataMember]
        public bool? HasManyParents { get; set; }

        [DataMember]
        public bool ExistsAlready { get; set; }

        [DataMember]
        public MojSummaryConfig Summary { get; private set; } = new MojSummaryConfig();

        [DataMember]
        public MojAttrs Attrs { get; private set; } = new MojAttrs();

        [DataMember]
        public List<MiaTypeTriggerConfig> Triggers { get; private set; } = new List<MiaTypeTriggerConfig>();

        [DataMember]
        internal List<MojProp> LocalProps { get; private set; } = new List<MojProp>();

        [DataMember]
        public List<MojSoftReference> SoftReferences { get; set; } = new List<MojSoftReference>();

        [DataMember]
        public MojAssignFromCollectionConfig AssignFromConfig = MojAssignFromCollectionConfig.None;

        internal void AddLocalProp(MojProp prop)
        {
            prop.DeclaringType = this;
            LocalProps.Add(prop);
        }

        internal MojProp OverrideLocalProp(MojProp prop, MojProp inheritedProp, bool @sealed)
        {
            var index = LocalProps.IndexOf(prop);
            LocalProps.Remove(prop);

            prop = inheritedProp.Clone();
            prop.DeclaringType = this;
            prop.IsOverride = true;
            prop.IsVirtual = false;
            prop.IsSealed = @sealed;

            LocalProps.Insert(index, prop);

            return prop;
        }

        public IEnumerable<MojProp> GetLocalProps(bool custom = true)
        {
            return LocalProps.Where(x => (custom || !x.IsCustom));
        }

        public IEnumerable<MojProp> GetLocalTrackedProps()
        {
            if (Kind != MojTypeKind.Model && Kind != MojTypeKind.Complex)
                throw new MojenException("This operation is intended only for model and complex types.");

            return GetLocalProps().Where(x =>
                x.IsTracked &&
                // Ignore navigation properties.
                !x.IsNavigation);
        }

        public IEnumerable<MojDbPropAnnotation> GetIndexesWhereIsMember(MojProp prop)
        {
            return GetProps().Where(x =>
                 x.DbAnno.Index.Is &&
                !x.DbAnno.Sequence.IsDbSequence &&
                 (x == prop || x.DbAnno.Unique.IsMember(prop)))
                 .Select(x => x.DbAnno);
        }

        [DataMember]
        public MojPickConfig LocalPick { get; set; }

        /// <summary>
        /// Returns the nearest MojPickItem definition of the self-or-ancestor class axis.
        /// </summary>
        public MojPickConfig FindPick()
        {
            return SelectAncestorOrSelf(cur => First(cur.LocalPick));
        }

        public bool HasCreateOnInitProps
        {
            get { return GetCreateOnInitProps().Any(); }
        }

        public IEnumerable<MojProp> GetCreateOnInitProps()
        {
            return GetProps().Where(x =>
                (x.Type.IsCollection && !x.IsNavigation) ||
                (x.Type.Type != null &&
                 !x.Type.IsString &&
                 !x.Type.Type.IsPrimitive &&
                 !x.Type.Type.IsValueType)
                );
        }

        public IEnumerable<MojProp> NonArrayCollectionProps
        {
            get { return GetProps().Where(x => x.Type.IsCollection && !x.Type.IsArray); }
        }

        public MojProp Guid
        {
            get { return CheckPropNotNull("Guid", FindGuid()); }
        }

        public MojProp FindGuid()
        {
            return GetProps().FirstOrDefault(x => x.IsGuidKey);
        }

        public MojProp Key
        {
            get { return CheckPropNotNull("Key", FindKey()); }
        }

        public MojProp GetDeletedMarker(params MojPropDeletedMarker[] kinds)
        {
            return CheckPropNotNull("DeletedMarker", FindDeletedMarker(kinds));
        }

        public MojProp FindDeletedMarker(params MojPropDeletedMarker[] kinds)
        {
            if (kinds == null || kinds.Length == 0)
                throw new ArgumentException("The kinds of deleted markers must be specified.", nameof(kinds));

            var props = GetProps().Where(x => x.DeletedMarker.Is).ToArray();
            foreach (var kind in kinds)
            {
                var prop = props.FirstOrDefault(x => x.DeletedMarker.Kind == kind);
                if (prop != null)
                    return prop;
            }

            return null;
        }

        public MojProp FindKey()
        {
            return GetProps().FirstOrDefault(x => x.IsKey);
        }

        public MojProp TenantKey
        {
            get { return CheckPropNotNull("TenantKey", FindTenantKey()); }
        }

        public MojProp FindTenantKey()
        {
            return GetProps().FirstOrDefault(x => x.IsTenantKey);
        }

        public MojProp FindReferenceWithForeignKey(MojType to, bool required = false)
        {
            var prop = GetProps().Where(x =>
                x.Reference.ForeignKey != null &&
                x.Reference.IsToOne &&
                (x.Reference.ToType == to || x.Reference.ToType == to.Store))
                // Prefer navigation property over foreign key property.
                .Select(x => x.NavigationOrSelf)
                .FirstOrDefault();

            if (prop == null && required)
                throw new MojenException($"No foreign key reference found to type '{to.ClassName}'.");

            return prop;
        }

        public IEnumerable<MojProp> GetReferenceProps(MojReferenceBinding? binding = null, MojMultiplicity? multiplicity = null)
        {
            return GetProps()
                .Where(x =>
                    x.Reference.Is &&
                    (binding == null || x.Reference.Binding.HasFlag(binding.Value)) &&
                    (multiplicity == null || x.Reference.Multiplicity.HasFlag(multiplicity.Value)))
                .ToList()
                // Remove foreign key props if the navigation prop was also included.
                .Exclude((list, x) => x.IsForeignKey && list.Any(y => y.Reference.ForeignKey == x && y.IsNavigation))
                // Prefer navigation property over foreign key property.
                .Select(x => x.NavigationOrSelf);
        }

        public MojProp FindReferenceWithForeignKey(string to, bool required = false)
        {
            var prop = GetProps().Where(x =>
                x.Reference.ForeignKey != null &&
                x.Reference.IsToOne &&
                (x.Reference.ToType.Name == to || x.Reference.ToType.StoreOrSelf.Name == to))
                // Prefer navigation property over foreign key property.
                .Select(x => x.NavigationOrSelf)
                .FirstOrDefault();

            if (prop == null && required)
                throw new MojenException($"No foreign key reference found to type '{to}'.");

            return prop;
        }

        public bool Is(MojType type)
        {
            return TestAncestorOrSelf(cur => cur == type);
        }

        public IEnumerable<MojProp> GetOwnedByRefProps()
        {
            return GetReferenceProps().Where(x => x.Reference.OwnedByProp != null);
        }

        public IEnumerable<MojProp> GetBackReferencePropsTo(MojType type)
        {
            return GetReferenceProps().Where(x => x.Reference.IsChildToParent && x.Reference.ToType == type);
        }

        MojProp CheckPropNotNull(string propName, MojProp prop)
        {
            if (prop == null)
                throw new MojenException(string.Format("The type '{0}' does not have a {1} property.", Name, propName));

            return prop;
        }

        [DataMember]
        public bool IsKeyAccessible { get; set; }

        [DataMember]
        public bool IsEnumEntity { get; set; }

        public bool IsKeyAccessibleEffective()
        {
            return TestAncestorOrSelf(cur => cur.IsKeyAccessible);
        }

        public bool IsDerivedFromStoreWrapper
        {
            get { return TestAncestorOrSelf(cur => cur != this && cur.IsStoreWrapper); }
        }

        public bool IsODataOpenTypeEffective
        {
            get { return TestAncestorOrSelf(cur => cur.IsODataOpenType); }
        }

        [DataMember]
        public bool IsGuidGenerateable { get; set; }

        [DataMember]
        public bool IsMultitenant { get; set; }

        [DataMember]
        public List<MojValueSetContainer> Seedings { get; private set; } = new List<MojValueSetContainer>();

        [DataMember]
        public List<MojProp> ChangeTrackingProps { get; private set; } = new List<MojProp>();

        [DataMember]
        public List<MexExpressionNode> Conditions { get; private set; } = new List<MexExpressionNode>();

        [DataMember]
        public MojType Store { get; set; }

        public MojType GetNearestStore()
        {
            if (Kind == MojTypeKind.Entity)
                return this;
            return SelectAncestorOrSelf(cur => First(cur.Store));
        }

        [DataMember]
        public bool IsStoreOwner { get; set; }

        public MojType StoreOrSelf
        {
            get { return Store ?? this; }
        }

        public MojType RequiredStore
        {
            get
            {
                if (Store != null) return Store;
                if (Kind == MojTypeKind.Entity) return this;
                throw new MojenException("The type is not a store type and has no store type assigned.");
            }
        }

        public void CheckRequiredStore()
        {
            var dummy = RequiredStore;
        }

        /// <summary>
        /// Indicates whether the class in abstract.
        /// If abstract then validation and change tracking properties
        /// will *not* be initialized for this class.
        /// </summary>
        [DataMember]
        public bool IsAbstract { get; set; }

        [DataMember]
        public bool IsSealed { get; set; }

        [DataMember]
        public bool IsObservable { get; set; } = true;

        [DataMember]
        public MojDataSetSizeKind DataSetSize { get; set; } = MojDataSetSizeKind.Normal;

        [DataMember]
        public List<MojTypeComparison> Comparisons { get; private set; } = new List<MojTypeComparison>();

        [DataMember]
        public bool IsStoreWrapper { get; set; }

        public bool CanNavigateReferences
        {
            get { return Kind == MojTypeKind.Entity || (Kind == MojTypeKind.Model && Store != null); }
        }

        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public bool NoValidation { get; set; }

        [DataMember]
        public bool NoChangeTracking { get; set; }

        public bool HasAncestorType(string typeName)
        {
            if (BaseClassName == typeName)
                return true;

            var cur = BaseClass;
            while (cur != null)
            {
                if (cur.ClassName == typeName || cur.BaseClassName == typeName)
                    return true;

                cur = cur.BaseClass;
            }

            return false;
        }

        public bool IsAccessibleFromThis(MojFormedType type)
        {
            return type.FormedNavigationFrom.Root?.SourceType == this;
        }

        public bool IsNativeProp(MojProp prop)
        {
            if (// Either local property
                prop.DeclaringType == this ||
                // or property of ancestor type
                TestAncestorOrSelf(type => type == prop.DeclaringType))
                return true;

            return false;
        }

        public bool IsAccessibleFromThis(MojProp prop)
        {
            if (// Either local property
                prop.DeclaringType == this ||
                // or property of ancestor type
                TestAncestorOrSelf(type => type == prop.DeclaringType) ||
                // or navigated to from this type
                prop.FormedNavigationFrom.Root?.SourceType == this)
                return true;

            return false;
        }

        public bool IsForeign(MojProp prop)
        {
            if (prop.FormedNavigationFrom.Is && prop.FormedNavigationFrom.TargetType != this)
                return true;

            if (prop.DeclaringType == this || TestAncestorOrSelf(type => type == prop.DeclaringType))
                return false;

            return true;
        }

        public MojProp GetNavigationPropFor(MojProp foreignProp)
        {
            var naviProp = GetProps()
                .FirstOrDefault(x =>
                    x.IsNavigation &&
                    x.Reference.ToType == foreignProp.DeclaringType);

            if (naviProp == null)
                throw new MojenException($"Navigation property not found for foreign property '{foreignProp}'.");

            return naviProp;
        }

        public MojProp FindStoreProp(string name)
        {
            var prop = FindProp(name);
            if (prop == null)
                return null;

            if (Kind == MojTypeKind.Entity)
                return prop;

            if (GetNearestStore() != null)
            {
                var sprop = GetNearestStore().FindProp(name);
                if (sprop != null)
                    return sprop;
            }

            if (BaseClass != null && prop.ProxyOfInheritedProp != null)
                return BaseClass.FindStoreProp(prop.ProxyOfInheritedProp);

            return null;
        }

        public void CheckPropExists(string name)
        {
            GetProp(name);
        }

        public MojProp GetProp(string name)
        {
            var prop = FindProp(name);
            if (prop == null)
                throw new MojenException(string.Format("Property '{0}' not found on type '{1}'.", name, ClassName));

            return prop;
        }

        public MojProp GetPickDisplayProp(bool required = true)
        {
            var pick = FindPick();
            if (pick == null)
            {
                if (required)
                    throw new MojenException($"The type '{ClassName}' does no pick-display property defined.");

                return null;
            }

            return GetProp(pick.DisplayProp);
        }

        public MojProp FindProp(string name)
        {
            return GetProps().FirstOrDefault(x => x.Name == name);
        }

        public IEnumerable<MojOrderConfig> GetOrderBy()
        {
            return GetProps().Where(x => x.InitialSort.Is).Select(x => x.InitialSort).ToArray();
        }

        public IEnumerable<MojProp> GetExposableSchemaProps()
        {
            return GetProps()
                .Where(x =>
                    // Never expose the TenantId to the outside world.
                    !x.IsTenantKey &&
                    // Foreign properties are not part of the type's schema.
                    !IsForeign(x))
                .ToArray();
        }

        public IEnumerable<MojProp> GetProps(bool custom = true, bool overriden = false)
        {
            return TraverseProps(null, custom: custom, overriden: overriden).Select(c => c.Item2).ToArray();
        }

        // KABU TODO: REMOVE? Not used
        //public IEnumerable<Tuple<MojType, MojProp>> GetPropsWithObjects(bool inherited = true, bool custom = false, bool overriden = false, bool hidden = false)
        //{
        //    return TraverseProps(null, inherited, custom, overriden, hidden).ToArray();
        //}

        protected IEnumerable<Tuple<MojType, MojProp>> TraverseProps(
            List<MojProp> descendantProps, bool hidden = true, bool custom = true, bool inherited = true, bool overriden = false)
        {
            if (descendantProps == null)
                descendantProps = new List<MojProp>();
            if (inherited && BaseClass != null)
            {
                var localProps = new List<MojProp>(LocalProps);
                var props = descendantProps.ToList();
                props.AddRange(localProps);
                foreach (var item in BaseClass.TraverseProps(props, hidden, custom, inherited, overriden))
                    yield return item;
            }

            foreach (MojProp prop in LocalProps)
            {
                var descendantProp = descendantProps.FirstOrDefault(x => x.Name == prop.Name);
                if (descendantProp != null)
                {
                    if (!descendantProp.IsNew && !descendantProp.IsOverride)
                        throw new MojenException(
                            string.Format("Duplicate property '{0}' in ancestor axis, but override/new was not specified.", descendantProp.Name));

                    if (!descendantProp.IsNew && !prop.IsVirtual)
                        throw new MojenException(
                            string.Format("Duplicate property '{0}' in descendant axis, but virtual was not specified.", prop.Name));

                    if (!overriden)
                        continue;
                }

                if (prop.IsCustom && !custom)
                    continue;

                if (prop.IsHiddenCollectionNavigationProp && !hidden)
                    continue;

                yield return Tuple.Create(this, prop);
            }
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            // NOP
        }

        public override string ToString()
        {
            return Name + " [" + Kind + "]";
        }

        // Helpers ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        bool TestAncestorOrSelf(Func<MojType, bool> predicate)
        {
            var cur = this;
            do
            {
                if (predicate(cur))
                    return true;

                cur = cur.BaseClass;
            }
            while (cur != null);

            return false;
        }

        T SelectAncestorOrSelf<T>(Func<MojType, Tuple<T, bool>> predicate)
        {
            var cur = this;
            do
            {
                var result = predicate(cur);
                if (!result.Item2)
                    return result.Item1;

                cur = cur.BaseClass;
            }
            while (cur != null);

            return default(T);
        }

        Tuple<T, bool> First<T>(T item)
            where T : class
        {
            return item != null ? Tuple.Create(item, false) : Tuple.Create<T, bool>(null, true);
        }
    }
}