using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseSecureProxy.Infrastructure.Services;

/// <summary>Shared in-memory RowVersion checks used by the mock admin services.</summary>
internal static class MockAdminRowVersion
{
    public static string? ValidateRowVersionOrGetError(string? rowVersionBase64)
    {
        var parsed = RowVersionParser.TryParse(rowVersionBase64);
        return parsed.Success ? null : parsed.ErrorMessage;
    }

    public static void AssertCurrentOrThrow(ConcurrentDictionary<Guid, byte[]> store, Guid id, byte[] supplied)
    {
        var current = store.GetOrAdd(id, _ => CreateVersionBytes());
        if (!current.AsSpan().SequenceEqual(supplied))
        {
            throw new DbUpdateConcurrencyException("Stale RowVersion.");
        }
    }

    public static void Bump(ConcurrentDictionary<Guid, byte[]> store, Guid id) =>
        store[id] = CreateVersionBytes();

    public static byte[] CreateVersionBytes()
    {
        var bytes = new byte[8];
        Random.Shared.NextBytes(bytes);
        return bytes;
    }
}
