using System;
using System.Linq;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;
using System.Collections.Generic;

namespace Casimodo.Lib.Mojen
{
    public enum MojIndexPropKind
    {
        IndexMember = 0,
        TenantIndexMember = 1,
        StartSelector = 2,
        EndSelector = 3
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojUniqueParameterConfig
    {
        [DataMember]
        public bool Is { get; set; }

        [DataMember]
        public MojIndexPropKind Kind { get; set; }

        [DataMember]
        public MojProp Prop { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojDbPropAnnotation : MojBase
    {
        public static readonly MojDbPropAnnotation None = new() { Is = false };

        public MojDbPropAnnotation()
        { }

        public MojDbPropAnnotation(MojProp prop)
        {
            Prop = prop;
            Is = true;
        }

        [DataMember]
        public MojProp Prop { get; set; }

        [DataMember]
        public bool Is { get; set; }

        [DataMember]
        public MojUniqueConfig Unique { get; set; } = MojUniqueConfig.None;

        [DataMember]
        public MojIndexConfig Index { get; set; } = MojIndexConfig.None;

        [DataMember]
        public MojSequenceConfig Sequence { get; set; } = MojSequenceConfig.None;

        public string GetIndexName()
        {
            if (!Is || Prop == null) throw new InvalidOperationException("This property DB annotation does not apply.");

            return GetIndexNameCore();
        }

        public int GetIndexMemberIndex(MojProp prop)
        {
            var index = Unique.GetMembers().ToList().FindIndex(per => Moj.AreSame(per.Prop, prop));
            if (index == -1)
            {
                // The actual target property always comes last.
                if (Moj.AreSame(Prop, prop))
                    return Unique.GetMembers().Count();
            }

            if (index == -1)
                throw new MojenException($"Property '{prop.Name}' is not a member of index '{GetIndexName()}'.");

            return index;
        }

        string GetIndexNameCore()
        {
            if (!Is || Prop == null)
                return "None";
            return (Unique.Is ? "U" : "") + "IX_" + Prop.Name;
        }

        public override string ToString()
        {
            return $"I: {Index.Is}, U: {Unique.Is}, S: {Sequence.Is}, Name: {GetIndexNameCore()}";
        }
    }

    public interface IMojErrorMessageHolder
    {
        string ErrorMessage { get; set; }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojUniqueConfig : MojBase, IMojErrorMessageHolder
    {
        public static readonly MojUniqueConfig None = new() { Is = false };

        [DataMember]
        public bool Is { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        public bool HasParams
        {
            get { return _parameters.Count != 0; }
        }

        [DataMember]
        internal List<MojUniqueParameterConfig> _parameters = new();

        public IEnumerable<MojUniqueParameterConfig> GetParams(bool includeTenant = false)
        {
            return _parameters.DistinctBy(x => x.Prop.Name).Where(x => FilterTenant(x, includeTenant));
        }

        bool FilterTenant(MojUniqueParameterConfig item, bool include)
        {
            return include || item.Kind != MojIndexPropKind.TenantIndexMember;
        }

        public bool HasUniqePerConstraint
        {
            get { return GetMembers().Any(); }
        }

        /// <summary>
        /// KABU TODO: Does currently *not* contain the actual unique property,
        /// only other participating properties.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MojUniqueParameterConfig> GetMembers()
        {
            return _parameters.Where(x => 
                x.Kind == MojIndexPropKind.IndexMember ||
                x.Kind == MojIndexPropKind.TenantIndexMember);
        }

        public bool IsMember(MojProp prop)
        {
            prop = prop.StoreOrSelf;
            return GetMembers().Any(per => Moj.AreSame(per.Prop, prop));
        }

        public MojUniqueConfig Clone()
        {
            var clone = (MojUniqueConfig)MemberwiseClone();
            clone._parameters = new List<MojUniqueParameterConfig>(_parameters);

            return clone;
        }
    }
}