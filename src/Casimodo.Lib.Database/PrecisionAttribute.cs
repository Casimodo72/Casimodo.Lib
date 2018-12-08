using System;

namespace Casimodo.Lib.Data
{
    public class PrecisionAttribute : Attribute
    {
        public PrecisionAttribute(byte precision, byte scale)
        {
            Precision = precision;
            Scale = scale;
        }

        public byte Precision { get; set; }

        public byte Scale { get; set; }
    }
}