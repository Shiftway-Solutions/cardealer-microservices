using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminService.Application.UseCases.Vehicles.ApproveVehicle;
using AdminService.Application.UseCases.Vehicles.RejectVehicle;
using AdminService.Infrastructure.External;

namespace AdminService.Api.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class VehiclesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IVehicleServiceClient _vehicleServiceClient;
        private readonly ILogger<VehiclesController> _logger;

        public VehiclesController(
            IMediator mediator,
            IVehicleServiceClient vehicleServiceClient,
            ILogger<VehiclesController> logger)
        {
            _mediator = mediator;
            _vehicleServiceClient = vehicleServiceClient;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated list of vehicles for admin management
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetVehicles(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? sellerType,
            [FromQuery] bool? featured,
            [FromQuery] bool? hasReports,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            CancellationToken ct = default)
        {
            try
            {
                var filters = new VehicleSearchFilters
                {
                    Search = search,
                    Status = status,
                    SellerType = sellerType,
                    Featured = featured,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _vehicleServiceClient.SearchVehiclesAsync(filters, ct);

                if (result == null)
                {
                    return Ok(new
                    {
                        items = Array.Empty<object>(),
                        total = 0,
                        page,
                        pageSize,
                        totalPages = 0
                    });
                }

                // Map to the format the frontend expects
                var items = result.Vehicles.Select(v => new
                {
                    id = v.Id.ToString(),
                    title = v.Title,
                    make = v.Make,
                    model = v.Model,
                    year = v.Year,
                    price = v.Price,
                    currency = v.Currency,
                    image = v.Images?.FirstOrDefault(i => i.IsPrimary)?.Url
                            ?? v.Images?.FirstOrDefault()?.Url
                            ?? "",
                    status = v.StatusName ?? "active",
                    sellerId = v.SellerId?.ToString() ?? "",
                    sellerName = v.SellerName ?? "Unknown",
                    sellerType = v.SellerType ?? "individual",
                    views = v.ViewCount,
                    leads = v.LeadCount,
                    featured = v.IsFeatured,
                    reportsCount = 0,
                    createdAt = v.CreatedAt.ToString("o"),
                    publishedAt = v.PublishedAt?.ToString("o"),
                    rejectionReason = v.RejectionReason
                }).ToList();

                return Ok(new
                {
                    items,
                    total = result.TotalCount,
                    page = result.Page,
                    pageSize = result.PageSize,
                    totalPages = result.TotalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin vehicles list");
                return StatusCode(500, new
                {
                    error = "Failed to retrieve vehicles. Please try again later.",
                    items = Array.Empty<object>(),
                    total = 0,
                    page,
                    pageSize,
                    totalPages = 0
                });
            }
        }

        /// <summary>
        /// Get vehicle statistics for admin dashboard
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetVehicleStats(CancellationToken ct = default)
        {
            try
            {
                var stats = await _vehicleServiceClient.GetVehicleStatsAsync(ct);

                return Ok(new
                {
                    total = stats.Total,
                    active = stats.Active,
                    pending = stats.Pending,
                    rejected = stats.Rejected,
                    featured = stats.Featured,
                    withReports = stats.WithReports
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicle stats");
                return StatusCode(500, new
                {
                    error = "Failed to retrieve vehicle statistics.",
                    total = 0,
                    active = 0,
                    pending = 0,
                    rejected = 0,
                    featured = 0,
                    withReports = 0
                });
            }
        }

        /// <summary>
        /// Get a single vehicle by ID
        /// </summary>
        [HttpGet("{vehicleId:guid}")]
        public async Task<IActionResult> GetVehicleById(Guid vehicleId, CancellationToken ct = default)
        {
            try
            {
                var vehicle = await _vehicleServiceClient.GetVehicleByIdAsync(vehicleId, ct);

                if (vehicle == null)
                    return NotFound(new { Error = $"Vehicle {vehicleId} not found" });

                return Ok(new
                {
                    id = vehicle.Id.ToString(),
                    title = vehicle.Title,
                    make = vehicle.Make,
                    model = vehicle.Model,
                    year = vehicle.Year,
                    price = vehicle.Price,
                    currency = vehicle.Currency,
                    image = vehicle.Images?.FirstOrDefault(i => i.IsPrimary)?.Url
                            ?? vehicle.Images?.FirstOrDefault()?.Url
                            ?? "",
                    status = vehicle.StatusName ?? "active",
                    sellerId = vehicle.SellerId?.ToString() ?? "",
                    sellerName = vehicle.SellerName ?? "Unknown",
                    sellerType = vehicle.SellerType ?? "individual",
                    views = vehicle.ViewCount,
                    leads = vehicle.LeadCount,
                    featured = vehicle.IsFeatured,
                    reportsCount = 0,
                    createdAt = vehicle.CreatedAt.ToString("o"),
                    publishedAt = vehicle.PublishedAt?.ToString("o"),
                    rejectionReason = vehicle.RejectionReason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { Error = "Failed to get vehicle" });
            }
        }

        /// <summary>
        /// Approve a vehicle for publication
        /// </summary>
        [HttpPost("{vehicleId}/approve")]
        public async Task<IActionResult> ApproveVehicle(
            Guid vehicleId,
            [FromBody] ApproveVehicleRequest? request)
        {
            try
            {
                // Approve the vehicle in VehiclesSaleService (PendingReview → Active)
                var approved = await _vehicleServiceClient.ApproveVehicleAsync(
                    vehicleId,
                    request?.ApprovedBy ?? "admin",
                    request?.Reason);

                var command = new ApproveVehicleCommand(
                    vehicleId,
                    request?.ApprovedBy ?? "admin",
                    request?.Reason ?? "Approved",
                    request?.OwnerEmail ?? "",
                    request?.VehicleTitle ?? ""
                );

                var result = await _mediator.Send(command);

                return Ok(new { Success = result && approved, Message = "Vehicle approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { Error = "Failed to approve vehicle" });
            }
        }

        /// <summary>
        /// Reject a vehicle submission
        /// </summary>
        [HttpPost("{vehicleId}/reject")]
        public async Task<IActionResult> RejectVehicle(
            Guid vehicleId,
            [FromBody] RejectVehicleRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { Error = "A rejection reason is required" });

            try
            {
                // Actually unpublish/reject the vehicle in VehiclesSaleService
                var unpublished = await _vehicleServiceClient.UnpublishVehicleAsync(vehicleId);
                if (!unpublished)
                {
                    _logger.LogWarning("Failed to unpublish vehicle {VehicleId} in VehiclesSaleService", vehicleId);
                }

                var command = new RejectVehicleCommand(
                    vehicleId,
                    request.RejectedBy ?? "admin",
                    request.Reason,
                    request.OwnerEmail ?? "",
                    request.VehicleTitle ?? ""
                );

                var result = await _mediator.Send(command);

                return Ok(new { Success = result && unpublished, Message = "Vehicle rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { Error = "Failed to reject vehicle" });
            }
        }

        /// <summary>
        /// Toggle featured status for a vehicle
        /// </summary>
        [HttpPatch("{vehicleId}/featured")]
        public async Task<IActionResult> ToggleFeatured(
            Guid vehicleId,
            [FromBody] ToggleFeaturedRequest request,
            CancellationToken ct = default)
        {
            try
            {
                var result = await _vehicleServiceClient.FeatureVehicleAsync(vehicleId, request.Featured, ct);

                if (!result)
                    return StatusCode(500, new { Error = "Failed to update featured status" });

                return Ok(new { Success = true, Message = $"Vehicle featured status set to {request.Featured}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling featured for vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { Error = "Failed to toggle featured status" });
            }
        }

        /// <summary>
        /// Delete a vehicle (soft delete)
        /// </summary>
        [HttpDelete("{vehicleId:guid}")]
        public async Task<IActionResult> DeleteVehicle(Guid vehicleId, CancellationToken ct = default)
        {
            try
            {
                var result = await _vehicleServiceClient.DeleteVehicleAsync(vehicleId, ct);

                if (!result)
                    return StatusCode(500, new { Error = "Failed to delete vehicle" });

                return Ok(new { Success = true, Message = "Vehicle deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vehicle {VehicleId}", vehicleId);
                return StatusCode(500, new { Error = "Failed to delete vehicle" });
            }
        }
    }

    public record ApproveVehicleRequest(
        string? ApprovedBy,
        string? Reason,
        string? OwnerEmail,
        string? VehicleTitle
    );

    public record RejectVehicleRequest(
        string? RejectedBy,
        string? Reason,
        string? OwnerEmail,
        string? VehicleTitle
    );

    public record ToggleFeaturedRequest(
        bool Featured
    );
}
