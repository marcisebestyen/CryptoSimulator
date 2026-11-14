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
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null &&
                !(srcMember is string && string.IsNullOrEmpty((string)srcMember))));

            // Wallet mappings
            CreateMap<Wallet, WalletGetDto>();
            CreateMap<WalletPutDto, Wallet>();

            // Transactions mappings
            CreateMap<Transactions, TransactionsGetDto>();

            // CryptoLog mappings
            CreateMap<CryptoLog, CryptoPriceHistoryDto>()
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.CurrentValue));

            // Crypto mappings
            CreateMap<Crypto, CryptoGetDto>();
            CreateMap<CryptoPostDto, Crypto>();

            // Alerts mappings 
            CreateMap<PriceAlert, PriceAlertGetDto>()
                .ForMember(dest => dest.CryptoName, opt => opt.MapFrom(src => src.Crypto != null ? src.Crypto.Name : null));
        }
    }
}
