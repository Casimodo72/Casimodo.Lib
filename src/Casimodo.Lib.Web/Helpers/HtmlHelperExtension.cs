using System;
using System.Globalization;
using System.Web.Mvc;

namespace Casimodo.Lib.Web
{
    public static class HtmlHelperExtension
    {
        public static DateTimeFormatInfo DateTimeFormat(this HtmlHelper helper)
        {
            return CultureInfo.CurrentUICulture.DateTimeFormat;
        }

        public static object LookupInfo<TModel>(this HtmlHelper helper, string prop, string route)
        {
            return (object)HArg.O().O(Display: HProp.Display(typeof(TModel), prop), Route: route);
        }

        public static string GetDateTimePattern(this HtmlHelper helper, bool date = true, bool dlong = false, bool time = true, bool tlong = true, int ms = 0, bool placeholder = false)
        {
            var format = CultureInfo.CurrentUICulture.DateTimeFormat;
            string result = "";

            if (placeholder)
                result += "{0:";

            if (date)
                result += dlong ? format.LongDatePattern : format.ShortDatePattern;

            if (time)
            {
                if (!string.IsNullOrEmpty(result))
                    result += " ";
                result += tlong ? format.LongTimePattern : format.ShortTimePattern;
            }

            if (ms > 0)
            {
                if (!string.IsNullOrEmpty(result))
                    result += ".";

                result += "".PadRight(Math.Min(ms, 3), 'f');
            }

            if (placeholder)
                result += "}";

            return result;
        }

        public static string ActiveClass(this UrlHelper urlHelper, string controller)
        {
            string result = "active";

            string controllerName = urlHelper.RequestContext.RouteData.Values["controller"].ToString();

            if (!controllerName.Equals(controller, StringComparison.OrdinalIgnoreCase))
                result = null;

            return result;
        }
    }
}