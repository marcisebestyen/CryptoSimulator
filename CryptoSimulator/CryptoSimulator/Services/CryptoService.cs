using AutoMapper;
using CryptoSimulator.Data;
using CryptoSimulator.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CryptoSimulator.Services
{
    public interface ICryptoService
    {
        Task<IEnumerable<CryptoGetWithCurrentValueDto>> GetAllWithCurrentValueAsync();
    }

    public class CryptoService : ICryptoService
    {
        private readonly CryptoSimulationDbContext _context;
        private readonly IMapper _mapper;

        public CryptoService(CryptoSimulationDbContext context, IMapper mapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper;
        }

        public async Task<IEnumerable<CryptoGetWithCurrentValueDto>> GetAllWithCurrentValueAsync()
        {
            DateTime maxDateTime = new DateTime(9999, 12, 31, 23, 59, 59);

            var cryptoQuery = from crypto in _context.Cryptos
                              join currentLog in _context.CryptoLogs on crypto.Id equals currentLog.CryptoId
                              where currentLog.To == maxDateTime
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
    }
}
