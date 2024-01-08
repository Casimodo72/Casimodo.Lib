using System.Runtime.Serialization;

namespace Casimodo.Mojen
{
    public class MojValueSet : MojBase
    {
        public MojValueSet()
        {
            Values = [];
            AuthRoles = [];
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

        [DataMember]
        public string Pw { get; set; }

        public MojValueSetProp Get(string name, bool create = false)
        {
            Guard.ArgNotNull(name, nameof(name));

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

        public void Build()
        {
            foreach (var prop in Values)
                prop.Build(this);
        }

        public MojValueSetProp Find(string name)
        {
            return Values.FirstOrDefault(x => x.Name == name);
        }

        public bool Has(string name)
        {
            return null != Find(name);
        }

        public bool TryGetValue(string name, out object value)
        {
            value = null;
            for (int i = 0; i < Values.Count; i++)
            {
                if (Values[i].Name == name)
                {
                    value = Values[i].GetValue(this);
                    return true;
                }
            }

            return false;
        }
    }
}