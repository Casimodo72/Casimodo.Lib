namespace Casimodo.Mojen
{
    public sealed class MojInterfaceBuilder : MojTypeBuilder<MojInterfaceBuilder, MojInterfacePropBuilder>
    {
        public MojInterfaceBuilder(MojType type)
        {
            TypeConfig = type;
        }

        public MojInterfaceBuilder Inherit(MojType iface, bool store = true)
        {
            Guard.ArgNotNull(iface, nameof(iface));

            if (iface.Kind != MojTypeKind.Interface)
                throw new MojenException("The given MojType must be an interface type.");

            return Interface(iface.ClassName, store);
        }

        public MojInterfaceBuilder Interface(string name, bool store = true)
        {
            var item = TypeConfig.Interfaces.FirstOrDefault(x => x.Name == name);
            if (item != null)
            {
                // Update
                item.AddToStore = store;
            }
            else
            {
                // Create
                TypeConfig.Interfaces.Add(new MojInterface { Name = name, AddToStore = store });
            }

            return This();
        }
    }
}