using System;
using System.Reflection;

namespace Casimodo.Lib.Data
{
    public static class MoAutoMapperInitializer
    {
        /// <summary>
        /// Maps entity view model properties according to their [StoreMapping] attributes.
        /// </summary>
        public static void CreateMap<TSource, TDestination>(AutoMapper.IMapperConfiguration config)
        {
            CreateMap(typeof(TSource), typeof(TDestination), config);
        }

        /// <summary>
        /// Maps entity view model properties according to their [StoreMapping] attributes.
        /// </summary>
        public static void CreateMap(Type source, Type dest, AutoMapper.IMapperConfiguration config)
        {
            PropertyInfo destProp;
            StoreMappingAttribute sourceMap;
            bool ignore;
            var map = config.CreateMap(source, dest);

            foreach (var sourceProp in source.GetProperties(BindingFlags.Public))
            {
                destProp = dest.GetProperty(sourceProp.Name, BindingFlags.Public);
                if (destProp == null)
                    continue;

                ignore = true;

                sourceMap = GetMappingAttr(sourceProp);
                if (sourceMap != null && sourceMap.To)
                    ignore = false;

                if (ignore)
                    map.ForSourceMember(sourceProp.Name, (o) => o.Ignore());
            }
        }

        static StoreMappingAttribute GetMappingAttr(PropertyInfo prop)
        {
            var attrs = (StoreMappingAttribute[])prop.GetCustomAttributes(typeof(StoreMappingAttribute), false);
            if (attrs == null || attrs.Length == 0)
                return null;

            return attrs[0];
        }
    }
}