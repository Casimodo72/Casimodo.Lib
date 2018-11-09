using System;

namespace Casimodo.Lib.Mojen
{
    public sealed class MojEntityBuilder : MojClassBuilder<MojEntityBuilder, MojEntityPropBuilder>
    {
        public MojEntityBuilder Table(string tableName)
        {
            TypeConfig.TableName = tableName;

            return This();
        }

        public MojEntityBuilder Content(Action<MojEntityBuilder> build)
        {
            build(This());

            return This();
        }

        public override MojType Build()
        {
            base.Build();

            // Check: All unique index member properties must be required.
            foreach (var prop in TypeConfig.LocalProps)
            {
                // KABU TODO: INDEX-PROP-NULLABLE: Currently disabled since in object "Party" we have
                //   two potential index scenarios where only one index is actually active.
                //foreach (var p in prop.DbAnno.Unique.GetParams(includeTenant: true))
                //    if (p.Prop.Type.CanBeNull && !p.Prop.Rules.IsRequired)
                //        throw new MojenException("All unique index member properties must be required or non-nullable.");
            }

            return TypeConfig;
        }
    }
}