using CryptoSimulator.DTOs;

namespace CryptoSimulator.Services
{
    public interface IPriceAlertService
    {
        Task<(PriceAlertGetDto? alert, string? error)> CreateAlertAsync(PriceAlertCreateDto dto);
        Task CheckAlertsAsync(int cryptoId, decimal currentPrice);
        Task<(IEnumerable<PriceAlertGetDto>? alerts, string? error)> GetActiveAlertsForUserAsync(int userId);
        Task<(bool success, string? error)> DeleteAlertAsync(int alertId);
        Task<(IEnumerable<PriceAlertGetDto>? alerts, string? error)> GetTriggeredAlertsHistoryForUserAsync(int userId);

    }
}
