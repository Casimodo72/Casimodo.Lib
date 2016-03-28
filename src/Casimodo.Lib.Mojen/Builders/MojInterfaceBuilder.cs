namespace Casimodo.Lib.Mojen
{
    public sealed class MojInterfaceBuilder : MojTypeBuilder<MojInterfaceBuilder, MojInterfacePropBuilder>
    {
        public MojInterfaceBuilder(MojType type)
        {
            TypeConfig = type;
        }
    }
}