using AutoMapper;
using CryptoSimulator.Data;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CryptoSimulator.Services
{
    public interface ICryptoService
    {
        Task<IEnumerable<CryptoGetWithCurrentValueDto>> GetAllWithCurrentValueAsync();
        Task<bool> UpdateCryptoPriceAsync(int cryptoId, decimal newPrice, DateTime updateTimestamp);
    }

    public class CryptoService : ICryptoService
    {
        private readonly CryptoSimulationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private static readonly DateTime MaxDateTimeValue = new DateTime(9999, 12, 31, 23, 59, 59);

        public CryptoService(CryptoSimulationDbContext context, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CryptoGetWithCurrentValueDto>> GetAllWithCurrentValueAsync()
        {
            var cryptoQuery = from crypto in _context.Cryptos
                              join currentLog in _context.CryptoLogs on crypto.Id equals currentLog.CryptoId
                              where currentLog.To == MaxDateTimeValue
                              orderby crypto.Name
                              select new CryptoGetWithCurrentValueDto
                              {
                                  Id = crypto.Id,
                                  Name = crypto.Name,
                                  CurrentExchangeRate = currentLog.CurrentValue
                              };

            var results = await cryptoQuery.ToListAsync();
            return results;
        }

        public async Task<bool> UpdateCryptoPriceAsync(int cryptoId, decimal newPrice, DateTime updateTimestamp)
        {
            if (updateTimestamp.Kind != DateTimeKind.Utc)
            {
                updateTimestamp = updateTimestamp.ToUniversalTime();
            }

            var previousLog = (await _unitOfWork.CryptoLogRepository.GetAsync(cl => cl.CryptoId == cryptoId && cl.To == MaxDateTimeValue, null)).FirstOrDefault();

            if (previousLog != null)
            {
                if (updateTimestamp < previousLog.From)
                {
                    Console.WriteLine($"Error: Update timestamp {updateTimestamp} is earlier than previous log's From time {previousLog.From} for CryptoId {cryptoId}.");
                    return false; 
                }
                previousLog.To = updateTimestamp;
                await _unitOfWork.CryptoLogRepository.UpdateAsync(previousLog);
            }

            var newLogEntry = new CryptoLog
            {
                CryptoId = cryptoId,
                CurrentValue = newPrice,
                From = updateTimestamp,
                To = MaxDateTimeValue
            };
            await _unitOfWork.CryptoLogRepository.InsertAsync(newLogEntry);

            return true;
        }
    }
}
