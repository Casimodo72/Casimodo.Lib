using System;

namespace Casimodo.Lib.Web
{
    // ASP Core Response Caching: https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-2.2

    // KABU TODO: IMPORTANT: IMPL or remove. This is currently a dummy.
    [AttributeUsageAttribute(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class CustomResponseCacheAttribute : Attribute // ASP Core: ResponseCacheAttribute  // ASP: OutputCacheAttribute 
    {
        public string CacheProfileName { get; set; }
        public bool Revalidate { get; set; }

        //public override void OnResultExecuting(ResultExecutingContext filterContext)
        //{
        //    if (Revalidate)
        //        filterContext.HttpContext.Response.Cache.AppendCacheExtension("must-revalidate, proxy-revalidate");

        //    //if (Location == OutputCacheLocation.ServerAndClient)
        //    //    filterContext.HttpContext.Response.Cache.SetOmitVaryStar(true);

        //    base.OnResultExecuting(filterContext);
        //}
    }
}
