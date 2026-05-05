using ILT.Application.Abstractions;
using ILT.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ILT.Application.Services;

public sealed class IltService(
    ITransactionSource source,
    IIltCalculator calculator,
    ILogger<IltService> logger) : IIltService
{
    public async Task<IltResult> CalculateAsync(
        IReadOnlyList<string> accountNumbers,
        CancellationToken cancellationToken)
    {
        if (accountNumbers.Count == 0)
        {
            throw new ArgumentException("At least one account number is required.", nameof(accountNumbers));
        }

        var fetchTasks = accountNumbers
            .Select(account => source.GetTransactionsAsync(account, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(fetchTasks);

        var transactions = results.SelectMany(r => r).ToArray();

        logger.LogInformation(
            "Fetched {Count} transactions across {AccountCount} account(s).",
            transactions.Length,
            accountNumbers.Count);

        return calculator.Calculate(transactions, accountNumbers);
    }
}
