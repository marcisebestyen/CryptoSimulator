using CryptoSimulator.DTOs;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CryptoSimulator.Controllers
{
    [Route("api/alerts")]
    [ApiController]
    public class AlertsController : ControllerBase
    {
        private readonly IPriceAlertService _priceAlertService;
        private readonly ILogger<AlertsController> _logger;

        public AlertsController(IPriceAlertService service, ILogger<AlertsController> logger)
        {
            _priceAlertService = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PriceAlertGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        public async Task<ActionResult<PriceAlertGetDto>> CreateAlert([FromBody] PriceAlertCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreateAlert called with invalid model state.");
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Attempting to create alert for UserId: {UserId}, CryptoId: {CryptoId}", dto.UserId, dto.CryptoId);

            var (alert, error) = await _priceAlertService.CreateAlertAsync(dto);

            if (error != null)
            {
                _logger.LogWarning("Failed to create alert: {Error}", error);
                if (error.Contains("not found", System.StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { message = error });
                }
                return BadRequest(new { message = error });
            }

            if (alert == null)
            {
                _logger.LogError("Alert creation returned null DTO without an error message for UserId: {UserId}, CryptoId: {CryptoId}", dto.UserId, dto.CryptoId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while creating the alert." });
            }

            _logger.LogInformation("Successfully created alert with Id: {AlertId} for UserId: {UserId}", alert.Id, alert.UserId);

            return Created($"/api/alerts/{alert.Id}", alert);
        }

        [HttpGet("{userId:int}")] 
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PriceAlertGetDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<ActionResult<IEnumerable<PriceAlertGetDto>>> GetActiveAlertsForUser(int userId)
        {
            _logger.LogInformation("Attempting to get active alerts for UserId: {UserId}", userId);

            var (alerts, error) = await _priceAlertService.GetActiveAlertsForUserAsync(userId);

            if (error != null)
            {
                _logger.LogWarning("Failed to get active alerts for UserId {UserId}: {Error}", userId, error);
                if (error.Contains("not found", System.StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { message = error });
                }
                return BadRequest(new { message = error });
            }

            if (alerts == null)
            {
                _logger.LogError("Fetching active alerts for UserId {UserId} returned null DTO list without an error message being set by the service.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching alerts." });
            }

            _logger.LogInformation("Successfully fetched {AlertCount} active alerts for UserId: {UserId}", alerts.Count(), userId);
            return Ok(alerts);
        }

        [HttpDelete("{alertId:int}")] 
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<IActionResult> DeleteAlert(int alertId)
        {
            _logger.LogInformation("Attempting to delete alert with ID: {AlertId}", alertId);

            var (success, error) = await _priceAlertService.DeleteAlertAsync(alertId);

            if (!success)
            {
                if (!string.IsNullOrEmpty(error))
                {
                    if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Deletion failed for alert ID {AlertId}: {Error}", alertId, error);
                        return NotFound(new { message = error });
                    }

                    _logger.LogError("Deletion failed for alert ID {AlertId} due to a server-side issue reported by the service: {Error}", alertId, error);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = error });
                }

                _logger.LogError("Deletion failed for alert ID {AlertId} with no specific error message from service.", alertId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while deleting the alert." });
            }

            _logger.LogInformation("Alert with ID: {AlertId} successfully deleted.", alertId);
            return NoContent();
        }

        [HttpGet("history/{userId:int}")] 
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PriceAlertGetDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        public async Task<ActionResult<IEnumerable<PriceAlertGetDto>>> GetAlertHistoryForUser(int userId)
        {
            _logger.LogInformation("Attempting to get triggered alert history for UserId: {UserId}", userId);

            var (alerts, error) = await _priceAlertService.GetTriggeredAlertsHistoryForUserAsync(userId);

            if (error != null)
            {
                _logger.LogWarning("Failed to get triggered alert history for UserId {UserId}: {Error}", userId, error);
                if (error.Contains("not found", System.StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { message = error });
                }
                return BadRequest(new { message = error }); 
            }

            if (alerts == null)
            {
                _logger.LogError("Fetching triggered alert history for UserId {UserId} returned null DTO list without an error message from service.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while fetching alert history." });
            }

            _logger.LogInformation("Successfully fetched {AlertCount} triggered alert history items for UserId: {UserId}", alerts.Count(), userId);
            return Ok(alerts);
        }
    }
}
