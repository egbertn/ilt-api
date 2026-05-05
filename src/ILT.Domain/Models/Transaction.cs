namespace ILT.Domain.Models;

public sealed record Transaction
{
    public required DateTimeOffset DateTime { get; init; }
    public required string AccountNumber { get; init; }
    public required string CounterAccountNumber { get; init; }
    public required decimal Amount { get; init; }
    public required string Category { get; init; }

    // Optional period span — used when a transaction covers multiple months
    // (e.g. a half-year insurance premium). When null, the transaction is
    // attributed to its DateTime month.
    public DateTimeOffset? PeriodStart { get; init; }
    public DateTimeOffset? PeriodEnd { get; init; }

    // Forward compatibility: unrecognised JSON fields land here so we don't
    // break when the upstream contract is extended.
    public IReadOnlyDictionary<string, object?> AdditionalProperties { get; init; }
        = new Dictionary<string, object?>();
}
