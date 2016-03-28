using System;
using System.Reflection;

namespace Casimodo.Lib.Data
{
    public static class MoAutoMapperInitializer
    {
        /// <summary>
        /// Maps entity view model properties according to their [StoreMapping] attributes.
        /// </summary>
        public static void CreateMap<TSource, TDestination>()
        {
            CreateMap(typeof(TSource), typeof(TDestination));
        }

        /// <summary>
        /// Maps entity view model properties according to their [StoreMapping] attributes.
        /// </summary>
        public static void CreateMap(Type source, Type dest)
        {
            PropertyInfo destProp;
            StoreMappingAttribute sourceMap;
            bool ignore;
            var map = AutoMapper.Mapper.CreateMap(source, dest);

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