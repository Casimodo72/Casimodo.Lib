using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojVersionMapping : MojBase
    {
        public static readonly MojVersionMapping None = new MojVersionMapping(false);

        public MojVersionMapping()
            : this(true)
        { }

        MojVersionMapping(bool @is)
        {
            Is = @is;
        }

        [DataMember]
        public bool Is { get; set; }

        [DataMember]
        public bool? HasSource { get; set; }

        [DataMember]
        public bool IsIncludePropsByDefault { get; set; }

        [DataMember]
        public string SourceName { get; set; }

        [DataMember]
        public string SourcePluralName { get; set; }

        [DataMember]
        public string TargetName { get; set; }

        [DataMember]
        public List<string> IgnoreSourceProps { get { return _ignorePreviousProps ?? (_ignorePreviousProps = new List<string>()); } }
        List<string> _ignorePreviousProps;

        [DataMember]
        public List<MojVersionMapping> ToPropOverrides { get; set; } = new List<MojVersionMapping>();

        [DataMember]
        public string ValueExpression { get; set; }

        public void InheritFrom(MojVersionMapping source)
        {
            if (!Is) throw new MojenException("This version mapping is immutable.");
            if (!source.Is)
                return;

            IgnoreSourceProps.AddRangeDistinctBy(source.IgnoreSourceProps, x => x);
        }

        public static MojVersionMapping CloneFrom(MojVersionMapping source)
        {
            if (!source.Is)
                return source;

            var clone = new MojVersionMapping();
            clone.AssignFromCore(source);

            return clone;
        }

        void AssignFromCore(MojVersionMapping source)
        {
            if (!Is) throw new MojenException("This version mapping is immutable.");
            if (!source.Is) throw new MojenException("Immutable version mapping must not be assigned.");

            HasSource = source.HasSource;
            SourceName = source.SourceName;
            SourcePluralName = source.SourcePluralName;
            ValueExpression = source.ValueExpression;
            IgnoreSourceProps.Clear();
            IgnoreSourceProps.AddRange(source.IgnoreSourceProps);
            ToPropOverrides.Clear();
            ToPropOverrides.AddRange(source.ToPropOverrides);
        }
    }
}