using System;

namespace Casimodo.Lib.Data
{
    /// <summary>
    /// Used by the MoAutoMapperInitializer for generation of AutoMapper 
    /// mappings of model properties to entity properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class StoreMappingAttribute : Attribute
    {
        public StoreMappingAttribute(bool from = true, bool to = true)
        {
            From = from;
            To = to;
        }

        public bool From { get; set; }

        public bool To { get; set; }
    }
}