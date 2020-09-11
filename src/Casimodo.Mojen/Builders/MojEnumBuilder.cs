namespace Casimodo.Lib.Mojen
{
    public sealed class MojEnumBuilder : MojTypeBuilder<MojEnumBuilder, MojEnumPropBuilder>
    {
        public MojEnumBuilder(MojType type)
        {
            TypeConfig = type;
        }

        public MojEnumBuilder Flags()
        {
            Attr(new MojAttr("Flags", 0));
            TypeConfig.IsFlagsEnum = true;

            return this;
        }

        public new MojEnumPropBuilder Prop(string name, int value)
        {
            var pbuilder = Prop(name, typeof(int));
            pbuilder.PropConfig.EnumValue = value;

            return pbuilder;
        }

        public override MojType Build()
        {
            base.Build();
            return TypeConfig;
        }
    }
}