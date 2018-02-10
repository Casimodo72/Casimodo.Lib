using System;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    public enum MojReferenceAxis
    {
        None,
        Link,
        Value,
        ToParent,
        ToChild,
        ToAncestor,
        ToDescendant
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojSoftReference : MojReference
    {
        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public MexExpressionNode Condition { get; set; }

        public MojSoftReference CloneToEntity()
        {
            return (MojSoftReference)CloneToEntity(null, null);
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojReference : MojBase
    {
        public static readonly MojReference None = new MojReference();

        public MojReference()
        { }

        [DataMember]
        public bool Is { get; set; }

        [DataMember]
        public MojReferenceBinding Binding { get; set; }

        [DataMember]
        public bool IsSoftDeleteCascadeDisabled { get; set; }

        // Loose or Nested
        public bool IsLoose
        {
            get { return Is && Binding.HasFlag(MojReferenceBinding.Loose); }
        }

        // Loose or Nested
        public bool IsNested
        {
            get { return Is && Binding.HasFlag(MojReferenceBinding.Nested); }
        }

        // Associated or Owned or Independent
        public bool IsOwned
        {
            get { return Is && Binding.HasFlag(MojReferenceBinding.Owned); }
        }

        // Associated or Owned or Independent
        public bool IsAssotiated
        {
            get { return Is && Binding.HasFlag(MojReferenceBinding.Associated); }
        }

        public bool IsIdependent
        {
            get { return Is && Binding.HasFlag(MojReferenceBinding.Independent); }
        }

        [DataMember]
        public MojMultiplicity Multiplicity { get; set; } = MojMultiplicity.None;

        [DataMember]
        public MojReferenceAxis Axis { get; set; } = MojReferenceAxis.None;

        public bool IsToOne
        {
            get { return Is && Multiplicity.HasFlag(MojMultiplicity.One); }
        }        

        public bool IsToMany
        {
            get { return Is && Multiplicity.HasFlag(MojMultiplicity.Many); }
        }

        public bool IsToZero
        {
            get { return Is && Multiplicity.HasFlag(MojMultiplicity.Zero); }
        }

        [DataMember]
        public bool IsNavigation { get; set; }

        [DataMember]
        public MojProp ForeignKey { get; set; }

        [DataMember]
        public bool IsForeignKey { get; set; }

        [DataMember]
        public MojProp NavigationProp { get; set; }

        [DataMember]
        public bool IsCollection { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public MojType ToType { get; set; }

        [DataMember]
        public MojProp ToTypeKey { get; set; }

        // Parent/child relationship

        /// <summary>
        /// Indicates whether this is a child reference to a parent type.
        /// Set on child references.
        /// </summary>
        public bool IsChildToParent
        {
            get { return Axis == MojReferenceAxis.ToParent; }
        }

        /// <summary>
        /// The deletion of the referenced object will be optimized.
        /// E.g. in the context of EF, the referenced entitiy will not be loaded before it is deleted.
        /// This is needed for e.g. Blobs where we don't want to load the entire entity's data
        /// into memory when deleting that entity.
        /// </summary>
        [DataMember]
        public bool IsDeletionOptimized { get; set; }

        // KABU TODO: REMOVE
        ///// <summary>
        ///// If 1 then this is a child reference to a parent type.
        ///// Set on child references.
        ///// </summary>
        //[DataMember]
        //public int ChildToParentReferenceCount { get; set; }

        /// <summary>
        /// Set on parent references. This is the back-reference property of the
        /// child type to this parent type.
        /// Only set if the parent reference is a collection.
        /// </summary>
        [DataMember]
        public MojProp ChildToParentProp { get; set; }

        public MojReference Clone()
        {
            return (MojReference)MemberwiseClone();
        }

        public bool IsRelatedToModel()
        {
            return
                ToType.IsModel() ||
                ToTypeKey.IsModel() ||
                ForeignKey.IsModel() ||
                NavigationProp.IsModel() ||
                ChildToParentProp.IsModel();
        }

        public MojReference CloneToEntity(MojProp source, MojProp entity)
        {
            var clone = Clone();

            if (ToType.IsModel())
                clone.ToType = ToType.RequiredStore;

            if (ToTypeKey.IsModel())
                clone.ToTypeKey = ToTypeKey.RequiredStore;

            if (ForeignKey.IsModel())
            {
                if (ForeignKey == source)
                    clone.ForeignKey = entity;
                else
                    clone.ForeignKey = ForeignKey.RequiredStore;
            }

            if (NavigationProp.IsModel())
            {
                // NOTE: In case the navigation was defined to be on the model only,
                //   the NavigationProp will have no store assigned.
                //   We'll remove the NavigationProp entirely from the entity's reference in this case.
                if (NavigationProp.Store == null)
                {
                    clone.NavigationProp = null;
                }
                else if (NavigationProp == source)
                    clone.NavigationProp = entity;
                else
                    clone.NavigationProp = NavigationProp.RequiredStore;
            }

            if (ChildToParentProp.IsModel())
                clone.ChildToParentProp = ChildToParentProp.RequiredStore;

            return clone;
        }
    }
}