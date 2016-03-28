using System;

namespace Casimodo.Lib.Data
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class TrackChangesAttribute : Attribute
    { }
}