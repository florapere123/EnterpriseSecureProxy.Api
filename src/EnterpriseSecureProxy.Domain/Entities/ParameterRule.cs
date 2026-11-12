using EnterpriseSecureProxy.Domain.Enums;

namespace EnterpriseSecureProxy.Domain.Entities;

/// <summary>
/// Declarative validation rule for a single inbound parameter belonging to a <see cref="ProxyOperation"/>.
/// </summary>
public sealed class ParameterRule
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public ParameterLocation Location { get; init; } = ParameterLocation.Body;

    public ParameterDataType DataType { get; init; } = ParameterDataType.String;

    public bool IsRequired { get; init; } = true;

    public IReadOnlyList<string>? AllowedValues { get; init; }

    public string? Pattern { get; init; }

    public double? MinValue { get; init; }

    public double? MaxValue { get; init; }

    public string? Description { get; init; }

    public static ParameterRule Create(
        string name,
        ParameterLocation location,
        ParameterDataType dataType,
        bool isRequired = true,
        IReadOnlyList<string>? allowedValues = null,
        string? pattern = null,
        double? minValue = null,
        double? maxValue = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ParameterRule
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Location = location,
            DataType = dataType,
            IsRequired = isRequired,
            AllowedValues = allowedValues,
            Pattern = pattern,
            MinValue = minValue,
            MaxValue = maxValue,
            Description = description
        };
    }

    public static ParameterRule Rehydrate(
        Guid id,
        string name,
        ParameterLocation location,
        ParameterDataType dataType,
        bool isRequired,
        IReadOnlyList<string>? allowedValues,
        string? pattern,
        double? minValue,
        double? maxValue,
        string? description = null) =>
        new()
        {
            Id = id,
            Name = name,
            Location = location,
            DataType = dataType,
            IsRequired = isRequired,
            AllowedValues = allowedValues,
            Pattern = pattern,
            MinValue = minValue,
            MaxValue = maxValue,
            Description = description
        };
}
