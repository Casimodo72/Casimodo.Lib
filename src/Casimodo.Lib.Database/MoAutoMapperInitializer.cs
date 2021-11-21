using System;
using System.Reflection;

namespace Casimodo.Lib.Data
{
    public static class MoAutoMapperInitializer
    {
        /// <summary>
        /// Maps entity view model properties according to their [StoreMapping] attributes.
        /// </summary>
        public static void CreateModelToStoreMap<TSource, TDestination>(AutoMapper.IMapperConfigurationExpression config)
        {
            CreateModelToStoreMap(typeof(TSource), typeof(TDestination), config);
        }

        /// <summary>
        /// Maps entity view model properties according to their [StoreMapping] attributes.
        /// </summary>
        static void CreateModelToStoreMap(Type sourceType, Type destinationType, AutoMapper.IMapperConfigurationExpression config)
        {
            PropertyInfo destProp;
            StoreMappingAttribute sourceMap;
            var map = config.CreateMap(sourceType, destinationType);

            foreach (var sourceProp in sourceType.GetProperties(BindingFlags.Public))
            {
                destProp = destinationType.GetProperty(sourceProp.Name, BindingFlags.Public);
                if (destProp == null)
                    continue;

                sourceMap = GetMappingAttr(sourceProp);

                // KABU TODO: REVISIT: Renamed Ignore to DoNotValidate
                if (sourceMap == null || !sourceMap.To)
                {
                    map.ForSourceMember(sourceProp.Name, (o) => o.DoNotValidate());
                }
            }
        }

        static StoreMappingAttribute GetMappingAttr(PropertyInfo prop)
        {
            return prop.GetCustomAttribute<StoreMappingAttribute>(false);
        }
    }
}