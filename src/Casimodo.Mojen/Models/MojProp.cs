using System.Runtime.Serialization;
using Casimodo.Lib.Data;
using System.Reflection;
using System.Collections;

namespace Casimodo.Mojen
{
    public class MapFromModelAttribute : Attribute
    {
        public bool MapContent { get; set; }

        public bool Clone { get; set; }
    }

    [Flags]
    public enum MojPropDeletedMarker
    {
        None = 0,
        Effective = 1 << 0,
        Self = 1 << 1,
        Cascade = 1 << 2,
        RecycleBin = 1 << 3
    }

    public class MojPropDeletedMarkerConfig : MojBase
    {
        public static readonly MojPropDeletedMarkerConfig None = new() { Is = false };

        public MojPropDeletedMarkerConfig()
        {
            Is = true;
        }

        public bool Is { get; private set; }

        public MojPropDeletedMarker Kind { get; set; }
    }

    [Flags]
    public enum MojPropGetSetOptions
    {
        None = 0,
        Public = 1 << 0,
        Protected = 1 << 1,
        ProtectedInternal = 1 << 2,
        Internal = 1 << 3,
        Private = 1 << 4
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojPropRules : MojBase
    {
        [DataMember]
        public int? MinLength { get; set; }

        [DataMember]
        public int? MaxLength { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojProp : MojBase
    {
        public const int DefaultMillisecondDigits = 3;

        public MojProp()
        {
            Id = Guid.NewGuid();
            IsEditable = true;
            IsTracked = true;
            IsObservable = true;
            IsSortable = true;
            Type = new MojPropType
            {
                DeclaringProp = this
            };
            InitReferenceProps();
        }

        void InitReferenceProps()
        {
            Attrs = new MojAttrs();
            Summary = new MojSummaryConfig();
            ForeignKeyConditions = new List<MexExpressionNode>();
            AutoRelatedProps = new List<MojProp>();
            CascadeFromProps = new List<MojProp>();
        }

        public MojProp Clone()
        {
            var clone = (MojProp)MemberwiseClone();

            clone.InitReferenceProps();

            clone.Attrs.AddRange(Attrs);
            clone.Summary.AssignFrom(Summary);
            clone.ForeignKeyConditions.AddRange(ForeignKeyConditions);
            clone.AutoRelatedProps.AddRange(AutoRelatedProps);
            clone.CascadeFromProps.AddRange(CascadeFromProps);

            return clone;
        }

        public static MojProp CloneModelToEntity(MojProp mprop)
        {
            return AssignModelToEntity(mprop, new MojProp());
        }

        public static MojProp AssignModelToEntity(MojProp source, MojProp target)
        {
            if (!source.IsModel())
                throw new MojenException("A model property was expected.");

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Properties
            foreach (var p in typeof(MojProp).GetProperties(flags)
                .Select(p => new
                {
                    Prop = p,
                    Mapping = p.GetCustomAttribute<MapFromModelAttribute>()
                })
                .Where(p => p.Mapping != null))
            {
                var value = p.Prop.GetValue(source);

                // Convert property value.

                if (value != null)
                {
                    if (p.Mapping.MapContent)
                    {
                        if (value is IEnumerable)
                        {
                            if (value is not IList listValue)
                                throw new MojenException($"Cannot convert enumerable type '{p.Prop.PropertyType.Name}' to list of entitiy items.");

                            var listValueCopy = (IList)Activator.CreateInstance(p.Prop.PropertyType);
                            foreach (var item in listValue)
                            {
                                var entityItem = ConvertToEntity(source, target, item);
                                if (entityItem != null)
                                    listValueCopy.Add(entityItem);
                            }
                            value = listValueCopy;
                        }
                        else
                        {
                            value = ConvertToEntity(source, target, value);
                        }
                    }
                    else if (p.Mapping.Clone)
                    {
                        if (value is IMojCloneableConfig cloneableConfigValue)
                        {
                            value = cloneableConfigValue.Clone();
                        }
                        else
                        {
                            if (value is not ICloneable clonableValue)
                                throw new MojenException($"The type '{p.Prop.PropertyType.Name}' must be cloneable.");

                            value = clonableValue.Clone();
                        }
                    }
                }

                p.Prop.SetValue(target, value);
            }

            return target;
        }

        static object ConvertToEntity(MojProp source, MojProp entity, object item)
        {
            if (item == null)
                return null;

            if (item is MojType type)
                return type.IsEntity() ? type : type.RequiredStore;

            if (item is MojPropType propType)
                return propType.IsModel() ? propType.Clone().ConvertToEntity() : propType;

            if (item is MojProp prop)
                return prop.IsEntity() ? prop : prop.RequiredStore;

            if (item is MojDefaultValuesConfig dvalue)
            {
                return dvalue;
            }

            if (item is MojReference reference)
            {
                if (reference == MojReference.None)
                    return reference;

                if (!reference.IsRelatedToModel())
                    return reference;

                return reference.CloneToEntity(source, entity);
            }

            if (item is MojFormedNavigationPath path)
            {
                if (path != MojFormedNavigationPath.None)
                    throw new MojenException($"Formed navigation paths are not expected to be used at data layer level.");
                return path;
            }

            throw new MojenException($"Failed to convert item '{item.GetType().Name}' to an entity item.");
        }

        public MojProp Initialize(string name)
        {
            SetName(name);

            return this;
        }

        public void SetName(string name)
        {
            // KABU TODO: REMOVE: There are cases where we don't know the name at this stage.
            // if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");

            Name = name;
            Alias = name;
            if (!string.IsNullOrWhiteSpace(name))
                FieldName = "_" + Moj.FirstCharToLower(name);
        }

        [MapFromModel]
        [DataMember]
        public Guid? Id { get; set; }

        [MapFromModel]
        [DataMember]
        public bool IsKey { get; set; }

        [MapFromModel]
        [DataMember]
        public bool IsGuidKey { get; set; }

        [MapFromModel]
        [DataMember]
        public bool IsTenantKey { get; set; }

        [MapFromModel]
        [DataMember]
        public MojPropDeletedMarkerConfig DeletedMarker { get; set; } = MojPropDeletedMarkerConfig.None;

        [MapFromModel]
        [DataMember]
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                _vName = Moj.FirstCharToLower(value);
            }
        }
        internal string _name;

        /// <summary>
        /// Name for variable (first char lower case).
        /// </summary>
        public string VName
        {
            get { return _vName; }
        }
        internal string _vName;

        [MapFromModel]
        [DataMember]
        public string Alias { get; set; }

        [MapFromModel]
        [DataMember]
        public string SingleName { get; set; }

        /// <summary>
        /// Only for reference navigation properties.
        /// If true then this navigation prop definition will *not* produce a
        /// property at all, but is just used/needed by the Mojen machinery.
        /// </summary>
        [MapFromModel]
        [DataMember]
        public bool IsHiddenCollectionNavigationProp { get; set; }

        /// <summary>
        /// E.g. when building a reference, a single builder instruction will
        /// automatically create additional related properties such as
        /// "MyPropertyId", "MyPropertyUri", etc.
        /// Those are put into AutoRelatedProps for post-processing.
        /// </summary>        
        [DataMember]
        public List<MojProp> AutoRelatedProps { get; internal set; }

        public bool IsAutoRelated { get; set; }

        [MapFromModel(MapContent = true)]
        [DataMember]
        public List<MojProp> CascadeFromProps { get; internal set; }

        /// <summary>
        /// Information about a previous version of this property.
        /// Used for generation of mappings for data migration scenarios.
        /// </summary>
        [MapFromModel]
        [DataMember]
        public MojVersionMapping VerMap { get; set; } = MojVersionMapping.None;

        [MapFromModel]
        [DataMember]
        public string FieldName { get; set; }

        /// <summary>
        /// The MojType which contains (i.e. has declared) this property.
        /// This is not the derived type if this property was inherited.
        /// KABU TODO: VERY IMPORTANT: We also need the derived type if this property is inherited.
        /// </summary>
        [MapFromModel(MapContent = true)]
        [DataMember]
        public MojType DeclaringType { get; set; }

        [MapFromModel(MapContent = true)]
        [DataMember]
        public MojPropType Type { get; set; }

        [MapFromModel]
        [DataMember]
        public List<string> OnChangeRaiseProps { get; set; } = new List<string>();

        [MapFromModel(Clone = true)]
        [DataMember]
        public MojPropValueConstraints Rules { get; set; } = MojPropValueConstraints.None;

        internal MojPropValueConstraints UseRules()
        {
            return Rules.Is ? Rules : (Rules = new MojPropValueConstraints());
        }

        /// <summary>
        /// Only applicable to string properties.
        /// E.g. turns off/on the spellcheck in HTML textareas. Default: false.
        /// </summary>
        [MapFromModel]
        [DataMember]
        public bool IsSpellCheck { get; set; }

        [MapFromModel]
        [DataMember]
        public bool IsODataDynamicPropsContainer { get; set; }

        /// <summary>
        /// The value of the enum member. Only used by enums.
        /// </summary>
        [MapFromModel]
        [DataMember]
        // KABU TODO: Maybe move to the prop type?
        public int? EnumValue { get; set; }

        [MapFromModel(MapContent = true)]
        [DataMember]
        public MojDefaultValuesConfig DefaultValues { get; set; } = MojDefaultValuesConfig.None;

        public void AddDefaultValue(object value, string scenario = null)
        {
            EnsureDefaultValues().Set(value, scenario);
        }

        public void AddDefaultValue(MojDefaultValueCommon value, string scenario = null)
        {
            EnsureDefaultValues().Set(value, scenario);
        }

        public void AddDefaultValue(MojDefaultValueAttr attr, string scenario = null)
        {
            EnsureDefaultValues().Set(attr, scenario);
        }

        MojDefaultValuesConfig EnsureDefaultValues()
        {
            if (!DefaultValues.Is)
                DefaultValues = new MojDefaultValuesConfig();

            return DefaultValues;
        }

        [MapFromModel]
        [DataMember]
        public string ProxyOfInheritedProp { get; set; }

        [MapFromModel]
        [DataMember]
        // KABU TODO: Maybe move to prop type.
        public bool IsColor { get; set; }

        [MapFromModel]
        [DataMember]
        // KABU TODO: Maybe move to prop type.
        public bool IsColorWithOpacity { get; set; }

        [MapFromModel]
        [DataMember]
        public string DisplayLabel { get; set; }

        [MapFromModel]
        [DataMember]
        public bool IsPickDisplay { get; set; }

        [MapFromModel]
        [DataMember]
        public bool UseColor { get; set; }

        /// <summary>
        /// For multiline text: number of editor rows.
        /// </summary>
        [MapFromModel]
        [DataMember]
        public int RowCount { get; set; }

        /// <summary>
        /// For multiline text: number of editor cols.
        /// </summary>
        [MapFromModel]
        [DataMember]
        public int ColCount { get; set; }

        [DataMember]
        [MapFromModel]
        public MojAttrs Attrs { get; internal set; }

        [DataMember]
        [MapFromModel]
        public MojSummaryConfig Summary { get; internal set; }

        public MojProp ForeignKey
        {
            get { return Reference?.ForeignKey; }
        }

        // KABU TODO: REMOVE? This was intended for polymorphic associations,
        //   which do not work the way we want in EF.
        //public string ForeignKeyOrSelfName
        //{
        //    get { return ForeignKey?.Name ?? Name; }
        //}

        [DataMember]
        [MapFromModel(MapContent = true)]
        public MojReference Reference { get; set; } = MojReference.None;

        [DataMember]
        [MapFromModel]
        public bool IsNavigation { get; set; }

        [DataMember]
        [MapFromModel]
        public bool IsForeignKey { get; set; }

        public bool IsRequiredOnEdit
        {
            get
            {
                return Rules.IsRequired ||
                    Rules.IsLocallyRequired ||
                    //Attrs.Any(x => x.Name == "LocallyRequired") ||
                    !Type.CanBeNull ||
                    (Reference.Is && !Reference.IsToZero);
            }
        }

        public MojProp RequiredNavigation
        {
            get
            {
                var p = NavigationOrSelf;
                if (p.IsNavigation)
                    return p;

                if (!Reference.Is)
                    throw new MojenException($"Property '{Name}' is not a reference property.");

                throw new MojenException($"Navigation property not found for context property '{Name}'.");
            }
        }

        /// <summary>
        /// Returns the navigation property of this reference property or, if none exists, the property itself.
        /// </summary>
        public MojProp Navigation
        {
            get
            {
                if (!Reference.Is || IsNavigation)
                    return this;

                if (Reference.NavigationProp != null)
                    return Reference.NavigationProp;

                foreach (var p in AutoRelatedProps)
                    if (p.IsNavigation)
                        return p;

                return null;
            }
        }

        /// <summary>
        /// Returns the navigation property of this reference property or, if none exists, the property itself.
        /// </summary>
        public MojProp NavigationOrSelf
        {
            get
            {
                if (!Reference.Is || IsNavigation)
                    return this;

                if (Reference.NavigationProp != null)
                    return Reference.NavigationProp;

                foreach (var p in AutoRelatedProps)
                    if (p.IsNavigation)
                        return p;

                return this;
            }
        }

        /// <summary>
        /// Returns the foreign key property of this navigation reference property or, if none exists, the property itself.
        /// </summary>
        public MojProp ForeignKeyOrSelf
        {
            get
            {
                if (!Reference.Is || IsForeignKey)
                    return this;

                if (Reference.ForeignKey != null)
                    return Reference.ForeignKey;

                foreach (var p in AutoRelatedProps)
                    if (p.IsForeignKey)
                        return p;

                return this;
            }
        }

        public bool HasForeignKeyConditions
        {
            get { return _foreignKeyConditions != null && _foreignKeyConditions.Count != 0; }
        }

        [MapFromModel]
        [DataMember]
        // KABU TODO: Move to MojReference.
        public List<MexExpressionNode> ForeignKeyConditions
        {
            get { return _foreignKeyConditions ??= new List<MexExpressionNode>(); }
            set { _foreignKeyConditions = value; }
        }
        List<MexExpressionNode> _foreignKeyConditions;

        /// <summary>
        /// Only used for model properties (not entity properties).
        /// Defines a corresponding entity property.
        /// </summary>
        [DataMember]
        public MojProp Store { get; set; }

        public void SetStore(MojProp sprop)
        {
            Store = sprop;
            if (DeclaringType?.Store != null)
            {
                var p = DeclaringType.Store.FindProp(sprop.Name);
                if (p != null && p != sprop)
                    throw new MojenException("Store property mismatch.");
            }
        }

        /// <summary>
        /// Marks this property for subsequent creation of a store property.
        /// If a store property already exists, then this flag allows for 
        /// subsequent assignment of model's values to the store prop in order to keep the
        /// store prop in sync with the model prop.
        /// </summary>
        internal bool IsStorePending { get; set; }

        public MojProp StoreOrSelf
        {
            get { return Store ?? this; }
        }

        public MojProp RequiredStore
        {
            get
            {
                if (DeclaringType.Kind == MojTypeKind.Entity) return this;
                if (Store != null) return Store;
                throw new MojenException("The property is not a store property and has no store property assigned.");
            }
        }

        /// <summary>
        /// Only used for entity (not model) properties.
        /// Indicates that this entity property won't be added to the DB.
        /// </summary>
        [DataMember]
        public bool IsExcludedFromDb { get; set; }

        [DataMember]
        public bool IsExplicitelyIncludedInOData { get; set; }

        [DataMember]
        [MapFromModel]
        public bool IsCustom { get; set; }

        // KABU TODO: REMOVE?
#if (false)
        /// <summary>
        /// Indicates whether the model property shall be mapped with its correspondig
        /// entity property using AutoMapper. Default: true.
        /// </summary>
        [DataMember]
        public bool? IsMappedToStore { get; set; }
#endif

        [DataMember]
        [MapFromModel]
        public MojBinaryConfig FileRef { get; set; } = MojBinaryConfig.None;

        /// <summary>
        /// NOTE: Only used by entities. Only configurable via entity builder / entity prop builder.
        /// </summary>
        [DataMember]
        public MojDbPropAnnotation DbAnno { get; set; } = MojDbPropAnnotation.None;

        [DataMember]
        [MapFromModel]
        public bool IsNew { get; set; }

        [DataMember]
        [MapFromModel]
        public bool IsVirtual { get; set; }

        [DataMember]
        [MapFromModel]
        public bool IsOverride { get; set; }

        [DataMember]
        [MapFromModel]
        public bool IsSealed { get; set; }

        /// <summary>
        /// Indicates whether the property has a setter.
        /// </summary>
        [DataMember]
        [MapFromModel]
        public MojPropGetSetOptions SetterOptions { get; set; } = MojPropGetSetOptions.Public;

        public bool HasSetter
        {
            get { return SetterOptions != MojPropGetSetOptions.None; }
        }

        /// <summary>
        /// Indicates whether the property will raise "property changed" events.
        /// </summary>
        [DataMember]
        [MapFromModel]
        public bool IsObservable { get; set; }

        [DataMember]
        [MapFromModel]
        public bool IsTracked { get; set; }

        [DataMember]
        [MapFromModel]
        public bool IsComputed { get; set; }

        /// <summary>
        /// Indicates whether the property is editable by a user via a UI.
        /// </summary>
        [DataMember]
        [MapFromModel]
        public bool IsEditable { get; set; }

        [DataMember]
        [MapFromModel]
        public MojOrderConfig InitialSort { get; set; } = MojOrderConfig.None;

        [DataMember]
        [MapFromModel]
        public bool IsSortable { get; set; }

        [DataMember]
        [MapFromModel]
        public bool IsFilterable { get; set; }

        [DataMember]
        [MapFromModel]
        public bool IsGroupable { get; set; }

        [DataMember]
        [MapFromModel]
        public bool IsCurrentLoggedInPerson { get; set; }

        public bool IsDataMember
        {
            get { return Attrs.Any(x => x.Name == "DataMember"); }
        }

        public MojFormedNavigationPath GetFormedPath()
        {
            if (FormedNavigationTo.Is && FormedNavigationFrom.Is)
                throw new MojenException("Both navigation directions are is use.");

            return FormedNavigationFrom.Is
                ? FormedNavigationFrom
                : FormedNavigationTo;
        }

        public string FormedTargetPath
        {
            get
            {
                var path = GetFormedPath();
                if (path.Is)
                    return path.TargetPath;

                return Name;
            }
        }

        public IEnumerable<string> FormedTargetPathNames
        {
            get
            {
                var path = GetFormedPath();
                if (path.Is)
                {
                    foreach (var name in path.StepPropNames())
                        yield return name;
                }
                else
                {
                    yield return Name;
                }
            }
        }

        public bool IsReferenced
        {
            get { return GetFormedPath().Is; }
        }

        [DataMember]
        [MapFromModel(MapContent = true)]
        /// <summary>
        /// Used by navigation properties.
        /// Set if this navigation property points to an other property of an other type.        
        /// </summary>
        public MojFormedNavigationPath FormedNavigationTo { get; set; } = MojFormedNavigationPath.None;

        [DataMember]
        [MapFromModel(MapContent = true)]
        /// <summary>
        /// Set if this property is being navigated from an other type.
        /// Not a DataMember.
        /// </summary>
        public MojFormedNavigationPath FormedNavigationFrom { get; set; } = MojFormedNavigationPath.None;

        public string GetFormedNavigationPropPathOfKey()
        {
            if (FormedNavigationFrom.Is)
                return FormedNavigationFrom.Last.TargetFormedType.Get(DeclaringType.Key.Name).FormedTargetPath;
            else
                return DeclaringType.Key.Name;
        }

#if (false)
        [OnSerializing]
        void OnSerializing(StreamingContext context)
        { }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        { }
#endif

        public override string ToString()
        {
            return Name;
        }
    }
}