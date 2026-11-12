using System.Diagnostics;
using EnterpriseSecureProxy.Application.Auditing;
using EnterpriseSecureProxy.Application.Steps;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Application.Validation;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.Exceptions;
using EnterpriseSecureProxy.Domain.Interfaces;

namespace EnterpriseSecureProxy.Application.UseCases;

/// <summary>Runs one proxy request end to end: validate, decrypt, check idempotency, forward, and audit.</summary>
public sealed class ExecuteProxyOperationHandler
{
    private readonly IConfigurationRepository _configuration;
    private readonly IRequestValidator _validator;
    private readonly IProxyForwarder _forwarder;
    private readonly ProxyAuditRecorder _audit;
    private readonly EncryptionStep _encryptionStep;
    private readonly IdempotencyStep _idempotencyStep;

    public ExecuteProxyOperationHandler(
        IConfigurationRepository configuration,
        IRequestValidator validator,
        IProxyForwarder forwarder,
        ProxyAuditRecorder audit,
        EncryptionStep encryptionStep,
        IdempotencyStep idempotencyStep)
    {
        _configuration = configuration;
        _validator = validator;
        _forwarder = forwarder;
        _audit = audit;
        _encryptionStep = encryptionStep;
        _idempotencyStep = idempotencyStep;
    }

    public async Task<ProxyExecuteResponse> HandleAsync(
        ProxyExecuteRequest request,
        ProxyInvocationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var stopwatch = Stopwatch.StartNew();
        var operationKey = request.ClientId?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(operationKey))
        {
            return await FailAsync(context, stopwatch, operationKey, AuditOutcome.ValidationFailed,
                "VALIDATION_FAILED", "A non-empty 'clientId' (operation key) is required.", null,
                new[] { new ValidationError("clientId", "The 'clientId' field is required.") }, cancellationToken);
        }

        var operation = await _configuration.GetOperationAsync(operationKey, cancellationToken);
        if (operation is null)
        {
            return await FailAsync(context, stopwatch, operationKey, AuditOutcome.OperationNotFound,
                "NOT_FOUND", $"Operation '{operationKey}' is not configured or is disabled.", null, null, cancellationToken);
        }

        // A request that passed the enabled check is allowed to complete even if an admin disables
        // the operation mid-flight; cache TTL + explicit eviction on update bounds staleness.

        try
        {
            EnforceRoleAccess(operation, operationKey, context);
        }
        catch (OperationForbiddenException)
        {
            stopwatch.Stop();
            await _audit.RecordAsync(context, operationKey, AuditOutcome.Forbidden, stopwatch.ElapsedMilliseconds,
                detail: "RBAC denied.", cancellationToken: cancellationToken);
            throw;
        }

        if (operation.MaxRequestSize > 0 &&
            context.RequestContentLengthBytes is { } contentLength &&
            contentLength > operation.MaxRequestSize)
        {
            return await FailAsync(context, stopwatch, operationKey, AuditOutcome.PayloadTooLarge,
                "PAYLOAD_TOO_LARGE",
                $"Request payload exceeds the configured limit of {operation.MaxRequestSize} bytes.",
                null, null, cancellationToken);
        }

        var effectiveRequest = await _encryptionStep.DecryptAsync(
            request, operation, context, stopwatch.ElapsedMilliseconds, cancellationToken);

        if (operation.Method == HttpVerb.Post && string.IsNullOrWhiteSpace(context.IdempotencyKey))
        {
            return await FailAsync(context, stopwatch, operationKey, AuditOutcome.ValidationFailed,
                "IDEMPOTENCY_KEY_REQUIRED",
                "The Idempotency-Key header is required for POST proxy operations.", null,
                new[] { new ValidationError("Idempotency-Key", "Header is required.") }, cancellationToken);
        }

        var idempotentReplay = _idempotencyStep.TryGetCachedResponse(
            operation, context, operationKey, effectiveRequest, stopwatch.ElapsedMilliseconds);
        if (idempotentReplay is not null)
        {
            stopwatch.Stop();
            return idempotentReplay;
        }

        var errors = _validator.Validate(operation, effectiveRequest);
        if (errors.Count > 0)
        {
            return await FailAsync(context, stopwatch, operationKey, AuditOutcome.ValidationFailed,
                "VALIDATION_FAILED", "One or more parameters failed validation.", null, errors, cancellationToken);
        }

        var destination = await _configuration.GetDestinationAsync(operation.DestinationName, cancellationToken);
        if (destination is null)
        {
            return await FailAsync(context, stopwatch, operationKey, AuditOutcome.DestinationUnavailable,
                "DESTINATION_UNAVAILABLE", $"Destination '{operation.DestinationName}' is unavailable.", null, null, cancellationToken);
        }

        var (targetUri, uriErrors) = ProxyTargetUriBuilder.TryBuild(destination, operation, effectiveRequest);
        if (uriErrors.Count > 0)
        {
            return await FailAsync(context, stopwatch, operationKey, AuditOutcome.ValidationFailed,
                "VALIDATION_FAILED", "The outbound path could not be built safely.", null, uriErrors, cancellationToken);
        }

        var forwardRequest = new ForwardRequest
        {
            CorrelationId = context.CorrelationId,
            Operation = operation,
            Destination = destination,
            TargetUri = targetUri!,
            Headers = ResolveHeaders(operation, context),
            Body = effectiveRequest.Body
        };

        var result = await _forwarder.ForwardAsync(forwardRequest, cancellationToken);
        stopwatch.Stop();

        _idempotencyStep.TryStore(operation, context, operationKey, effectiveRequest, result);

        if (!result.IsSuccess)
        {
            await _audit.RecordAsync(context, operationKey, AuditOutcome.DestinationError, stopwatch.ElapsedMilliseconds,
                result.StatusCode, result.FailureReason, cancellationToken);

            return ProxyExecuteResponse.Failure(context.CorrelationId, operationKey, "DESTINATION_ERROR",
                result.FailureReason ?? "The downstream service returned an error.",
                downstreamStatusCode: result.StatusCode);
        }

        await _audit.RecordAsync(context, operationKey, AuditOutcome.Succeeded, stopwatch.ElapsedMilliseconds,
            result.StatusCode, cancellationToken: cancellationToken);

        return ProxyExecuteResponse.Ok(context.CorrelationId, operationKey, result.StatusCode, result.Body);
    }

    private static void EnforceRoleAccess(ProxyOperation operation, string operationKey, ProxyInvocationContext context)
    {
        if (operation.AllowedRoles.Count == 0)
        {
            return;
        }

        var clientRole = context.ClientRole?.Trim();
        var authorized = !string.IsNullOrEmpty(clientRole) &&
                         operation.AllowedRoles.Contains(clientRole, StringComparer.OrdinalIgnoreCase);

        if (authorized)
        {
            return;
        }

        var reason = string.IsNullOrEmpty(clientRole)
            ? "Missing required X-Client-Role header."
            : $"Role '{clientRole}' is not authorized for this operation.";

        throw new OperationForbiddenException(operationKey, reason);
    }

    // OWASP Broken Access Control: strip denylisted headers again at send time so a bad config cannot forward Authorization or cookies.
    private static IReadOnlyDictionary<string, string> ResolveHeaders(ProxyOperation operation, ProxyInvocationContext context)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in operation.HeaderRules)
        {
            if (HeaderRule.IsNeverForwardable(rule.Name) && rule.Mode == HeaderRuleMode.Forward)
            {
                continue;
            }

            switch (rule.Mode)
            {
                case HeaderRuleMode.Inject when !string.IsNullOrEmpty(rule.InjectedValue):
                    headers[rule.Name] = rule.InjectedValue!;
                    break;
                case HeaderRuleMode.Forward when
                    !HeaderRule.IsNeverForwardable(rule.Name) &&
                    context.InboundHeaders.TryGetValue(rule.Name, out var value):
                    headers[rule.Name] = value;
                    break;
            }
        }

        return headers;
    }

    private async Task<ProxyExecuteResponse> FailAsync(
        ProxyInvocationContext context, Stopwatch stopwatch, string operationKey, AuditOutcome outcome,
        string status, string message, int? downstreamStatus, IReadOnlyList<ValidationError>? errors,
        CancellationToken cancellationToken)
    {
        stopwatch.Stop();
        await _audit.RecordAsync(context, operationKey, outcome, stopwatch.ElapsedMilliseconds, downstreamStatus,
            message, cancellationToken);
        return ProxyExecuteResponse.Failure(context.CorrelationId, operationKey, status, message, errors);
    }
}
