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
                    index.Members.Add(new MojIndexMemberConfig
                    {
                        Kind = MojIndexPropKind.TenantIndexMember,
                        Prop = tenantProp
                    });
                }
            }

            foreach (var propName in props)
            {
                index.Members.Add(new MojIndexMemberConfig
                {
                    Kind = MojIndexPropKind.IndexMember,
                    Prop = TypeConfig.GetProp(propName).RequiredStore.ForeignKeyOrSelf
                });
            }

            TypeConfig.Indexes.Add(index);

            return This();
        }
    }
}