using System;
using System.Collections.ObjectModel;
//using System.Data.Entity.ModelConfiguration.Configuration;
//using System.Data.Entity.ModelConfiguration.Conventions;

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

    // KABU TODO: IMPORTANT: REVISIT: EF Core conventions not ready yet?
    //     See https://github.com/aspnet/EntityFrameworkCore/issues/13954
#if (false)
    public class PrecisionAttributeConvention : PrimitivePropertyAttributeConfigurationConvention<PrecisionAttribute>
    {
        public override void Apply(ConventionPrimitivePropertyConfiguration config, PrecisionAttribute attribute)
        {
            if (!_supportedTypes.Contains(config.ClrPropertyInfo.PropertyType))
                return;
            config.HasPrecision(attribute.Precision, attribute.Scale);
        }

        static readonly ReadOnlyCollection<Type> _supportedTypes = new ReadOnlyCollection<Type>(new Type[] {
            typeof(decimal), typeof(decimal?)
        });
    }
#endif
}