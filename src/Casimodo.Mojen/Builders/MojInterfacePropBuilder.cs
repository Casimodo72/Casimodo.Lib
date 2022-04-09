namespace Casimodo.Mojen
{
    public sealed class MojInterfacePropBuilder : MojPropBuilder<MojInterfaceBuilder, MojInterfacePropBuilder>
    {
        public MojInterfacePropBuilder Type(MojType type)
        {
            TypeCore(type);

            return this;
        }
    }
}