using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using Microsoft.Identity.Client;

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
    public class PriceAlertService : IPriceAlertService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<PriceAlertService> _logger;

        public PriceAlertService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<PriceAlertService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<(PriceAlertGetDto? alert, string? error)> CreateAlertAsync(PriceAlertCreateDto dto)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(new object[] { dto.UserId });
            if (user == null)
            {
                return (null, $"User with ID {dto.UserId} not found.");
            }

            var crypto = await _unitOfWork.CryptoRepository.GetByIdAsync(new object[] { dto.CryptoId });
            if (crypto == null)
            {
                return (null, $"Crypto with ID {dto.CryptoId} not found.");
            }

            PriceAlertType alertTypeEnum;
            if (string.Equals(dto.AlertType, "above", StringComparison.OrdinalIgnoreCase))
            {
                alertTypeEnum = PriceAlertType.Above;
            }
            else if (string.Equals(dto.AlertType, "below", StringComparison.OrdinalIgnoreCase))
            {
                alertTypeEnum = PriceAlertType.Below;
            }
            else
            {
                return (null, "Invalid AlertType. Must be 'above' or 'below'.");
            }

            var newAlert = new PriceAlert
            {
                UserId = dto.UserId,
                CryptoId = dto.CryptoId,
                TargetPrice = dto.TargetPrice,
                AlertType = alertTypeEnum,
                IsActive = true,
                HasBeenNotifiedForCurrentState = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.PriceAlertRepository.InsertAsync(newAlert);
            await _unitOfWork.SaveAsync();

            var alertGetDto = _mapper.Map<PriceAlertGetDto>(newAlert);
            if (alertGetDto != null && crypto != null)
            {
                alertGetDto.CryptoName = crypto.Name;
            }

            return (alertGetDto, null);
        }

        public async Task CheckAlertsAsync(int cryptoId, decimal currentPrice)
        {
            var activeAlerts = await _unitOfWork.PriceAlertRepository.GetAsync(
                a => a.CryptoId == cryptoId && a.IsActive
            );

            if (!activeAlerts.Any())
            {
                return;
            }

            bool changesMade = false;

            foreach (var alert in activeAlerts)
            {
                bool alertTriggeredThisCycle = false;

                if (alert.AlertType == PriceAlertType.Below)
                {
                    if (currentPrice < alert.TargetPrice)
                    {
                        if (!alert.HasBeenNotifiedForCurrentState)
                        {
                            _logger.LogInformation($"ALERT TRIGGERED (BELOW): UserId: {alert.UserId}, CryptoId: {alert.CryptoId}, Name: {alert.Crypto?.Name ?? "N/A"}. Price {currentPrice:F2} is below target {alert.TargetPrice:F2}. AlertId: {alert.Id}");
                            alert.HasBeenNotifiedForCurrentState = true;
                            alert.LastTriggeredAt = DateTime.UtcNow;
                            alertTriggeredThisCycle = true;
                        }
                    }
                    else // currentPrice >= alert.TargetPrice
                    {
                        if (alert.HasBeenNotifiedForCurrentState)
                        {
                            _logger.LogInformation($"ALERT RESET (BELOW): UserId: {alert.UserId}, CryptoId: {alert.CryptoId}, Name: {alert.Crypto?.Name ?? "N/A"}. Price {currentPrice:F2} is no longer below target {alert.TargetPrice:F2}. AlertId: {alert.Id}");
                            alert.HasBeenNotifiedForCurrentState = false;
                            alertTriggeredThisCycle = true;
                        }
                    }
                }
                else if (alert.AlertType == PriceAlertType.Above)
                {
                    if (currentPrice > alert.TargetPrice)
                    {
                        if (!alert.HasBeenNotifiedForCurrentState)
                        {
                            _logger.LogInformation($"ALERT TRIGGERED (ABOVE): UserId: {alert.UserId}, CryptoId: {alert.CryptoId}, Name: {alert.Crypto?.Name ?? "N/A"}. Price {currentPrice:F2} is above target {alert.TargetPrice:F2}. AlertId: {alert.Id}");
                            alert.HasBeenNotifiedForCurrentState = true;
                            alert.LastTriggeredAt = DateTime.UtcNow;
                            alertTriggeredThisCycle = true;
                        }
                    }
                    else // currentPrice <= alert.TargetPrice
                    {
                        if (alert.HasBeenNotifiedForCurrentState)
                        {
                            _logger.LogInformation($"ALERT RESET (ABOVE): UserId: {alert.UserId}, CryptoId: {alert.CryptoId}, Name: {alert.Crypto?.Name ?? "N/A"}. Price {currentPrice:F2} is no longer above target {alert.TargetPrice:F2}. AlertId: {alert.Id}");
                            alert.HasBeenNotifiedForCurrentState = false;
                            alertTriggeredThisCycle = true;
                        }
                    }
                }

                if (alertTriggeredThisCycle)
                {
                    _unitOfWork.PriceAlertRepository.Update(alert);
                    changesMade = true;
                }
            }

            if (changesMade)
            {
                await _unitOfWork.SaveAsync();
                _logger.LogDebug("Price alert status changes saved for CryptoId: {CryptoId}", cryptoId);
            }
        }

        public async Task<(IEnumerable<PriceAlertGetDto>? alerts, string? error)> GetActiveAlertsForUserAsync(int userId)
        {
            _logger.LogInformation("Attempting to fetch active alerts for UserId: {UserId}", userId);

            var user = await _unitOfWork.UserRepository.GetByIdAsync(new object[] { userId });
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found when fetching active alerts.", userId);
                return (null, $"User with ID {userId} not found.");
            }

            var activeAlertsEntities = 
                await _unitOfWork.PriceAlertRepository.
                GetAsync(alert => alert.UserId == userId && 
                        alert.IsActive,includeProperties: new[]{ "Crypto" });

            if (activeAlertsEntities == null) 
            {
                _logger.LogWarning("Fetching active alerts for UserId {UserId} resulted in a null entity list.", userId);
                return (new List<PriceAlertGetDto>(), null); 
            }

            if (!activeAlertsEntities.Any())
            {
                _logger.LogInformation("No active alerts found for UserId: {UserId}", userId);
                return (new List<PriceAlertGetDto>(), null);
            }

            var alertDtos = _mapper.Map<IEnumerable<PriceAlertGetDto>>(activeAlertsEntities);

            _logger.LogInformation("Successfully fetched {AlertCount} active alerts for UserId: {UserId}", alertDtos.Count(), userId);
            return (alertDtos, null);


        }

        public async Task<(bool success, string? error)> DeleteAlertAsync(int alertId)
        {
            _logger.LogInformation("Attempting to delete alert with ID: {AlertId}", alertId);

            var alertToDelete = await _unitOfWork.PriceAlertRepository.GetByIdAsync(new object[] { alertId });

            if (alertToDelete == null)
            {
                _logger.LogWarning("Alert with ID {AlertId} not found for deletion.", alertId);
                return (false, $"Alert with ID {alertId} not found.");
            }

            try
            {
                await _unitOfWork.PriceAlertRepository.DeleteAsync(alertId);
                await _unitOfWork.SaveAsync();
                _logger.LogInformation("Successfully deleted alert with ID: {AlertId}", alertId);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting alert with ID: {AlertId}", alertId);
                return (false, $"Failed to delete alert with ID {alertId}. Error: {ex.Message}");
            }
        }

        public async Task<(IEnumerable<PriceAlertGetDto>? alerts, string? error)> GetTriggeredAlertsHistoryForUserAsync(int userId)
        {
            _logger.LogInformation("Attempting to fetch triggered alert history for UserId: {UserId}", userId);

            var user = await _unitOfWork.UserRepository.GetByIdAsync(new object[] { userId });
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found when fetching triggered alert history.", userId);
                return (null, $"User with ID {userId} not found.");
            }

            var triggeredAlertEntities = await _unitOfWork.PriceAlertRepository.GetAsync(
                alert => alert.UserId == userId && alert.LastTriggeredAt != null,
                includeProperties: new[] { "Crypto" }
            );

            if (triggeredAlertEntities == null)
            {
                _logger.LogWarning("Fetching triggered alert history for UserId {UserId} resulted in a null entity list from repository.", userId);
                return (new List<PriceAlertGetDto>(), null); 
            }

            if (!triggeredAlertEntities.Any())
            {
                _logger.LogInformation("No triggered alert history found for UserId: {UserId}", userId);
                return (new List<PriceAlertGetDto>(), null);             }

            var orderedAlertEntities = triggeredAlertEntities.OrderByDescending(alert => alert.LastTriggeredAt);

            var alertDtos = _mapper.Map<IEnumerable<PriceAlertGetDto>>(orderedAlertEntities);

            _logger.LogInformation("Successfully fetched {AlertCount} triggered alert history items for UserId: {UserId}", alertDtos.Count(), userId);
            return (alertDtos, null);
        }
    }
}
