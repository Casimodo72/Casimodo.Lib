using Casimodo.Lib;
using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public interface IMojClassBuilder : IMojTypeBuilder
    {
        new IMojClassPropBuilder Prop(MojProp prop);
    }

    public abstract class MojClassBuilder<TClassBuilder, TPropBuilder> :
        MojTypeBuilder<TClassBuilder, TPropBuilder>,
        IMojClassBuilder
        where TClassBuilder : MojClassBuilder<TClassBuilder, TPropBuilder>
        where TPropBuilder : MojClassPropBuilder<TClassBuilder, TPropBuilder>, IMojClassPropBuilder, new()
    {
        public TClassBuilder Class(string className)
        {
            TypeConfig.ClassName = className;

            return This();
        }

        /// <summary>
        /// Marks the type as abstract.
        /// If abstract then validation and change tracking properties
        /// will *not* be initialized for this class.
        /// </summary>
        public TClassBuilder Abstract()
        {
            TypeConfig.IsAbstract = true;

            return This();
        }

        public TClassBuilder Sealed()
        {
            TypeConfig.IsSealed = true;

            return This();
        }

        public TClassBuilder PluralName(string pluralName)
        {
            TypeConfig.InitPluralName(pluralName);

            return This();
        }

        public TClassBuilder Base(MojType baseModel)
        {
            if (TypeConfig.Kind != baseModel.Kind)
                throw new MojenException(
                    string.Format("Cannot set type with kind '{0}' as a base of type of kind '{0}'.)", baseModel.Kind, TypeConfig.Kind));

            TypeConfig.BaseClass = baseModel;
            TypeConfig.BaseClassName = baseModel.ClassName;
            //TypeConfig.Store = baseModel.Store;
            TypeConfig.IsStoreOwner = false;

            return This();
        }

        public TClassBuilder Base(string baseClassName)
        {
            TypeConfig.BaseClass = null;
            TypeConfig.BaseClassName = baseClassName;

            return This();
        }

        /// <summary>
        /// Marks the object als being multitenant.
        /// </summary>
        internal TClassBuilder Multitenant()
        {
            TypeConfig.IsMultitenant = true;

            return This();
        }

        public TClassBuilder GuidGenerateable()
        {
            TypeConfig.IsGuidGenerateable = true;

            return This();
        }

        public TClassBuilder KeyAccessible()
        {
            TypeConfig.IsKeyAccessible = true;

            return This();
        }

        public TClassBuilder EnumEntity()
        {
            TypeConfig.IsEnumEntity = true;

            return This();
        }

        public TClassBuilder ODataOpenType()
        {
            TypeConfig.IsODataOpenType = true;

            Interface(App.Get<DataLayerConfig>().IODataDynamicPropertiesAccessor);
            return This();
        }

        public TClassBuilder Interface(MojInterface iface)
        {
            var item = TypeConfig.Interfaces.FirstOrDefault(x => x.Name == iface.Name);
            if (item != null)
                // Remove previuos.
                TypeConfig.Interfaces.Remove(item);

            TypeConfig.Interfaces.Add(iface);

            return This();
        }

        public TClassBuilder HasManyParents()
        {
            TypeConfig.HasManyParents = true;

            return This();
        }

        public TClassBuilder Interface(MojType iface, bool store = true)
        {
            Guard.ArgNotNull(iface, nameof(iface));

            if (iface.Kind != MojTypeKind.Interface)
                throw new MojenException("The given MojType must be an interface type.");

            return Interface(iface.ClassName, store);
        }

        public TClassBuilder Interface(string name, bool store = true)
        {
            var item = TypeConfig.Interfaces.FirstOrDefault(x => x.Name == name);
            if (item != null)
            {
                // Update
                item.AddToStore = store;
            }
            else
            {
                // Create
                TypeConfig.Interfaces.Add(new MojInterface { Name = name, AddToStore = store });
            }

            return This();
        }

        public TClassBuilder Size(MojDataSetSizeKind size)
        {
            TypeConfig.DataSetSize = size;

            return This();
        }

        public TClassBuilder NotObservable()
        {
            TypeConfig.IsObservable = false;

            return This();
        }

        public MojTypeComparisonBuilder Equal(string name)
        {
            var builder = new MojTypeComparisonBuilder();
            builder.Config.Name = name;
            TypeConfig.Comparisons.Add(builder.Config);

            builder.None();

            return builder;
        }

        // KABU TODO: REMOVE? Not used
        //public TPropBuilder PropReferenceTo(MojType to,
        //    bool navigation = false,
        //    bool nullable = true,
        //    bool required = false,
        //    bool nested = false,
        //    bool owned = false,
        //    bool storename = false,
        //    bool uri = false,
        //    string key = null,
        //    string display = null,

        //    Action<MexConditionBuilder> conditionBuilder = null)
        //{
        //    return base.Prop(to.Name)
        //        .Reference(to,
        //            navigation: navigation,
        //            nullable: nullable,
        //            required: required,
        //            nested: nested,
        //            owned: owned,
        //            storename: storename,
        //            //uri: uri,
        //            key: key, display: display);
        //}

        //public TPropBuilder PropReferenceToParent(MojType to, bool navigation)
        //{
        //    return base.Prop(to.Name)
        //        .ReferenceToParent(to, navigation: navigation);
        //}

        public TPropBuilder Prop()
        {
            return base.Prop("", typeof(string));
        }

        public new TPropBuilder Prop(string name, int maximumLength)
        {
            return base.Prop(name, maximumLength);
        }

        public TPropBuilder Prop(string name)
        {
            return base.Prop(name, typeof(string));
        }

        public TPropBuilder PropIndex()
        {
            return Prop("Index", typeof(int)).DefaultValue(0);
        }

        public TPropBuilder Prop(string name, MojType mojtype, bool nullable = false)
        {
            return base.Prop(name, type: null, mojtype: mojtype, nullableMojType: nullable);
        }

        public TPropBuilder Prop(string name, Type type)
        {
            return base.Prop(name, type, null, false);
        }

        public new TPropBuilder Prop(MojProp prop)
        {
            var clone = base.Prop(prop).PropConfig;
            _pbuilder = MojPropBuilder.Create<TPropBuilder>(this, clone);

            return _pbuilder;
        }

        public TClassBuilder Key()
        {
            base.Prop("Id", typeof(Guid)).Key().GuidKey().StoreCore();
            KeyAccessible();
            GuidGenerateable();
            Interface("IIdGetter");

            return This();
        }

        public TClassBuilder AsSoftChildCollectionOf(MojType parentType,
            string display,
            Action<MexConditionBuilder> condition)
        {
            return AsSoftChildSingleOrCollectionCore(parentType, display: display, collection: true, condition: condition);
        }

        public TClassBuilder AsSoftChildOf(MojType parentType,
            string display,
            Action<MexConditionBuilder> condition)
        {
            return AsSoftChildSingleOrCollectionCore(parentType, display: display, collection: false, condition: condition);
        }

        TClassBuilder AsSoftChildSingleOrCollectionCore(MojType parentType,
            string display,
            bool collection,
            Action<MexConditionBuilder> condition)
        {
            Guard.ArgNotNull(parentType, nameof(parentType));
            Guard.ArgNotNull(condition, nameof(condition));

            bool owned = true;

            if (TypeConfig.IsEntity() && parentType.IsModel())
                parentType = parentType.RequiredStore;

            var c = BuildCondition(condition);
            if (c == null || c.IsEmpty)
                throw new ArgumentException("A child reference condition must be specified.", nameof(condition));

            var reference = new MojSoftReference
            {
                Is = true,
                Binding = MojReferenceBinding.Loose | (owned ? MojReferenceBinding.Owned : MojReferenceBinding.Associated),
                Multiplicity = MojMultiplicity.OneOrZero,
                Axis = MojReferenceAxis.ToParent,
                IsCollection = collection,
                ToType = parentType,
                ToTypeKey = parentType.Key,
                DisplayName = display,
                Condition = c
            };

            TypeConfig.SoftReferences.Add(reference);

            return This();
        }

        MexExpressionNode BuildCondition(Action<MexConditionBuilder> build)
        {
            if (build == null)
                return null;

            return MexConditionBuilder.BuildCondition(build);
        }

        /// <summary>
        /// Specifies that instances of this type should be used as pick items
        /// with the given key and display propery names.
        /// </summary>
        internal TClassBuilder PickDisplayProp(MojProp prop)
        {
            var pick = TypeConfig.LocalPick;
            if (pick == null)
                pick = TypeConfig.LocalPick = new MojPickConfig();

            prop.IsPickDisplay = true;

            pick.DisplayProp = prop.Name;
            if (pick.KeyProp == null)
                pick.KeyProp = TypeConfig.FindKey()?.Name;

            return This();
        }

        /// <summary>
        /// KABU TODO: ELIMINATE
        /// </summary>
        public TClassBuilder PickProps(string keyProp, string displayProp)
        {
            TypeConfig.LocalPick = new MojPickConfig { KeyProp = keyProp, DisplayProp = displayProp };

            return This();
        }

        public TClassBuilder Where(Action<MexConditionBuilder> build)
        {
            var conditionBuilder = new MexConditionBuilder();
            build(conditionBuilder);
            TypeConfig.Conditions.Add(conditionBuilder.Expression);

            return This();
        }

        public TClassBuilder NoValidation()
        {
            TypeConfig.NoValidation = true;

            return This();
        }

        public TClassBuilder NoTracking()
        {
            TypeConfig.NoChangeTracking = true;

            return This();
        }


        public TClassBuilder NamedAssignFrom(string name, params string[] props)
        {
            if (!TypeConfig.AssignFromConfig.Is)
                TypeConfig.AssignFromConfig = new MojAssignFromCollectionConfig();

            var assignment = new MojNamedAssignFromConfig();
            assignment.Name = name;

            foreach (var prop in props)
            {
                TypeConfig.CheckPropExists(prop);
                assignment.Properties.Add(prop);
            }

            TypeConfig.AssignFromConfig.Items.Add(assignment);

            return This();
        }

        /// <summary>
        /// NOTE: This method is reentrant. I.e. it can be called multiple times on the same type.
        /// </summary>
        /// <returns></returns>
        public override MojType Build()
        {
            base.Build();

            if (TypeConfig.LocalPick != null &&
                TypeConfig.LocalPick.KeyProp == null)
            {
                TypeConfig.LocalPick.KeyProp = TypeConfig.Key.Name;
            }

            // Inherit change tracked properties.
            if (TypeConfig.Kind == MojTypeKind.Model ||
                TypeConfig.Kind == MojTypeKind.Complex)
            {
                TypeConfig.ChangeTrackingProps.AddRangeDistinctBy(TypeConfig.GetLocalTrackedProps(), (x) => x.Name);

                if (TypeConfig.BaseClass != null)
                    TypeConfig.ChangeTrackingProps.AddRangeDistinctBy(TypeConfig.BaseClass.ChangeTrackingProps, (x) => x.Name);

                ComputeUIHint();
            }

            // Process base type.
            if (TypeConfig.BaseClass != null)
            {
                // Inherit base version mapping.
                if (TypeConfig.VerMap.Is)
                    TypeConfig.VerMap.InheritFrom(TypeConfig.BaseClass.VerMap);
            }

            // Add IKeyAccessor<TKey> if applicable and missing.
            if (TypeConfig.IsKeyAccessible)
            {
                var key = TypeConfig.Key;
                Interface($"IKeyAccessor<{key.Type.Name}>");
                Interface("IKeyAccessor");
            }

            // Add IGuidGenerateable if applicable and missing.
            if (TypeConfig.IsGuidGenerateable)
                Interface("IGuidGenerateable");

            if (TypeConfig.IsMultitenant)
                Interface("IMultitenant");

            // Properties ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~          

            // Process auto-generated related properties.            
            foreach (MojProp prop in TypeConfig.LocalProps)
            {
                foreach (var autoProp in prop.AutoRelatedProps)
                {
                    var related = MojPropBuilder.Create<TPropBuilder>(this, autoProp);

                    // Set summary.
                    related.PropConfig.Summary.AssignFrom(prop.Summary);
                }
            }

            AssignDeletedMarkerPropsByConvention();

            // Check props
            foreach (MojProp prop in TypeConfig.LocalProps)
            {
                if (prop.IsDataMember && prop.Store == null && !prop.IsStorePending)
                    throw new MojBuilderException(string.Format("Property '{0}': DataMember is configured, but has no underlying store.", prop.Name));
            }

            // Store ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~           

            if (TypeConfig.Store != null && TypeConfig.IsStoreOwner)
            {
                var store = TypeConfig.Store;

                var builder = MojTypeBuilder.Create<MojEntityBuilder>(App, store);

                // KABU TODO: Use property attributes in order to automate assignment
                //   to the store MojType.
                store.Id = TypeConfig.Id;
                store.DisplayName = TypeConfig.DisplayName;
                store.DisplayPluralName = TypeConfig.DisplayPluralName;
                store.DataContextName = TypeConfig.DataContextName;
                store.IsMultitenant = TypeConfig.IsMultitenant;
                store.IsODataOpenType = TypeConfig.IsODataOpenType;
                store.IsHardDeleteEnabled = TypeConfig.IsHardDeleteEnabled;
                store.VerMap = MojVersionMapping.CloneFrom(TypeConfig.VerMap);
                store.LocalPick = TypeConfig.LocalPick;
                store.DataSetSize = TypeConfig.DataSetSize;
                store.SoftReferences.AddRange(TypeConfig.SoftReferences.Select(x => x.CloneToEntity()));
                store.AssignFromConfig = TypeConfig.AssignFromConfig;

                if (TypeConfig.BaseClass != null && TypeConfig.BaseClass.Store != null)
                {
                    store.BaseClass = TypeConfig.BaseClass.Store;
                    store.BaseClassName = TypeConfig.BaseClass.Store.ClassName;
                }

                store.IsKeyAccessible = TypeConfig.IsKeyAccessible;
                store.IsGuidGenerateable = TypeConfig.IsGuidGenerateable;
                store.HasManyParents = TypeConfig.HasManyParents;

                // Add interfaces
                foreach (var iface in TypeConfig.Interfaces)
                    if (iface.AddToStore)
                        builder.Interface(iface);

                // Store properties
                ProcessPendingStoreProps();

                foreach (var comp in TypeConfig.Comparisons)
                    if (!store.Comparisons.Contains(comp))
                        store.Comparisons.Add(comp);

                // Build the store type.
                MojTypeBuilder.Create<MojEntityBuilder>(App, store).Build();
            }

            return TypeConfig;
        }

        static readonly MojPropDeletedMarker[] DeletedMarkers = new[]
        {
            MojPropDeletedMarker.Effective,
            MojPropDeletedMarker.Self,
            MojPropDeletedMarker.Cascade,
            MojPropDeletedMarker.RecycleBin
        };

        void AssignDeletedMarkerPropsByConvention()
        {
            foreach (var kind in DeletedMarkers)
            {
                var prop = TypeConfig.FindDeletedMarker(kind);
                if (prop != null)
                    continue;

                string conventionPropName = null;
                if (kind == MojPropDeletedMarker.Effective)
                    conventionPropName = "IsDeleted";
                else if (kind == MojPropDeletedMarker.Self)
                    conventionPropName = "IsSelfDeleted";
                else if (kind == MojPropDeletedMarker.Cascade)
                    conventionPropName = "IsCascadeDeleted";
                else if (kind == MojPropDeletedMarker.RecycleBin)
                    conventionPropName = "IsRecyclableDeleted";

                prop = TypeConfig.FindProp(conventionPropName);
                if (prop == null || prop.DeletedMarker.Is)
                    continue;

                prop.DeletedMarker = new MojPropDeletedMarkerConfig { Kind = kind };
            }
        }

        protected void ProcessPendingStoreProps()
        {
            if (TypeConfig.Store == null || !TypeConfig.IsStoreOwner)
                return;

            foreach (MojProp mprop in TypeConfig.LocalProps)
            {
                ProcessPendingStoreProp(mprop);
            }
        }

        protected internal void ProcessPendingStoreProp(MojProp mprop)
        {
            if (TypeConfig.Store == null || !TypeConfig.IsStoreOwner)
                return;

            if (!mprop.IsModel() || !mprop.IsStorePending)
                return;

            // Process auto related props first because
            // we need store props on those before trying to clone this prop.
            foreach (var prop in mprop.AutoRelatedProps)
                ProcessPendingStoreProp(prop);

            MojProp sprop = mprop.Store;
            if (sprop != null)
            {
                var xprop = sprop.DeclaringType.FindProp(sprop.Name);
                if (xprop != sprop)
                    throw new MojenException("Store property mismatch.");

                MojProp.AssignModelToEntity(mprop, sprop);
            }
            else
            {
                sprop = MojProp.CloneModelToEntity(mprop);

                TypeConfig.Store.AddLocalProp(sprop);
            }

            mprop.Store = sprop;
            mprop.IsStorePending = false;
        }

        internal MojEntityPropBuilder GetStorePropBuilder(MojProp prop)
        {
            return MojPropBuilder.Create<MojEntityPropBuilder>(GetEntityBuilder(), prop);
        }

        internal MojEntityBuilder GetEntityBuilder()
        {
            if (TypeConfig.Store == null)
                throw new InvalidOperationException("The type has no store type assigned.");

            if (_entityBuilder == null)
                _entityBuilder = MojTypeBuilder.Create<MojEntityBuilder>(App, TypeConfig.Store);

            return _entityBuilder;
        }

        MojEntityBuilder _entityBuilder;

        void ComputeUIHint()
        {
            MojAttr attr = null;
            string hint = null;
            foreach (MojProp p in TypeConfig.LocalProps.ToArray())
            {
                attr = p.Attrs.FirstOrDefault(x => x.Name == "UIHint");
                if (attr != null)
                    continue;

                if (p.Type.AnnotationDataType != null)
                {
                    if (_uiHintMapByDataType.TryGetValue(p.Type.AnnotationDataType.Value, out hint))
                        AddUIHintAttr(p, hint);
                    else
                        AddUIHintAttr(p, p.Type.AnnotationDataType.ToString());
                }
                else if (p.Type.TypeNormalized != null && _uiHintMapByType.TryGetValue(p.Type.TypeNormalized, out hint))
                {
                    AddUIHintAttr(p, hint);
                }
            }
        }

        void AddUIHintAttr(MojProp prop, string hint)
        {
            AddAttr(prop, "UIHint", 99, "uiHint", hint);
        }

        void AddAttr(MojProp prop, string name, int pos, string argName, string argValue)
        {
            var attr = new MojAttr(name, pos);
            attr.CSArg(argName, argValue);
            prop.Attrs.Add(attr);
        }

        readonly Dictionary<Type, string> _uiHintMapByType = new Dictionary<Type, string>
        {
            { typeof(int), "Integer" },
            { typeof(bool), "Boolean" }
        };

        readonly Dictionary<DataType, string> _uiHintMapByDataType = new Dictionary<DataType, string>
        {
            { DataType.Text, "String" }
        };

        IMojClassPropBuilder IMojClassBuilder.Prop(MojProp prop)
        {
            return Prop(prop);
        }
    }
}