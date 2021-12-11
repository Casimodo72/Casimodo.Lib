using System;

namespace Casimodo.Lib.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class KeyInfoAttribute : Attribute
    {
        public string PropName { get; set; }
    }
}