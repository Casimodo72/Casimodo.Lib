using System.ComponentModel.DataAnnotations;

namespace Casimodo.Lib.Data
{
    public class LocallyRequiredAttribute : RequiredAttribute
    {

    }

#if (false)
    public class LocallyRequiredAttribute : ValidationAttribute
    {
        public LocallyRequiredAttribute()
            : base(GetErrorMessage())
        { }

        static string GetErrorMessage()
        {
            // TODO: LOCALIZE
            return "Ein Wert für '{0}' wird benötigt.";
                // "The {0} field is required.";
        }

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return false;
            }
            string str = value as string;
            if ((str != null) && !this.AllowEmptyStrings)
            {
                return (str.Trim().Length != 0);
            }
            return true;
        }

        public bool AllowEmptyStrings { get; set; }
    }
#endif
}