namespace Casimodo.Mojen
{
    //[DataContract(Namespace = ContractConfig.Ns)]
    [Serializable]
    public class MojAttrs : List<MojAttr>
    {
        static readonly List<MojAttrAccessor> _registeredAttrs = [];

        static MojAttrs()
        {
            Add(new MojPrecisionAttr());
        }

        static void Add(MojAttrAccessor attr)
        {
            _registeredAttrs.Add(attr);
        }

        public MojAttrArg GetDefaultValueArg()
        {
            var item = this.FirstOrDefault(x => x.Name == "DefaultValue");
            if (item != null)
                return item.Args.First();

            return null;
        }

        public string GetDisplayName()
        {
            var item = this.FirstOrDefault(x => x.Name == "Display");
            if (item == null)
                return null;


            var arg = item.Args.First();
            return (string)arg.Value;
        }

        public void AddOrReplace(MojAttr attr)
        {
            var existing = Find(attr.Name);
            if (existing != null)
                Remove(existing);

            Add(attr);
        }

        public bool Contains(string name)
        {
            return null != this.FirstOrDefault(x => x.Name == name);
        }

        public MojAttr Remove(string name)
        {
            var item = Find(name);
            if (item != null)
                Remove(item);

            return item;
        }

        public MojAttr Find(string name)
        {
            return this.FirstOrDefault(x => x.Name == name);
        }

        public T Find<T>() where T : MojAttrAccessor, new()
        {
            var accessor = _registeredAttrs.OfType<T>().FirstOrDefault();
            if (accessor == null)
                return null;

            var attr = this.FirstOrDefault(x => x.Name == accessor.Name);
            if (attr == null)
                return null;

            return new T
            {
                Attr = attr
            };
        }

        public MojAttr FindOrCreate(string name, int order)
        {
            var attr = this.FirstOrDefault(x => x.Name == name);
            if (attr == null)
                attr = new MojAttr(name, order);

            return attr;
        }
    }
}