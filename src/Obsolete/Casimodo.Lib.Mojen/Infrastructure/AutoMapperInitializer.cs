
namespace Casimodo.Lib.Mojen
{
    public class AutoMapperInitializer
    {
        public void Initialize()
        {
            AutoMapper.Mapper.Initialize(c =>
            {
				c.CreateMap(typeof(MojType), typeof(MojType));

                c.CreateMap<MojProp, MojProp>()
					.ForMember(s => s.DeclaringType, o => o.Ignore());

            });
        }
    }
}