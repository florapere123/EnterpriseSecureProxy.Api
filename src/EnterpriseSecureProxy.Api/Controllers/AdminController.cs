using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseSecureProxy.Api.Controllers;

/// <summary>Read and update proxy operations and destinations, with optimistic concurrency on writes.</summary>
[ApiController]
[Route("api/admin")]
[Produces("application/json")]
public sealed class AdminController : ControllerBase
{
    private readonly IProxyOperationAdminService _adminService;
    private readonly IDestinationAdminService _destinationAdminService;

    public AdminController(
        IProxyOperationAdminService adminService,
        IDestinationAdminService destinationAdminService)
    {
        _adminService = adminService;
        _destinationAdminService = destinationAdminService;
    }

    /// <summary>Get one proxy operation by id, including its RowVersion for updates.</summary>
    [HttpGet("operations/{id:guid}")]
    [ApiConventionMethod(typeof(ProxyApiConventions), nameof(ProxyApiConventions.AdminGet))]
    public async Task<ActionResult<ProxyOperationAdminViewDto>> GetOperation(Guid id, CancellationToken cancellationToken)
    {
        var operation = await _adminService.GetOperationAsync(id, cancellationToken);
        return operation is null ? NotFound() : Ok(operation);
    }

    /// <summary>Update a proxy operation; send back the RowVersion from the last GET.</summary>
    [HttpPut("operations/{id:guid}")]
    [ApiConventionMethod(typeof(ProxyApiConventions), nameof(ProxyApiConventions.AdminUpdate))]
    public async Task<IActionResult> UpdateOperation(
        Guid id,
        [FromBody] UpdateProxyOperationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateOperationAsync(id, request, cancellationToken);

        if (!result.Success)
        {
            if (result.ConcurrencyConflict)
            {
                return Conflict(result);
            }

            return result.Message == "Operation not found." ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>Get one destination by id, including its RowVersion for updates.</summary>
    [HttpGet("destinations/{id:guid}")]
    [ApiConventionMethod(typeof(ProxyApiConventions), nameof(ProxyApiConventions.AdminGet))]
    public async Task<ActionResult<DestinationAdminViewDto>> GetDestination(Guid id, CancellationToken cancellationToken)
    {
        var destination = await _destinationAdminService.GetDestinationAsync(id, cancellationToken);
        return destination is null ? NotFound() : Ok(destination);
    }

    /// <summary>Update a destination; send back the RowVersion from the last GET.</summary>
    [HttpPut("destinations/{id:guid}")]
    [ApiConventionMethod(typeof(ProxyApiConventions), nameof(ProxyApiConventions.AdminUpdate))]
    public async Task<IActionResult> UpdateDestination(
        Guid id,
        [FromBody] UpdateDestinationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _destinationAdminService.UpdateDestinationAsync(id, request, cancellationToken);
        if (!result.Success)
        {
            if (result.ConcurrencyConflict)
            {
                return Conflict(result);
            }

            return result.Message == "Destination not found." ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }
}
