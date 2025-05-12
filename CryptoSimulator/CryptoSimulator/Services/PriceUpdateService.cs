
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;

namespace CryptoSimulator.Services
{
    public class PriceUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PriceUpdateService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(45);
        private static readonly DateTime MaxDateTimeValue = new DateTime(9999, 12, 31, 23, 59, 59);

        public PriceUpdateService(IServiceProvider serviceProvider, ILogger<PriceUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PriceUpdateService is starting.");

            while(!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdatePricesAndCheckAlertsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("PriceUpdateService stopping due to cancellation.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while updating prices.");
                }

                try
                {
                    await Task.Delay(_updateInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("PriceUpdateService delay was canceled. Stopping service.");
                    break;
                }
            }

            _logger.LogInformation("PriceUpdateService is stopping.");
        }

        private async Task UpdatePricesAndCheckAlertsAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Running price update and alert checking task at {time}", DateTimeOffset.Now);

            using (var scope = _serviceProvider.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var cryptoService = scope.ServiceProvider.GetRequiredService<ICryptoService>();
                var priceAlertService = scope.ServiceProvider.GetRequiredService<IPriceAlertService>();

                var cryptos = await unitOfWork.CryptoRepository.GetAllAsync(); 

                //var cryptoRepository = unitOfWork.CryptoRepository;
                //var cryptos = await cryptoRepository.GetAllAsync();

                foreach(var crypto in cryptos)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        decimal currentPriceBeforeUpdate = await GetCurrentPrice(unitOfWork, crypto.Id);
                        decimal newPrice = GenerateNewPriceExample(crypto.Id, await GetCurrentPrice(unitOfWork, crypto.Id));
                        DateTime updateTimestamp = DateTime.UtcNow;
                        bool processed = await cryptoService.UpdateCryptoPriceAsync(crypto.Id, newPrice, updateTimestamp);

                        if (processed)
                        {
                            await unitOfWork.SaveAsync();
                            _logger.LogDebug("Processed price update for CryptoId {cryptoId} with new price {newPrice} at {timestamp}", crypto.Id, newPrice, updateTimestamp);
                            _logger.LogDebug("Checking alerts for CryptoId {CryptoId} with new price {NewPrice}", crypto.Id, newPrice);
                            await priceAlertService.CheckAlertsAsync(crypto.Id, newPrice);
                        }
                        else
                        {
                            _logger.LogWarning("Price update was not processed for CryptoId {CryptoId}", crypto.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update price for CryptoId {CryptoId}", crypto.Id);
                    }
                }
            }
        }

        private async Task<decimal> GetCurrentPrice(IUnitOfWork unitOfWork, int cryptoId)
        {
            var log = (await unitOfWork.CryptoLogRepository.GetAsync(cl => cl.CryptoId == cryptoId && cl.To == MaxDateTimeValue, null)).FirstOrDefault();
            return log?.CurrentValue ?? 0m;
        }

        private decimal GenerateNewPriceExample(int cryptoId, decimal currentPrice)
        {
            var random = new Random();
            decimal percentageChange = (decimal)(random.NextDouble() * 0.02 - 0.01); 
            decimal newPrice = currentPrice * (1 + percentageChange);

            return Math.Max(0.01m, Math.Round(newPrice, 2));
        }
    }
}
