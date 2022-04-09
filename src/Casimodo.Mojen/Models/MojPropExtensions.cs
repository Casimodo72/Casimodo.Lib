namespace Casimodo.Mojen
{
    public static class MojPropExtensions
    {
        public static string ToMethodArguments(this IEnumerable<MojProp> props)
        {
            if (props == null || !props.Any()) return "";

            string result = "";
            int i = 0;
            foreach (var prop in props)
            {
                if (i++ > 0) result += ", ";
                result += $"{Moj.ToCsType(prop.Type.TypeNormalized)} {prop.VName}";
            }

            return result;
        }

        public static string GetFormedNavigationPath(this MojProp prop)
        {
            if (!prop.IsNavigation || !prop.FormedNavigationFrom.Is)
                return null;

            return prop.FormedNavigationFrom.TargetPath;
        }

        public static string GetFormedForeignKeyPath(this MojProp prop, bool alias = false)
        {
            if (!prop.Reference.Is || prop.Reference.ForeignKey == null)
                return null;

            var path = "";

            // Note: Direct properties do not have a FormedNavigationFrom. E.g. [Contract].CompanyId(loose);                    
            if (prop.FormedNavigationFrom.Is)
                // But indirect have a FormedNavigationFrom.
                // Use the *source path* as first portion of the path.
                // E.g. for [Contract].BusinessContact(nested) -> SalutationId(loose)
                // the source path is "BusinessContact"
                path += prop.FormedNavigationFrom.SourcePath + ".";

            // Finish with the foreign key to the target type.
            // The path becomes "BusinessContact.SalutationId"
            if (alias)
                path += prop.Reference.ForeignKey.Alias;
            else
                path += prop.Reference.ForeignKey.Name;

            return path;
        }
    }
}