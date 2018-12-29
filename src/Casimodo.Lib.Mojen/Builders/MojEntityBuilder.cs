using System;
using System.Collections.Generic;

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

        public MojEntityBuilder UniqueIndex(params string[] props)
        {
            return Index(true, props);
        }

        public MojEntityBuilder Index(bool unique, params string[] props)
        {
            Guard.ArgNotEmpty(props, nameof(props));

            var index = new MojIndexConfig { Is = true };
            index.IsUnique = unique;

            if (unique)
            {
                // Add tenant foreign key as member of the unique constraint if available.
                var tenantProp = TypeConfig.FindTenantKey();
                if (tenantProp != null)
                {
                    tenantProp = tenantProp.RequiredStore.ForeignKeyOrSelf;
                    index.Participants.Add(new MojIndexParticipantConfig
                    {
                        Kind = MojIndexPropKind.TenantIndexMember,
                        Prop = tenantProp
                    });
                }
            }

            foreach (var propName in props)
            {
                index.Participants.Add(new MojIndexParticipantConfig
                {
                    Kind = MojIndexPropKind.IndexMember,
                    Prop = TypeConfig.GetProp(propName).RequiredStore.ForeignKeyOrSelf
                });
            }

            TypeConfig.Indexes.Add(index);

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