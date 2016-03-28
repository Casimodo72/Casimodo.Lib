using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    [KnownType("GetKnownTypes")]
    public class MojValueSet<TValue> : MojValueSet
        where TValue : IComparable
    {
        //public sealed override object ValueObject
        //{
        //    get { return Value; }
        //}

        //[DataMember]
        //public TValue Value { get; set; }

        static Type[] GetKnownTypes()
        {
            return new Type[] { typeof(MojValueSet<TValue>) };
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public abstract class MojValueSet : MojBase
    {
        public MojValueSet()
        {
            Values = new List<MojValueSetProp>();
            AuthRoles = new List<string>();
        }

        [DataMember]
        public int SetId { get; set; }

        [DataMember]
        public List<MojValueSetProp> Values { get; private set; }

        //[DataMember]
        //public int Index { get; set; }

        [DataMember]
        public bool IsNull { get; set; }

        [DataMember]
        public bool IsDefault { get; set; }
        
        public string Description { get; set; }

        [DataMember]
        public List<string> AuthRoles { get; private set; }        

        public MojValueSetProp Get(string name, bool create = false)
        {
            var prop = Find(name);
            if (prop == null)
            {
                if (create)
                {
                    prop = new MojValueSetProp { Name = name };
                    Values.Add(prop);
                }
                else
                    throw new MojenException(string.Format("Value property '{0}' not found.", name));
            }

            return prop;
        }

        public MojValueSetProp Find(string name)
        {
            return Values.FirstOrDefault(x => x.Name == name);
        }

        public bool Has(string name)
        {
            return null != Find(name);
        }

        public bool TryGetValueOf(string name, out object value)
        {
            value = null;
            for (int i = 0; i < Values.Count; i++)
            {
                if (Values[i].Name == name)
                {
                    value = Values[i].Value;
                    return true;
                }
            }

            return false;
        }
    }
}