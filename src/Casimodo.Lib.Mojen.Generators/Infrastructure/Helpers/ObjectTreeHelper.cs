using Casimodo.Lib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Casimodo.Lib.Mojen
{
    public static class ObjectTreeHelper
    {
        public static bool IsReferenceToParent(MojType type, MojSoftReference reference)
        {
            return reference.ToType == type && reference.Axis == MojReferenceAxis.ToParent;
        }

        public static bool IsReferenceToChild(MojProp prop)
        {
            var reference = prop.Reference;

            if (reference.ToType.IsAbstract)
                return false;

            if ((reference.Binding.HasFlag(MojReferenceBinding.Nested) ||
                 reference.Binding.HasFlag(MojReferenceBinding.Owned)))
                return true;

            if (reference.Axis != MojReferenceAxis.ToChild &&
                // KABU TODO: TEMPORARY: Just for dev purposes: allow descendants as well.
                reference.Axis != MojReferenceAxis.ToDescendant)
                return false;

            return true;
        }
    }
}
