namespace EnterpriseSecureProxy.Domain.Enums;

/// <summary>Primitive type a parameter value must conform to after parsing.</summary>
public enum ParameterDataType
{
    String = 1,
    Integer = 2,
    Decimal = 3,
    Boolean = 4,
    DateTimeUtc = 5,
    Guid = 6,
    Json = 7
}
