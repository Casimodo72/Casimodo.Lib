using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public sealed class MojComplexTypeBuilder : MojClassBuilder<MojComplexTypeBuilder, MojComplexTypePropBuilder>
    {
        public override MojType Build()
        {
            base.Build();

            return TypeConfig;
        }
    }
}