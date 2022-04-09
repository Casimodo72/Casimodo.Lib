using Casimodo.Lib.Data;

namespace Casimodo.Mojen
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

            // KABU TODO: IMPORTANT: Add validation: nested and owned must not have any other axis that "ToChild".

            if ((reference.Binding.HasFlag(MojReferenceBinding.Nested) ||
                 reference.Binding.HasFlag(MojReferenceBinding.Owned)))
                return true;

            if (reference.Axis != MojReferenceAxis.ToChild)
                return false;

            return true;
        }
    }
}
