using AutoMapper;
using MuhasibPro.Business.EntityModel;
using MuhasibPro.Business.EntityModel.SistemModel;
using MuhasibPro.Domain.Entities;
using MuhasibPro.Domain.Entities.SistemEntity;

namespace Muhasib.Business.Mapping;

public class GeneralMapping : Profile
{
    public GeneralMapping()
    {
        //BaseEntity <->BaseModel için ortak kurallar
        CreateMap<BaseEntity, BaseModel>()
            .IncludeAllDerived()
            .ForMember(dest => dest.KayitTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.KaydedenId, opt => opt.Ignore())
            .ReverseMap()
            .IncludeAllDerived()
            .ForMember(
                dest => dest.Id,
                opt =>
                {
                    opt.Condition((src, dest) => dest?.Id == 0); // Koşulu belirleyin
                    opt.MapFrom(src => src.Id); // MapFrom'u ekleyin
                })
            .ForMember(dest => dest.KayitTarihi, opt => opt.Ignore())
            .ForMember(dest => dest.KaydedenId, opt => opt.Ignore());

        CreateMap<SistemLog, SistemLogModel>()
            .IncludeBase<BaseEntity, BaseModel>()
            .ReverseMap()
            .IncludeBase<BaseModel, BaseEntity>();

        CreateMap<Kullanici, KullaniciModel>()            
            .IncludeBase<BaseEntity, BaseModel>()
            .PreserveReferences()
            .ReverseMap()
            .IncludeBase<BaseModel, BaseEntity>();

        CreateMap<Hesap, HesapModel>()
            .IncludeBase<BaseEntity, BaseModel>()
            .PreserveReferences()
            .ReverseMap();



        CreateMap<Firma, FirmaModel>()
            .IncludeBase<BaseEntity, BaseModel>()
            .ForMember(dest => dest.MaliDonemler, opt => opt.MapFrom(src => src.MaliDonemler))

            .PreserveReferences()
            .ReverseMap()
            .IncludeBase<BaseModel, BaseEntity>();

        CreateMap<MaliDonem, MaliDonemModel>()
            .IncludeBase<BaseEntity, BaseModel>()
            .ForMember(dest => dest.FirmaModel, opt => opt.MapFrom(src => src.Firma))

            .PreserveReferences()
            .ReverseMap()
            .IncludeBase<BaseModel, BaseEntity>();


    }
}
