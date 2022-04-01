using System.Linq;

namespace Casimodo.Lib.Mojen
{
    public static class MojExtensions
    {
        public static string GetNextSequenceValueMethodName(this MojProp prop)
        {
            return $"GetNextSequenceValueFor{prop.DeclaringType.Name}{prop.Name}";
        }

        public static string GetStartSequenceValueMethodName(this MojProp prop)
        {
            return $"GetStartSequenceValueFor{prop.DeclaringType.Name}{prop.Name}";
        }

        public static string GetEndSequenceValueMethodName(this MojProp prop)
        {
            return $"GetEndSequenceValueFor{prop.DeclaringType.Name}{prop.Name}";
        }

        public static string GetNextSequenceValueMethodParams(this MojProp prop, bool includeTenant = false)
        {
            return prop.DbAnno.Unique.GetParams(includeTenant)
                .Select(per => $"{per.Prop.Type.NameNormalized} {per.Prop.VName}")
                .Join(", ");
        }

        public static string GetNextSequenceValueMethodArgs(this MojProp prop, bool includeTenant = false)
        {
            return prop.DbAnno.Unique.GetParams(includeTenant)
                .Select(per => $"{per.Prop.VName}").Join(", ");
        }

        
    }
}
