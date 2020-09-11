using System;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    public enum MojReferenceAxis
    {
        None,
        //Link,
        Value,
        ToParent,
        ToCollection,
        ToCollectionItem,
        ToChild,
        ToAncestor,
        // TODO: REMOVE? ToDescendant
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
        public static readonly MojReference None = new MojReference(false);

        MojReference(bool @is)
        {
            Is = @is;
        }

        public MojReference()
        {
            Is = true;
        }

        [DataMember]
        public bool Is { get; private set; }

        [DataMember]
        public MojReferenceBinding Binding { get; set; }

        [DataMember]
        public bool IsSoftDeleteCascadeDisabled { get; set; }

        /// <summary>
        /// (group: Loose or Nested)
        /// </summary>
        public bool IsLoose
        {
            get { return Is && Binding.HasFlag(MojReferenceBinding.Loose); }
        }

        /// <summary>
        /// (group: Loose or Nested)
        /// </summary>
        public bool IsNested
        {
            get { return Is && Binding.HasFlag(MojReferenceBinding.Nested); }
        }

        /// <summary>
        /// (group: Associated or Owned or Independent)
        /// </summary>        
        public bool IsOwned
        {
            get { return Is && Binding.HasFlag(MojReferenceBinding.Owned); }
        }

        [DataMember]
        public MojProp OwnedByProp { get; set; }

        /// <summary>
        /// (group: Associated or Owned or Independent)
        /// </summary>  
        public bool IsAssotiated
        {
            get { return Is && Binding.HasFlag(MojReferenceBinding.Associated); }
        }

        /// <summary>
        /// (group: Associated or Owned or Independent)
        /// </summary>  
        public bool Independent
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

        // TODO: REMOVE
        //[DataMember]
        //public bool IsNavigation { get; set; }

        [DataMember]
        public MojProp ForeignKey { get; set; }

        // TODO: REMOVE
        //[DataMember]
        //public bool IsForeignKey { get; set; }

        [DataMember]
        public MojProp NavigationProp { get; set; }

        [DataMember]
        public bool IsCollection { get; set; }

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
        /// The deletion of the referenced objects will be optimized.
        /// E.g. in the context of EF, the referenced entitiy will not be loaded when it is deleted.
        /// This is needed for e.g. Blobs where we don't want to load the entire entity's data
        /// into memory when deleting that entity.
        /// </summary>
        [DataMember]
        public bool IsOptimizedDeletion { get; set; }

        /// <summary>
        /// Only used if this reference is a collection.
        /// This is the foreign back-reference property of the
        /// contained item to this container.
        /// </summary>
        [DataMember]
        public MojProp ForeignBackrefProp { get; set; }

        /// <summary>
        /// The foreign navigation collection property if this is a one-to-many backref property.
        /// </summary>
        [DataMember]
        public MojProp ForeignCollectionProp { get; set; }

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
                ForeignCollectionProp.IsModel() ||
                ForeignBackrefProp.IsModel();
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

            if (ForeignBackrefProp.IsModel())
                clone.ForeignBackrefProp = ForeignBackrefProp.RequiredStore;

            if (ForeignCollectionProp.IsModel())
                clone.ForeignCollectionProp = ForeignCollectionProp.RequiredStore;

            return clone;
        }
    }
}