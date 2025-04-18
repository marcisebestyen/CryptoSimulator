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
            CreateMap<UserPutDto, User>();
            CreateMap<UserPostDto, User>();
            CreateMap<UserPutDto, User>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null &&
                !(srcMember is string && string.IsNullOrEmpty((string)srcMember))));

            // Wallet mappings
            CreateMap<Wallet, WalletGetDto>();
            CreateMap<WalletPutDto, Wallet>();
            CreateMap<WalletPostDto, Wallet>();

            // Transactions mappings
            CreateMap<Transactions, TransactionsGetDto>();
            CreateMap<TransactionsPutDto, Transactions>();
            CreateMap<TransactionsPostDto, Transactions>();

            // MyCryptos mappings
            CreateMap<MyCryptos, MyCryptosDto>();
            CreateMap<MyCryptosDto, MyCryptos>();

            // CryptoLog mappings
            CreateMap<CryptoLog, CryptoLogDto>();
            CreateMap<CryptoLogDto, CryptoLog>();

            // Crypto mappings
            CreateMap<Crypto, CryptoGetDto>();
            CreateMap<CryptoPutDto, Crypto>();
            CreateMap<CryptoPostDto, Crypto>();
        }
    }
}
