using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Casimodo.Lib.Mojen
{
    public interface IMojCloneableConfig
    {
        object Clone();
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojPropValueConstraints : IMojCloneableConfig, ICloneable
    {
        public static readonly MojPropValueConstraints None = new MojPropValueConstraints { Is = false };

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

        /// <summary>
        /// If true then the property was explicitely defined to be not required.
        /// This is needed for child collection back-references which
        /// must be *not* required.
        /// </summary>
        [DataMember]
        public bool IsNotRequired { get; set; }

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
