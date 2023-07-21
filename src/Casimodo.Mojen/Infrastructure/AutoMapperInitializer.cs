
using AutoMapper;

namespace Casimodo.Mojen
{
    public class AutoMapperInitializer
    {
        public MapperConfiguration Initialize()
        {
            return new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>());
        }

        public class AutoMapperProfile : Profile
        {
            public AutoMapperProfile()
            {
                CreateMap(typeof(MojType), typeof(MojType));

                CreateMap<MojProp, MojProp>()
                    .ForMember(s => s.DeclaringType, o => o.Ignore());
            }
        }
    }
}