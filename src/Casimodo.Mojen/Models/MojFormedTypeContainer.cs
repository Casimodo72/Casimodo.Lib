namespace Casimodo.Mojen
{
    public interface IFormedTypePropAccessor
    {
        MojProp Get(string propName, bool required = false);
    }

    public class MojFormedTypeRegistry : MojBase
    {
        public List<MojFormedTypeContainer> TypeContainers { get; set; } = new List<MojFormedTypeContainer>();
    }

    public class MojFormedTypeContainer : IFormedTypePropAccessor
    {
        internal readonly Dictionary<int, MojProp> _props = new();

        public MojFormedTypeContainer(MojType type)
        {
            Type = type;
            var index = 0;
            foreach (var prop in type.GetProps())
            {
                _props.Add(index, prop);
                index++;
            }
        }

        public MojType Type { get; private set; }

        public MojProp Get(int index)
        {
            return this[index];
        }

        public MojProp Get(string propName, bool required = true)
        {
            var prop = _props.Values.FirstOrDefault(x => x.Name == propName);
            if (prop == null && required)
                throw new MojenException($"Property '{propName}' not found in formed type '{Type.ClassName}'.");

            return prop;
        }

        public MojProp this[int index]
        {
            get { return _props[index]; }
        }
    }
}