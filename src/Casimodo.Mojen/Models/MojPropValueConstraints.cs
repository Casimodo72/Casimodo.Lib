using Casimodo.Lib.Data;
using System;
using System.Runtime.Serialization;

namespace Casimodo.Lib.Mojen
{
    public interface IMojCloneableConfig
    {
        object Clone();
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojPropValueConstraints : IMojCloneableConfig, ICloneable
    {
        public static readonly MojPropValueConstraints None = new() { Is = false };

        public MojPropValueConstraints()
        {
            Is = true;
        }

        [DataMember]
        public bool Is { get; private set; }

        [DataMember]
        public int? Min { get; set; }

        [DataMember]
        public int? Max { get; set; }

        [DataMember]
        public bool IsRequired { get; set; }

        [DataMember]
        public string Regex { get; set; }

        /// <summary>
        /// If true then the property was explicitely defined to be not required.
        /// This is needed for child collection back-references which
        /// must be *not* required.
        /// </summary>
        [DataMember]
        public bool IsNotRequired { get; set; }

        /// <summary>
        /// Indicates whether a value is required when being edited.
        /// Has nothing to do with the database.
        /// Does *not* reflect whether the value is required in the *database*.
        /// </summary>
        [DataMember]
        public bool IsLocallyRequired { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        object IMojCloneableConfig.Clone()
        {
            if (!Is)
                return None;

            return MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return MemberwiseClone();
        }
    }
}
