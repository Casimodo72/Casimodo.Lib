using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.UI;

namespace Casimodo.Lib.Web
{
    public class CustomOutputCacheAttribute : OutputCacheAttribute
    {
        //public override void OnResultExecuting(ResultExecutingContext filterContext)
        //{
        //    if (Location == OutputCacheLocation.ServerAndClient)
        //        filterContext.HttpContext.Response.Cache.SetOmitVaryStar(true);

        //    base.OnResultExecuting(filterContext);
        //}
    }
}
