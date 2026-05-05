using ILT.Application.Abstractions;
using ILT.Application.Configuration;
using ILT.Domain.Models;
using Microsoft.Extensions.Options;

namespace ILT.Application.Services;

public sealed class IltCalculator(
    ICategoryClassifier classifier,
    IOptionsMonitor<IltConfiguration> _options) : IIltCalculator
{
    public IltResult Calculate(
        IEnumerable<Transaction> transactions,
        IReadOnlyList<string> accountNumbers)
    {
        var config = _options.CurrentValue;
        var monthly = new Dictionary<(int Year, int Month), (decimal Income, decimal Expenses)>();
        var unknownCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var total = 0;
        var ignored = 0;

        foreach (var tx in transactions)
        {
            total++;

            if (Math.Abs(tx.Amount) < config.MinAbsoluteAmount)
            {
                ignored++;
                continue;
            }

            if (config.ExcludedCounterAccounts.Contains(tx.CounterAccountNumber))
            {
                ignored++;
                continue;
            }

            var classification = classifier.Classify(tx.Category);
            if (classification == CategoryType.Ignored)
            {
                ignored++;
                if (!config.IgnoredCategories.Contains(tx.Category) &&
                    !string.IsNullOrWhiteSpace(tx.Category))
                {
                    unknownCategories.Add(tx.Category);
                }
                continue;
            }

            foreach (var (year, month, share) in DistributeAcrossMonths(tx, config.MultiMonthHandling))
            {
                if (!monthly.TryGetValue((year, month), out var current))
                {
                    current = (0m, 0m);
                }

                if (classification == CategoryType.Income)
                {
                    current.Income += Math.Abs(share);
                }
                else
                {
                    current.Expenses += Math.Abs(share);
                }

                monthly[(year, month)] = current;
            }
        }

        var months = monthly
            .OrderBy(kvp => kvp.Key.Year)
            .ThenBy(kvp => kvp.Key.Month)
            .Select(kvp => new MonthlyIlt(
                kvp.Key.Year,
                kvp.Key.Month,
                Round(kvp.Value.Income),
                Round(kvp.Value.Expenses)))
            .ToArray();

        var avgIncome = months.Length == 0 ? 0m : Round(months.Average(m => m.Income));
        var avgExpenses = months.Length == 0 ? 0m : Round(months.Average(m => m.Expenses));
        var avgDisposable = Round(avgIncome - avgExpenses);

        return new IltResult(
            accountNumbers,
            months,
            avgIncome,
            avgExpenses,
            avgDisposable,
            total,
            ignored,
            [.. unknownCategories.OrderBy(c => c)]);
    }

    private static IEnumerable<(int Year, int Month, decimal Share)> DistributeAcrossMonths(
        Transaction tx,
        MultiMonthHandling handling)
    {
        if (handling == MultiMonthHandling.AssignToTransactionDate ||
            tx.PeriodStart is null || tx.PeriodEnd is null ||
            tx.PeriodEnd.Value < tx.PeriodStart.Value)
        {
            yield return (tx.DateTime.Year, tx.DateTime.Month, tx.Amount);
            yield break;
        }

        var start = new DateTime(tx.PeriodStart.Value.Year, tx.PeriodStart.Value.Month, 1);
        var end = new DateTime(tx.PeriodEnd.Value.Year, tx.PeriodEnd.Value.Month, 1);
        var monthCount = ((end.Year - start.Year) * 12) + (end.Month - start.Month) + 1;

        if (monthCount <= 1)
        {
            yield return (tx.DateTime.Year, tx.DateTime.Month, tx.Amount);
            yield break;
        }

        var share = tx.Amount / monthCount;
        for (var i = 0; i < monthCount; i++)
        {
            var bucket = start.AddMonths(i);
            yield return (bucket.Year, bucket.Month, share);
        }
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
