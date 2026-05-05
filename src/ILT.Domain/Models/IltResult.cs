namespace ILT.Domain.Models;

public sealed record IltResult(
    IReadOnlyList<string> AccountNumbers,
    IReadOnlyList<MonthlyIlt> Months,
    decimal AverageMonthlyIncome,
    decimal AverageMonthlyExpenses,
    decimal AverageMonthlyDisposable,
    int TransactionCount,
    int IgnoredTransactionCount,
    IReadOnlyList<string> UnknownCategories);
