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

        // Source: http://stackoverflow.com/questions/1443647/generate-url-in-html-helper
        public static UrlHelper UrlHelper(this HtmlHelper htmlHelper)
        {
            if (htmlHelper.ViewContext.Controller is Controller)
                return ((Controller)htmlHelper.ViewContext.Controller).Url;

            const string itemKey = "HtmlHelper_UrlHelper";

            if (htmlHelper.ViewContext.HttpContext.Items[itemKey] == null)
                htmlHelper.ViewContext.HttpContext.Items[itemKey] = new UrlHelper(htmlHelper.ViewContext.RequestContext, htmlHelper.RouteCollection);

            return (UrlHelper)htmlHelper.ViewContext.HttpContext.Items[itemKey];
        }

        public static bool GetIsCurrentController(this HtmlHelper helper, string controller)
        {
            return string.Equals(controller, helper.UrlHelper().RequestContext.RouteData.Values["controller"].ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}