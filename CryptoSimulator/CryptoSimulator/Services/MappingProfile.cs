using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;

namespace CryptoSimulator.Services
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<User, UserGetDto>();
            CreateMap<UserPutDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null &&
                !(srcMember is string && string.IsNullOrEmpty((string)srcMember))));

            // Wallet mappings
            CreateMap<Wallet, WalletGetDto>();
            CreateMap<WalletPutDto, Wallet>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            // Transactions mappings
            CreateMap<Transactions, TransactionsGetDto>();

            // CryptoLog mappings
            CreateMap<CryptoLog, CryptoPriceHistoryDto>()
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.CurrentValue));

            // Crypto mappings
            CreateMap<Crypto, CryptoGetDto>();
            CreateMap<CryptoPostDto, Crypto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // Alerts mappings 
            CreateMap<PriceAlert, PriceAlertGetDto>()
                .ForMember(dest => dest.CryptoName, opt => opt.MapFrom(src => src.Crypto != null ? src.Crypto.Name : null));
        }
    }
}
