namespace ILT.Domain.Models;

public sealed record MonthlyIlt(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses)
{
    public decimal Disposable => Income - Expenses;

    public string Period => $"{Year:D4}-{Month:D2}";
}
