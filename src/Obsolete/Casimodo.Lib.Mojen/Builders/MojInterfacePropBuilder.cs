using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib.Mojen
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