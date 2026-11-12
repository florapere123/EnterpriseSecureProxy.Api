using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnterpriseSecureProxy.Application.Auditing;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.Exceptions;
using EnterpriseSecureProxy.Domain.Interfaces;
using EnterpriseSecureProxy.Application.UseCases;

namespace EnterpriseSecureProxy.Application.Steps;

/// <summary>Decrypts the request body when an operation requires it. Called directly by the handler, not a pipeline.</summary>
public sealed class EncryptionStep
{
    private readonly IEncryptionService _encryption;
    private readonly ProxyAuditRecorder _audit;

    public EncryptionStep(IEncryptionService encryption, ProxyAuditRecorder audit)
    {
        _encryption = encryption;
        _audit = audit;
    }

    public async Task<ProxyExecuteRequest> DecryptAsync(
        ProxyExecuteRequest request,
        ProxyOperation operation,
        ProxyInvocationContext context,
        long elapsedMs,
        CancellationToken cancellationToken = default)
    {
        var operationKey = operation.OperationKey;

        if (!operation.RequiresEncryption)
        {
            return request;
        }

        var envelope = request.ToEncryptedPayload();
        if (envelope is null)
        {
            await AuditCryptoFailureAsync(context, operationKey, elapsedMs, cancellationToken);
            throw new ProxyDecryptionException(operationKey);
        }

        try
        {
            var associatedData = Encoding.UTF8.GetBytes(operationKey);
            var plaintext = _encryption.Decrypt(envelope, associatedData);
            using var doc = JsonDocument.Parse(plaintext);

            return new ProxyExecuteRequest
            {
                ClientId = request.ClientId,
                Parameters = request.Parameters,
                Body = doc.RootElement.Clone()
            };
        }
        catch (CryptographicException)
        {
            await AuditCryptoFailureAsync(context, operationKey, elapsedMs, cancellationToken);
            throw new ProxyDecryptionException(operationKey);
        }
        catch (JsonException)
        {
            await AuditCryptoFailureAsync(context, operationKey, elapsedMs, cancellationToken);
            throw new ProxyDecryptionException(operationKey);
        }
    }

    private Task AuditCryptoFailureAsync(
        ProxyInvocationContext context,
        string operationKey,
        long elapsedMs,
        CancellationToken cancellationToken) =>
        _audit.RecordAsync(
            context,
            operationKey,
            AuditOutcome.RejectionCryptoFailure,
            elapsedMs,
            detail: "Invalid or corrupted encrypted payload.",
            cancellationToken: cancellationToken);
}
