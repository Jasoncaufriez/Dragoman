using AutoMapper;
using Dragoman.Server.Dtos;
using Dragoman.Server.Models;

namespace Dragoman.Server.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
      

            // === TolkTva ===
            CreateMap<TolkTva, TvaRowDto>().ReverseMap();

            // === Langues (exemples) ===
            CreateMap<Langue, LangueDto>().ReverseMap();
            CreateMap<LangueSource, LangueSourceDto>().ReverseMap();
            CreateMap<LangueDestination, LangueDestinationDto>().ReverseMap();
            // === Indisponibilités ===
            CreateMap<Tolkindispo, IndispoDto>().ReverseMap();
            CreateMap<NewIndispoDto, Tolkindispo>(); 

        }
    }
}
