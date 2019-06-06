using Casimodo.Lib;
using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public static class MojClassBuilderHelper
    {
        public static string BuildLinkTypeName(MojType source, MojType target)
        {
            return source.Name + "2" + target.Name;
        }

        public static string BuildLinkTypeDbTableName(MojType source, MojType target)
        {
            return source.PluralName + "2" + target.PluralName;
        }
    }

    public interface IMojClassPropBuilder
    {
        IMojClassPropBuilder Required();
        IMojClassPropBuilder NotRequired();
        MojProp PropConfig { get; }

        IMojClassPropBuilder ReferenceCore(MojType to,
            MojReferenceAxis axis = MojReferenceAxis.None,
            bool navigation = false,
            bool? navigationOnModel = null,
            bool nullable = true,
            bool? required = null,
            bool nested = false,
            bool owned = false,
            Action<MexConditionBuilder> condition = null,
            bool storename = false,
            string suffix = "Id",
            bool hiddenNavigation = false,
            MojProp ownedByProp = null,
            bool backref = false,
            IMojClassPropBuilder backrefProp = null,
            string backrefPropName = null);
    }

    public class MojUnidirM2MBuildOptions
    {

    }

    public class MojClassPropBuilder<TClassBuilder, TPropBuilder> :
        MojPropBuilder<TClassBuilder, TPropBuilder>,
        IMojClassPropBuilder
        where TClassBuilder : MojClassBuilder<TClassBuilder, TPropBuilder>
        where TPropBuilder : MojClassPropBuilder<TClassBuilder, TPropBuilder>, new()
    {
        public TPropBuilder CascadeFromProp(string propName)
        {
            var sourceProp = TypeConfig.GetProp(propName);

            var targetProp = PropConfig.Reference.NavigationProp ?? PropConfig;

            targetProp.CascadeFromProps.Add(sourceProp);

            return This();
        }

        public TPropBuilder CollectionOfComplex(MojType type)
        {
            if (type.Kind != MojTypeKind.Complex)
                throw new MojenException("Only for complex types the options owned or nested can be omitted.");

            return ChildCollectionOf(type, nested: true);
        }

        MojType TypeConfig
        {
            get { return TypeBuilder.TypeConfig; }
        }

        public TPropBuilder IndependenCollectionOf(MojType type)
        {
            CollectionOfCore(type,
                owned: false,
                nested: false,
                axis: MojReferenceAxis.ToCollectionItem,
                independent: true);

            return This();
        }

        public TPropBuilder _UnidirManyToManyCollectionOf(MojType itemType,
            string linkTypeGuid,
            string ownerPropName = null,
            Action<MojModelBuilder> buildLinkType = null)
        {
            if (true)
            {
                var prop = PropConfig;

                var typeName = MojClassBuilderHelper.BuildLinkTypeName(prop.DeclaringType, itemType);
                var typePluralName = MojClassBuilderHelper.BuildLinkTypeDbTableName(prop.DeclaringType, itemType);

                var fromType = prop.DeclaringType; // e.g. Project
                var fromProp = ownerPropName ?? prop.DeclaringType.Name; // e.g. Project or given name (e.g. "Owner")
                var fromForeignKey = fromProp + "Id"; // e.g. ProjectId

                var toType = itemType; // e.g. MoTag
                var toProp = prop.Name; // e.g. Tag
                var toTypePlural = itemType.PluralName; // e.g. MoTags
                var toForeignKey = toProp + "Id"; // e.g. TagId

                // Add many-to-many link type.
                var m = App.CurrentBuildContext.AddModel(typeName, typePluralName)
                    .Id(linkTypeGuid);

                m.TypeConfig.IsManyToManyLink = true;

                buildLinkType(m);

                m.Store();

                m.Key();

                // TODO: When fromType is deleted then the link entry must also be deleted.
                // m.Prop(aprop).ToParent(atype, nullable: false).AsChildCollection();

                // When toType is deleted then the link entry must also be deleted.
                m.Prop(toProp).ToParent(toType, nullable: false).AsChildCollection(hidden: true);

                m.PropIndex().Store();

                var linkType = m.Build();

                prop.Name = "To" + prop.Name;

                ChildCollectionOf(linkType,
                    backrefNew: true,
                    backrefPropName: fromProp,
                    backrefNavigation: true,
                    backrefRequired: true);

                m.Store(eb =>
                {
                    eb.UniqueIndex(fromProp, toProp);
                });
                m.Build();
            }
            else
            {
                // TODO: REVISIT Independent unidir M2M collections are not supported by EF Core (yet?).
#pragma warning disable CS0162 // Unreachable code detected
                IndependenCollectionOf(itemType);
#pragma warning restore CS0162 // Unreachable code detected
            }

            return This();
        }

        public TPropBuilder ChildCollectionOf(MojType type, bool nested = false,
            bool hidden = false,
            string backrefPropName = null,
            bool? backrefNew = null,
            bool? backrefRequired = null,
            bool? backrefNavigation = null,
            bool isSoftDeleteCascadeDisabled = false)
        {
            return CollectionOf2(type, owned: true, nested: nested,
                axis: MojReferenceAxis.ToChild,
                hidden: hidden,
                backrefPropName: backrefPropName,
                backrefNew: backrefNew,
                backrefNavigation: backrefNavigation,
                backrefRequired: backrefRequired,
                isSoftDeleteCascadeDisabled: isSoftDeleteCascadeDisabled);
        }

        public TPropBuilder CollectionOf(MojType type,
            bool? backrefNew = null)
        {
            return CollectionOf2(type, owned: false, nested: false,
                axis: MojReferenceAxis.ToCollectionItem,
                backrefNew: backrefNew);
        }

        TPropBuilder CollectionOf2(MojType type, bool owned, bool nested,
            MojReferenceAxis axis,
            bool isSoftDeleteCascadeDisabled = false,
            bool hidden = false,
            // @backrefNew defaults to true, but is nullable because we need to know whether specified.
            bool? backrefNew = null,
            // @backrefExplicit defaults to true, but is nullable because we need to know whether specified.
            bool? backrefExplicit = null,
            bool? backrefRequired = null,
            string backrefPropName = null,
            bool? backrefNavigation = null)
        {
            if (backrefPropName != null && backrefNew == null)
                throw new MojenException("If the backref name is specified, then backrefNew must also be specified.");

            if (backrefPropName != null && backrefExplicit != null)
                throw new MojenException("If the backref name is specified, then backrefExplicit must *not* be specified.");

            // @backrefNew defaults to true, but is nullable because we need to know whether specified.
            backrefNew = backrefNew ?? true;

            if (TypeConfig.Kind == MojTypeKind.Entity)
                type = type.StoreOrSelf;

            MojProp backrefProp = null;
            if (backrefNew == false && backrefPropName == null)
            {
                // Use existing backref prop.
                backrefProp = type.FindReferenceWithForeignKey(to: TypeConfig);
                if (backrefProp == null)
                    throw new MojenException($"The child type '{type.Name}' has no back-reference to parent type '{TypeConfig.ClassName}'.");

                // KABU TODO: VERY IMPORTANT: Currently disabled, evaluate if we really need this
                //   because it currently breaks the Project->Jobs and Job->Project scenario.
                //if (childToParentReferenceProp.Reference.ChildToParentReferenceCount != 0)
                //    throw new MojenException($"The property '{type.Name}.{childToParentReferenceProp.Name}' " +
                //        "is already a back-reference to an other property.");
            }
            else if (backrefPropName != null)
            {
                backrefProp = type.FindProp(backrefPropName);
                if (backrefProp == null)
                {
                    // Try with "Id" suffix.
                    backrefProp = type.FindProp(backrefPropName + "Id");
                }

                if (backrefProp == null && backrefNew == false)
                    throw new MojenException($"The child type '{type.Name}' has no property named '{backrefPropName}'.");

                if (backrefProp != null && backrefNew == true)
                    throw new MojenException($"The child type '{type.Name}' has already a property named '{backrefPropName}'.");

                if (backrefProp != null)
                {
                    if (!backrefProp.Reference.Is)
                    {
                        throw new MojenException($"The child type '{type.Name}' has a property named '{backrefPropName}' but it is not a reference property.");

                        // KABU TODO: Does not work with EF.
                        //if (backrefProp.Type.TypeNormalized != typeof(Guid))
                        //    throw new MojenException($"The backref property '{backrefProp.Name}' " +
                        //        $"of child type '{type.Name}' must be of type GUID in order to create an unidirectional many-to-many relationship.");                    
                    }
                    else if (!backrefProp.Reference.IsToOne)
                    {
                        throw new MojenException($"The backref property '{backrefProp.Name}' " +
                            $"of child type '{type.Name}' must have reference multiplicity of One/OneOrZero.");
                    }

                    // TODO: REMOVE: childToParentProp = backrefProp;
                    backrefPropName = null;
                }
            }

            return CollectionOfCore(type,
                owned: owned,
                nested: nested,
                axis: axis,
                isSoftDeleteCascadeDisabled: isSoftDeleteCascadeDisabled,
                hidden: hidden,
                backrefProp: backrefProp,
                backrefExplicit: backrefExplicit,
                backrefRequired: backrefRequired,
                backrefPropName: backrefPropName,
                backrefNavigation: backrefNavigation);
        }

        internal TPropBuilder CollectionOfCore(MojType childType,
            MojReferenceAxis axis,
            bool owned, bool nested,
            bool independent = false,
            bool isSoftDeleteCascadeDisabled = false,
            bool hidden = false,
            // @backrefExplicit defaults to true, but is nullable because we need to know whether specified.
            bool? backrefExplicit = null,
            bool? backrefRequired = null,
            MojProp backrefProp = null,
            string backrefPropName = null,
            bool? backrefNavigation = null)
        {
            if (axis == MojReferenceAxis.ToChild && !owned)
                throw new MojenException("Child axis mismatch.");

            if (axis != MojReferenceAxis.ToChild && owned)
                throw new MojenException("Child axis mismatch.");

            if (backrefPropName != null && backrefExplicit != null)
                throw new MojenException("If the backref name is specified, then backrefExplicit must *not* be specified.");

            if (backrefProp != null && backrefExplicit != null)
                throw new MojenException("If the backref property is specified, then backrefExplicit must *not* be specified.");

            PropConfig.IsHiddenCollectionNavigationProp = hidden;
            PropConfig.SingleName = PropConfig.Name;
            PropConfig.SetName(MojType.Pluralize(PropConfig.Name));
            PropConfig.Type.Type = null;
            PropConfig.Type.IsCollection = true;

            bool isBackrefNavigationApplied = false;

            if (TypeConfig.CanNavigateReferences &&
                childType.CanNavigateReferences)
            {
                MojProp backrefOwnedByProp = owned ? PropConfig : null;

                // EF navigation collection property.
                PropConfig.Type.CollectionTypeName = "ICollection";

                // Reference binding
                // KABU TODO: IMPORTANT: IMPL nested reference binding.
                var binding = nested ? MojReferenceBinding.Nested : MojReferenceBinding.Loose;

                if (owned)
                    binding |= MojReferenceBinding.Owned;
                else if (independent)
                    binding |= MojReferenceBinding.Independent;
                else
                    binding |= MojReferenceBinding.Associated;

                // Reference
                PropConfig.IsNavigation = true;
                PropConfig.Reference = new MojReference
                {
                    Binding = binding,
                    IsSoftDeleteCascadeDisabled = isSoftDeleteCascadeDisabled,
                    Multiplicity = MojMultiplicity.Many,
                    Axis = axis,
                    ToType = childType
                };
                if (!independent)
                {
                    if (backrefProp == null)
                    {
                        // Add a back-reference property to the collection item type,
                        //   otherwise EF will itself auto-generate a crappy foreign key name.
                        //   Currently the back-reference will *not* be a navigation property.
                        // KABU TODO: REVISIT: Evaluate scenarios where we would want
                        //   a navigational back-reference property.
                        isBackrefNavigationApplied = true;

                        string effectiveBackrefPropName;
                        if (backrefPropName != null)
                            effectiveBackrefPropName = backrefPropName.RemoveRight("Id");
                        else if (PropConfig.SingleName == childType.Name || backrefExplicit == false)
                            effectiveBackrefPropName = TypeConfig.Name;
                        else
                            effectiveBackrefPropName = PropConfig.SingleName + "Of" + TypeConfig.Name;

                        MojReferenceAxis backrefAxis = MojReferenceAxis.None;
                        if (axis == MojReferenceAxis.ToChild)
                            backrefAxis = MojReferenceAxis.ToParent;
                        else if (axis == MojReferenceAxis.ToCollectionItem)
                        {
                            // TODO: Never hit in our scenarios.
                            // TODO: CLARIFY: Hmm, isn't that actually ToParent just with the parent having to one but many children?
                            //   Maybe not, because in the case of independent collections this is not true.
                            //   But will EF Core ever support independent collections and do we want to use them anyway?
                            backrefAxis = MojReferenceAxis.ToCollection;
                        }

                        else throw new MojenException("Unexpected axis");

                        MojPropBuilder pbuilder = null;

                        if (childType.Kind == MojTypeKind.Entity)
                        {
                            pbuilder =
                                MojTypeBuilder.Create<MojEntityBuilder>(App, childType)
                                    .Prop(effectiveBackrefPropName)
                                        .ReferenceCore(to: TypeBuilder.TypeConfig,
                                            axis: backrefAxis,
                                            required: backrefRequired,
                                            navigation: backrefNavigation == true,
                                            ownedByProp: backrefOwnedByProp);

                        }
                        else if (childType.Kind == MojTypeKind.Model)
                        {
                            pbuilder =
                                MojTypeBuilder.Create<MojModelBuilder>(App, childType)
                                    .Prop(effectiveBackrefPropName)
                                        .ReferenceCore(to: TypeBuilder.TypeConfig,
                                            axis: backrefAxis,
                                            required: backrefRequired,
                                            navigation: backrefNavigation == true,
                                            ownedByProp: backrefOwnedByProp);
                        }
                        else throw new MojenException($"Invalid child type kind '{childType.Kind}' for this operation.");

                        backrefProp = pbuilder
                            .PropConfig
                            // Prefer navigation prop over foreign key prop.
                            .NavigationOrSelf;
                    }
                    else
                    {
                        if (backrefOwnedByProp != null)
                        {
                            // TODO: Assign only once because both reference objects are the same.
                            backrefProp.NavigationOrSelf.Reference.OwnedByProp = backrefOwnedByProp;
                            backrefProp.ForeignKeyOrSelf.Reference.OwnedByProp = backrefOwnedByProp;
                        }
                    }

                    PropConfig.Reference.ForeignBackrefProp = backrefProp;
                }
            }
            else
            {
                // Non EF navigational collection property.
                // Currently the case only for type.ClassName == MoContentAttachment.
                // KABU TODO: REMOVE
                //CustomType(string.Format("Collection<{0}>", type.ClassName));
                PropConfig.Type.CollectionTypeName = "Collection";
            }

            if (!isBackrefNavigationApplied && backrefNavigation != null)
                throw new MojenException($"A navigation backref property could not be created in this scenario.");

            if (owned && backrefProp != null && backrefProp.Reference.OwnedByProp != null &&
                backrefProp.Reference.OwnedByProp != PropConfig)
                throw new MojenException($"This type defines already an owner via property '{backrefProp.Name}'.");

            if (backrefProp != null)
                backrefProp.NavigationOrSelf.Reference.ForeignCollectionProp = PropConfig;

            PropConfig.Type.CollectionElementTypeConfig = childType;
            PropConfig.Type.GenericTypeArguments.Add(MojPropType.Create(childType, nullable: false));

            PropConfig.Type.BuildName(null);

            PropConfig.IsSortable = false;
            PropConfig.IsFilterable = false;
            PropConfig.IsGroupable = false;

            StoreCore();

            return This();
        }

        public TPropBuilder Binary(int length = -1)
        {
            Type(typeof(byte[]));
            if (length >= 0)
                MaxLength(length);

            return This();
        }

        /// <summary>
        /// Makes the property an OData dynamic property container.
        /// Makes the type an OData open type.
        /// </summary>
        public TPropBuilder ODataDynamicPropsContainer()
        {
            Type(typeof(Dictionary<string, object>));
            PropConfig.IsODataDynamicPropsContainer = true;
            TypeBuilder.ODataOpenType();

            return This();
        }

        /// <summary>
        /// Initializes a non-primitive, non-value type.
        /// </summary>
        internal TPropBuilder CustomType(string type)
        {
            PropConfig.Type.SetCustom(type);

            return This();
        }

        [Obsolete("Don't use this one. Define an enum MojType instead and use that model.")]
        public TPropBuilder Enum(string type, bool nullable)
        {
            PropConfig.Type.SetEnum(type, nullable);

            return This();
        }

        public TPropBuilder Id(string guid)
        {
            PropConfig.Id = new Guid(guid);

            return This();
        }

        public TPropBuilder PhoneNumber()
        {
            MaxLength(32);
            return Type(DataType.PhoneNumber);
        }

        public TPropBuilder PostalCode()
        {
            MaxLength(5);
            MinLength(5);
            RegEx(@"^\d{5}$", "Die Postleitzahl muss eine fünfstellige Zahl sein.");
            return Type(DataType.PostalCode);
        }

        public TPropBuilder Email()
        {
            MaxLength(100);
            return Type(DataType.EmailAddress);
        }

        public TPropBuilder Url()
        {
            MaxLength(256);
            return Type(DataType.Url);
        }

        public TPropBuilder Password()
        {
            return Type(DataType.Password);
        }

        public TPropBuilder OnChangeRaise(string prop)
        {
            PropConfig.OnChangeRaiseProps.Add(prop);
            return This();
        }

        public TPropBuilder GuidKey()
        {
            PropConfig.IsGuidKey = true;
            Required();

            // KABU TODO: Generate an index in the DB.

            PropConfig.IsSortable = false;
            Editable(false);
            Trackable(false);
            Observable(false);

            return This();
        }

        public TPropBuilder Key()
        {
            PropConfig.IsKey = true;

            if (PropConfig.IsEntityOrModel())
            {
                Required();

                if (PropConfig.Type.TypeNormalized == typeof(int))
                    Attr(new MojAttr("DatabaseGenerated", 2).CArg("databaseGeneratedOption", "DatabaseGeneratedOption.Identity", typeof(Enum))); // databaseGeneratedOption
                else
                    Attr(new MojAttr("DatabaseGenerated", 2).CArg("databaseGeneratedOption", "DatabaseGeneratedOption.None", typeof(Enum)));
            }

            PropConfig.IsSortable = false;
            Editable(false);
            Trackable(false);
            Observable(false);

            return This();
        }

        public TPropBuilder New()
        {
            PropConfig.IsNew = true;

            return This();
        }

        public TPropBuilder PickDisplay()
        {
            TypeBuilder.PickDisplayProp(PropConfig);
            return This();
        }

        public TPropBuilder ToTenant(MojType to)
        {
            ToAncestor(to, navigation: false, required: true);

            NoVerMap();

            PropConfig.IsTenantKey = true;
            PropConfig.StoreOrSelf.IsTenantKey = true;

            // KABU TODO: VERY IMPORTANT: Currently we must not make the
            // TenantId required, because otherwise OData controllers
            // won't accept null values. Even explicitly defining
            // the property to be ignored (e.g. "odataBuilder.EntityType<ArticleDefinition>().Ignore(x => x.TenantId);")
            // does not have any effect.
            //Required();
            Editable(false);
            Trackable(false);
            Observable(false);

            TypeBuilder.Multitenant();

            return This();
        }

        public TPropBuilder ToParentAsChildCollection(MojType parent, bool required = true, string collectionPropName = null)
        {
            return ToParent(parent, required: required).AsChildCollection(collectionPropName: collectionPropName);
        }

        public TPropBuilder AsChildCollection(bool hidden = false, string collectionPropName = null)
        {
            return AsChildSingleOrCollection(hidden, MojMultiplicity.Many,
                collectionPropName: collectionPropName);
        }

        TPropBuilder AsChildSingleOrCollection(bool hidden, MojMultiplicity multiplicity, string collectionPropName = null)
        {
            if (!PropConfig.Reference.IsChildToParent)
                throw new MojenException($"The property '{PropConfig.Name}' is not a child reference to a parent type.");

            var parentType = PropConfig.Reference.ToType;

            // Prefer navigation property over foreign key property.
            var prop = PropConfig.NavigationOrSelf;

            var childType = prop.DeclaringType;

            // Create parent to child references.

            if (multiplicity.HasFlag(MojMultiplicity.Many))
            {
                collectionPropName = collectionPropName ?? prop.DeclaringType.Name;
                // Create parent's collection property.
                MojPropBuilder pbuilder = null;
                if (parentType.Kind == MojTypeKind.Model)
                {
                    pbuilder = MojTypeBuilder.Create<MojModelBuilder>(App, parentType)
                        .Prop(collectionPropName)
                            .CollectionOfCore(childType,
                                owned: true,
                                nested: false,
                                axis: MojReferenceAxis.ToChild,
                                // Hide the parent to child navigation property in the EF model.
                                hidden: hidden,
                                backrefProp: prop);
                }
                else if (parentType.Kind == MojTypeKind.Entity)
                {
                    pbuilder = MojTypeBuilder.Create<MojEntityBuilder>(App, parentType)
                        .Prop(collectionPropName)
                            .CollectionOfCore(childType,
                                owned: true,
                                nested: false,
                                axis: MojReferenceAxis.ToChild,
                                // Hide the parent to child navigation property in the EF model.
                                hidden: hidden,
                                backrefProp: prop);
                }
                else throw new MojenException($"Unexpected type kind: '{parentType.Kind}'");

                prop.Reference.ForeignCollectionProp = pbuilder.PropConfig;
            }
            else if (multiplicity.HasFlag(MojMultiplicity.One))
            {
                throw new MojenException("Not supported yet. Evaluate this scenario.");

#pragma warning disable CS0162
                if (parentType.Kind == MojTypeKind.Model)
                {
                    MojTypeBuilder.Create<MojModelBuilder>(App, parentType)
                        .Prop(prop.DeclaringType.Name)
                            .ReferenceCore(to: childType,
                                owned: true,
                                nested: false);
                }
                else if (parentType.Kind == MojTypeKind.Entity)
                {
                    MojTypeBuilder.Create<MojEntityBuilder>(App, parentType)
                        .Prop(prop.DeclaringType.Name)
                            .ReferenceCore(to: childType,
                                owned: true,
                                nested: false);
                }
                else throw new MojenException($"Unexpected type: '{parentType.Kind}'");
#pragma warning restore CS0162
            }
            else throw new MojenException($"Unexpected multiplicity '{multiplicity}'.");

            return This();
        }

        public TPropBuilder Type(MojType type, bool nullable = true, bool nested = false, bool? required = null,
            // @navigation is implicitly false but we need to know whether specified.
            bool? navigation = null,
            // @navigationOnModel is implicitly false but we need to know whether specified.
            bool? navigationOnModel = null)
        {
            Guard.ArgNotNull(type, nameof(type));

            if (navigation != null && navigationOnModel != null)
                throw new ArgumentNullException("The arguments 'navigation' are 'navigationOnModel' are mutually exclusive.");

            navigation = navigationOnModel == true ? false : (navigation != false);

            if (TypeConfig.IsEntityOrModel() && type.IsEntityOrModel())
            {
                return ReferenceCore(to: type, axis: MojReferenceAxis.Value,
                    navigation: navigation.Value,
                    navigationOnModel: navigationOnModel,
                    nullable: nullable,
                    nested: nested,
                    required: required);
            }
            else
                return base.TypeCore(type, nullable: nullable);
        }

        public TPropBuilder ToParent(MojType to,
           bool navigation = true,
           bool? navigationOnModel = null,
           bool nullable = true,
           bool? required = null,
           Action<MexConditionBuilder> condition = null)
        {
            return ReferenceCore(to, MojReferenceAxis.ToParent,
                navigation, navigationOnModel, nullable, required, nested: false, owned: false, condition: condition);
        }

        /// <summary>
        /// In the context of EF, the referenced entitiy will not be loaded before it is deleted.
        /// This is needed for e.g. Blobs where we don't want to load the entire entity's data
        /// into memory when deleting that entity.
        /// </summary>
        public TPropBuilder OptimizeDeletion()
        {
            if (!PropConfig.Reference.Is)
                throw new MojenException($"The option '{nameof(OptimizeDeletion)}' is available for reference properties only.");

            PropConfig.Reference.IsOptimizedDeletion = true;
            return This();
        }

        public TPropBuilder ToChildNew(MojType to,
            bool required = true,
            bool nested = false,
            string backrefName = null)
        {
            return ReferenceCore(to, MojReferenceAxis.ToChild,
                owned: true, // Owned because this is a ToChild reference.
                nested: nested,
                required: required,
                navigation: true,
                backref: true,
                backrefPropName: backrefName);
        }

        /// <summary>
        /// A non-nested child can have multiple parents.
        /// A parent to child relationship means that the child will be
        /// deleted when the parent is deleted. I.e. there can be no child without a parent.
        /// </summary>      
        public TPropBuilder ToChild(MojType to,
            bool navigation = true,
            bool? required = null,
            bool nested = false,
            bool backref = false,
            IMojClassPropBuilder childBackrefProp = null)
        {
            return ReferenceCore(to, MojReferenceAxis.ToChild, owned: true, nested: nested,
                required: required, navigation: navigation,
                backrefProp: childBackrefProp,
                backref: backref);
        }

        public TPropBuilder ToAncestor(MojType to, bool navigation = true, bool required = true)
        {
            return ReferenceCore(to, MojReferenceAxis.ToAncestor, navigation: navigation, required: required);
        }

        IMojClassPropBuilder IMojClassPropBuilder.ReferenceCore(MojType to,
            MojReferenceAxis axis,
            bool navigation,
            bool? navigationOnModel,
            bool nullable,
            bool? required,
            bool nested,
            bool owned,
            Action<MexConditionBuilder> condition,
            bool storename,
            string suffix,
            bool hiddenNavigation,
            MojProp ownedByProp,
            bool backref,
            IMojClassPropBuilder backrefProp,
            string backrefPropName)
        {
            return ReferenceCore(to, axis,
                 navigation: navigation,
                 navigationOnModel: navigationOnModel,
                 nullable: nullable,
                 required: required,
                 nested: nested,
                 owned: owned,
                 condition: condition,
                 storename: storename,
                 suffix: suffix,
                 hiddenNavigation: hiddenNavigation,
                 ownedByProp: ownedByProp,
                 backref: backref,
                 backrefProp: backrefProp,
                 backrefPropName: backrefPropName);
        }

        /// <summary>
        /// Creates a reference to an other type.
        /// </summary>
        /// <param name="to">The target type of the reference.</param>
        /// <param name="suffix"></param>
        /// <param name="nullable"></param>
        /// <param name="navigation">Generates an EF navigation property.</param>
        /// <param name="owned">
        /// Is true then the reference is owned, otherwise associated.
        /// Owned references: OnDeleted: deletes the referenced object as well or sets its IsDeleted marker property.</param>
        /// <param name="storename">Generates a property holding a generated file name (mostly a GUID and an file extension).</param>
        /// <param name="uri">Generates a dynamically populated property which holds the URI to the materialized file.</param>
        /// <param name="condition"></param>
        /// <returns></returns>  
        internal TPropBuilder ReferenceCore(MojType to,
        MojReferenceAxis axis = MojReferenceAxis.None,
        bool navigation = false,
        bool? navigationOnModel = null,
        bool nullable = true,
        bool? required = null,
        bool nested = false,
        bool owned = false,
        Action<MexConditionBuilder> condition = null,
        bool storename = false,
        string suffix = "Id",
        bool hiddenNavigation = false,
        MojProp ownedByProp = null,
        bool backref = false,
        IMojClassPropBuilder backrefProp = null,
        string backrefPropName = null)
        {
            if (to == null) throw new ArgumentNullException("to");

            if (axis == MojReferenceAxis.None)
                throw new ArgumentException("The reference direction is missing.", nameof(axis));

            if (backrefProp != null && !backref)
                throw new ArgumentException("Back-reference property was provided but the backref argument is false.");

            if (backrefProp != null && backrefPropName != null)
                throw new ArgumentException("Back-reference property was provided and the backrefProoName argument is not null.");

            var referenceProp = PropConfig;

            if (to.Kind == MojTypeKind.Model && referenceProp.DeclaringType.Kind != MojTypeKind.Model)
                to = to.Store;

            if (to == null)
                throw new MojenException("The given model type must have a store entity type assigned.");

            if (navigationOnModel != null && referenceProp.DeclaringType.Kind != MojTypeKind.Model)
                throw new MojenException("The option 'navigationOnModel' is intended for model types only.");

            if (string.IsNullOrWhiteSpace(referenceProp.Name))
                referenceProp.SetName(to.Name);

            var origName = referenceProp.Name;
            var toKeyProp = to.Key;

            // Foreign key property.

            // TODO: ? Clear bogus annotation data type.            

            var foreignKeyPropName = origName + suffix;
            referenceProp.SetName(foreignKeyPropName);
            referenceProp.Alias = origName;
            Display(to.DisplayName);

            Type(nullable ? typeof(Guid?) : typeof(Guid));

            referenceProp.Type.IsReference = true;

            // Don't bother sorting GUIDs.  
            var sortable = referenceProp.Type.TypeNormalized != typeof(Guid);

            referenceProp.IsSortable = sortable;

            // Reference binding
            var binding = nested ? MojReferenceBinding.Nested : MojReferenceBinding.Loose;
            if (owned)
                binding = binding | MojReferenceBinding.Owned;
            else
                binding = binding | MojReferenceBinding.Associated;

            // Reference
            referenceProp.IsForeignKey = true;
            var reference = referenceProp.Reference = new MojReference
            {
                Binding = binding,
                ForeignKey = referenceProp,
                Multiplicity = nullable ? MojMultiplicity.OneOrZero : MojMultiplicity.One,
                // TODO: Add validation: nested and owned must not have any other axis that "ToChild".
                Axis = axis,
                ToType = to,
                ToTypeKey = toKeyProp,
                OwnedByProp = ownedByProp
            };

            // Condition
            var expressionVal = BuildForeignKeyCondition(condition);
            if (expressionVal != null)
                referenceProp.ForeignKeyConditions.Add(expressionVal);

            if (required == true)
                Required();
            else if (required == false)
                NotRequired();

            // Store prop if applicable.
            StoreCore()
                // Always create an index for foreign key properties.
                .Index();

            if (referenceProp.IsModel())
            {
                // Allow for subsequent assignment to the store prop.
                referenceProp.IsStorePending = true;
            }

            TPropBuilder builder;

            //if (!navigation)
            //    throw new MojenException("All reference must have a navigation property now.");

            // Add navigation property.
            if (navigation || navigationOnModel == true)
            {
                builder = TypeBuilder.Prop(origName, type: null, mojtype: to, related: true);
                var prop = builder.PropConfig;

                prop.IsSortable = sortable;

                // Set as navigation prop on the reference.
                referenceProp.Reference.NavigationProp = prop;
                prop.Reference = referenceProp.Reference;
                prop.IsNavigation = true;

                // KABU TODO: IMPORTANT: I think we don't need the IsHiddenOneToManyNavigationProp
                //   anymore as one can now specify @navigationOnModel in order to
                //   have a navigation prop which is not exposed to the EF model.
                // Hidden (i.e. not exposed on the EF model) navigation references.
                if (hiddenNavigation)
                    throw new MojenException("Not supported yet. Evaluate the need for this scenario.");
                prop.IsHiddenCollectionNavigationProp = hiddenNavigation;

                // EF navigation properties must be virtual to allow for LazyLoading.
                // Even if this does not end up being an EF navigation prop,
                //   having virtual navigation props is desired.
                builder.Virtual();

                if (required == true)
                    builder.Required();
                else if (required == false)
                    builder.NotRequired();

                if (referenceProp.IsEntity())
                {
                    // Annotate with [ForeignKey("MyForeignKeyPropertyName")]
                    // This is used by EF conventions to build the database.
                    builder.ForeignKeyAttr(foreignKeyPropName);
                }

                // Add navigation prop to related properties of the foreign key prop.
                referenceProp.AutoRelatedProps.Add(prop);

                if (navigation && referenceProp.IsModel())
                {
                    // Add navigation property to model's entity.

                    builder.StoreCore();

                    // Allow for subsequent assignment to the store prop.
                    prop.IsStorePending = true;

                    // Annotate with [ForeignKey("MyForeignKeyPropertyName")]
                    // This is used by EF conventions to build the database.
                    builder.ForeignKeyAttr(foreignKeyPropName);

                    // Add to store's related props.
                    referenceProp.Store.AutoRelatedProps.Add(prop.Store);
                }
            }

            // Create back-reference navigation prop (and back-reference "foreign key", which actually won't be
            //   a foreign key in the DB due to EF Core limitation).
            if (backref && axis == MojReferenceAxis.ToChild)
            {
                if (backrefProp == null)
                {
                    var effectiveBackrefNaviPropName = backrefPropName ?? TypeBuilder.TypeConfig.Name;

                    if (to.Kind == MojTypeKind.Entity)
                    {
                        backrefProp = MojTypeBuilder.Create<MojEntityBuilder>(App, to).Prop(effectiveBackrefNaviPropName);
                    }
                    else if (to.Kind == MojTypeKind.Model)
                    {
                        backrefProp = MojTypeBuilder.Create<MojModelBuilder>(App, to).Prop(effectiveBackrefNaviPropName);

                    }
                    else throw new MojenException($"Invalid child type kind '{to.Kind}' for this operation.");
                }
                backrefProp.ReferenceCore(TypeConfig, MojReferenceAxis.ToParent, navigation: true, required: true, backref: false);

                reference.ForeignBackrefProp = backrefProp.PropConfig.ForeignKeyOrSelf;
            }

            return This();
        }

        public TPropBuilder AsChildOfNew(MojType parent, bool childRequired, string parentProp = null)
        {
            var parentPropName = parentProp ?? TypeBuilder.TypeConfig.Name;
            if (TypeConfig.Kind == MojTypeKind.Entity)
            {
                var pbuilder = MojTypeBuilder.Create<MojEntityBuilder>(App, parent).Prop(parentPropName);
                pbuilder.ToChild(TypeConfig, required: childRequired, childBackrefProp: this, backref: true);
            }
            else if (TypeConfig.Kind == MojTypeKind.Model)
            {
                var pbuilder = MojTypeBuilder.Create<MojModelBuilder>(App, parent).Prop(parentPropName);
                pbuilder.ToChild(TypeConfig, required: childRequired, childBackrefProp: this, backref: true);
            }
            else throw new MojenException($"Invalid type kind '{TypeConfig.Kind}' for this operation.");

            return This();
        }

        MexExpressionNode BuildForeignKeyCondition(Action<MexConditionBuilder> build)
        {
            if (build == null)
                return null;

            var conditionBuilder = new MexConditionBuilder();
            build(conditionBuilder);
            return conditionBuilder.Expression;
        }

        public TPropBuilder OwnedChildImage(MojType to, bool nullable = true)
        {
            return OwnedFile(to, MojReferenceAxis.ToChild, nullable, navigation: true, image: true);
        }

        /// <summary>
        /// Makes the property an owned file of the specified file-type.
        /// </summary>        
        public TPropBuilder OwnedChildFile(MojType to, bool nullable = true, bool navigation = false)
        {
            return OwnedFile(to, MojReferenceAxis.ToChild, nullable: nullable, navigation: navigation);
        }

        public TPropBuilder OwnedFile(MojType to, MojReferenceAxis axis, bool nullable = true, bool navigation = false, bool image = false)
        {
            ReferenceCore(to, axis: axis, nullable: nullable, navigation: navigation, owned: true);

            PropConfig.IsFilterable = false;
            PropConfig.IsGroupable = false;
            PropConfig.IsSortable = false;

            PropConfig.FileRef = new MojBinaryConfig();
            PropConfig.FileRef.Is = true;
            PropConfig.FileRef.IsImage = image;
            // KABU TODO: REMOVE: since UI specific.
            //PropConfig.FileRef.IsUploadable = uploadable;

            // KABU TODO: REMOVE
#if (false)
            if (uploadable)
            {
                // Add FileUploadInfo
                // If no key name was specified then use the key defined on the referenced type.
                var builder = TypeBuilder
                    .Prop("FileUploadInfo", type: null, mojtype: null)
                    .CustomType("DbAttachmentOperationInfo");
                builder.PropConfig.IsSortable = false;

                var storeBuilder = builder.StoreCore();
                // The DbAttachmentOperationInfo(s) are transient and never added to the DB.
                storeBuilder.PropConfig.IsExcludedFromDb = true;
                storeBuilder.PropConfig.IsSortable = false;

                // Add to related properties.
                PropConfig.AutoRelatedProps.Add(builder.PropConfig);
            }
#endif            

            return This();
        }

        public TPropBuilder Multiline(MojMultilineStringMode mode = MojMultilineStringMode.Default)
        {
            if (PropConfig.Type.TypeNormalized != typeof(string))
                throw new Exception(string.Format("Property CLR type '{0}' cannot be a multiline text.",
                    PropConfig.Type.TypeNormalized?.Name));

            PropConfig.Type.AnnotationDataType = DataType.MultilineText;
            PropConfig.Type.MultilineString = new MojMultilineStringType
            {
                Mode = mode
            };

            return This();
        }

        public TPropBuilder Spellcheck()
        {
            PropConfig.IsSpellCheck = true;
            return This();
        }

        /// <summary>
        /// KABU TODO: REVISIT: This will have only effect for XAML client view models.
        /// </summary>
        public TPropBuilder LocallyRequired(string error = null)
        {
            //return Attr(new MojAttr("LocallyRequired", 3).ErrorArg(error));
            PropConfig.UseRules().IsLocallyRequired = true;
            return This();
        }

        public TPropBuilder ProxyInherited(string propertyName)
        {
            PropConfig.ProxyOfInheritedProp = propertyName;

            return This();
        }

        public TPropBuilder InitialSort(MojSortDirection direction = MojSortDirection.Ascending, int index = 1)
        {
            PropConfig.InitialSort = new MojOrderConfig
            {
                Name = PropConfig.Name,
                Index = (index <= 0 ? 0 : index),
                Direction = direction
            };

            return This();
        }

        public TPropBuilder Virtual()
        {
            PropConfig.IsVirtual = true;

            return This();
        }

        public TPropBuilder Override(bool @sealed = false)
        {
            if (TypeConfig.BaseClass == null)
                throw new MojenException(
                    string.Format("The property '{0}' cannot be overriden because it is not defined in the ancestor axis.",
                    PropConfig.Name));

            var inheritedProp = TypeConfig.BaseClass.GetProps().Reverse().FirstOrDefault(x => x.Name == PropConfig.Name);
            if (inheritedProp == null)
                throw new MojenException(
                    string.Format("The property '{0}' cannot be overriden because it is not defined in the ancestor axis.",
                    PropConfig.Name));

            if (!inheritedProp.IsVirtual)
                throw new MojenException(
                    string.Format("The property '{0}' cannot be overriden because its ancestor property is not virtual or abstract.",
                    PropConfig.Name));

            PropConfig = TypeConfig.OverrideLocalProp(PropConfig, inheritedProp, @sealed);

            return This();
        }

        public TPropBuilder Computed()
        {
            Trackable(false);
            Editable(false);
            PropConfig.IsComputed = true;

            return This();
        }

        public TPropBuilder Trackable(bool value = true)
        {
            PropConfig.IsTracked = value;

            return This();
        }

        public TPropBuilder CurrentLoggedInPerson()
        {
            PropConfig.IsCurrentLoggedInPerson = true;

            return This();
        }

        public TPropBuilder Editable(bool editable = true)
        {
            PropConfig.IsEditable = editable;
            Attr(new MojAttr("Editable", 30).CArg("allowEdit", editable));

            return This();
        }

        public TPropBuilder Observable(bool observable = true)
        {
            if (!TypeConfig.IsObservable && observable)
                throw new MojenException(
                    string.Format("The property '{0}' must not be observable, " +
                        "because its container was defined to be non-observable.", PropConfig.Name));

            PropConfig.IsObservable = observable;

            return This();
        }

        internal protected MojEntityPropBuilder StoreCore()
        {
            return StoreCore(null);
        }

        protected virtual MojEntityPropBuilder StoreCore(Action<MojEntityPropBuilder> build)
        {
            // Overriden in ModelPropBuilder and EntityPropBuilder.

            // Return dummy entitiy prop builder.
            return MojEntityPropBuilder.CreateDummyBuilder();
        }

        IMojClassPropBuilder IMojClassPropBuilder.Required()
        {
            return Required();
        }

        IMojClassPropBuilder IMojClassPropBuilder.NotRequired()
        {
            return NotRequired();
        }

        protected MojProp EnsureStoreProp()
        {
            if (!TypeConfig.IsModel())
                throw new MojenException("A model type was expected.");

            var storeType = TypeConfig.RequiredStore;

            var mprop = PropConfig;

            var sprop = mprop.Store;

            if (sprop == null)
                sprop = storeType.GetProps().FirstOrDefault(x => x.Id == mprop.Id);

            if (sprop != null)
            {
                if (sprop.DeclaringType != storeType)
                    throw new MojenException("Store property declaring type mismatch");

                // Just re-assign values.
                MojProp.AssignModelToEntity(PropConfig, sprop);
            }
            else
            {
                sprop = MojProp.CloneModelToEntity(PropConfig);
                storeType.AddLocalProp(sprop);

                //ConvertToEntityProp(PropConfig, sprop);

                //if (mprop.CascadeFromProps.Count != 0)
                //{
                //    // Process cascade-from properties.
                //    foreach (var cascadeFrom in mprop.CascadeFromProps)
                //    {
                //        if (cascadeFrom.Store != null &&
                //            !sprop.CascadeFromProps.Contains(cascadeFrom.Store))
                //        {
                //            sprop.CascadeFromProps.Add(cascadeFrom.Store);
                //        }
                //    }
                //}
            }

            PropConfig.Store = sprop;
            PropConfig.IsStorePending = false;

            return sprop;
        }

        // KABU TODO: REMOVE
        //internal void ConvertToEntityProp(MojProp mprop, MojProp eprop)
        //{
        //    // Rules need attention

        //    // If the model type's store type changes:            
        //    //if (mprop.Type.TypeConfig != null &&
        //    //    mprop.Type.TypeConfig.Store != null &&
        //    //    mprop.Type.TypeConfig.Store != eprop.Type.TypeConfig)
        //    //{
        //    //    TypeBuilder.GetStorePropBuilder(eprop)
        //    //        .Type(mprop.Type.TypeConfig.Store);
        //    //}

        //    if (mprop.CascadeFromProps.Count != 0)
        //    {
        //        // Process cascade-from properties.
        //        foreach (var cascadeFrom in mprop.CascadeFromProps)
        //        {
        //            if (cascadeFrom.Store != null &&
        //                !eprop.CascadeFromProps.Contains(cascadeFrom.Store))
        //            {
        //                eprop.CascadeFromProps.Add(cascadeFrom.Store);
        //            }
        //        }
        //    }

        //    //eprop.ConvertToEntity();
        //}
    }
}