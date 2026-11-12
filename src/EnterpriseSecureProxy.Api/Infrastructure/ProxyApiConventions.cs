using EnterpriseSecureProxy.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace EnterpriseSecureProxy.Api.Infrastructure;

/// <summary>
/// OpenAPI response conventions for proxy and admin actions.
/// </summary>
internal static class ProxyApiConventions
{
    [ProducesResponseType(typeof(ProxyExecuteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProxyExecuteResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProxyExecuteResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProxyExecuteResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProxyExecuteResponse), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ProxyExecuteResponse), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProxyExecuteResponse), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProxyExecuteResponse), StatusCodes.Status500InternalServerError)]
    public static void Execute(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object request,
        CancellationToken cancellationToken)
    {
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static void AdminGet(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        Guid id,
        CancellationToken cancellationToken)
    {
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public static void AdminUpdate(
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        Guid id,
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
        [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
        object request,
        CancellationToken cancellationToken)
    {
    }
}
