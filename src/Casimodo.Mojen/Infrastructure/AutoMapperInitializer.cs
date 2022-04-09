
using AutoMapper;

namespace Casimodo.Mojen
{
    public class AutoMapperInitializer
    {
        public void Initialize()
        {
            // TODO: Use AutoMapper 10 and eliminate static registration.
            AutoMapper.Mapper.Initialize(cfg => cfg.AddProfile<AutoMapperProfile>());        
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