namespace ILT.Application.Configuration;

public enum MultiMonthHandling
{
    // Attribute the full amount to the month of the transaction date
    AssignToTransactionDate = 0,

    // Spread the amount evenly across the months in [PeriodStart, PeriodEnd]
    SpreadEvenly = 1
}
