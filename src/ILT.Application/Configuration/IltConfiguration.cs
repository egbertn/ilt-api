namespace ILT.Application.Configuration;

public sealed class IltConfiguration
{
    public const string SectionName = "Ilt";

    public HashSet<string> IncomeCategories { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> ExpenseCategories { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> IgnoredCategories { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public MultiMonthHandling MultiMonthHandling { get; set; }
        = MultiMonthHandling.AssignToTransactionDate;

    // Skip transactions whose absolute amount is below this threshold.
    public decimal MinAbsoluteAmount { get; set; } = 0m;

    // Skip transfers to/from these account numbers (e.g. own savings).
    public HashSet<string> ExcludedCounterAccounts { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
