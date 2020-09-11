using System;

namespace Casimodo.Lib.Web
{
    [AttributeUsageAttribute(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ODataConfigureAttribute : Attribute
    { }
}
