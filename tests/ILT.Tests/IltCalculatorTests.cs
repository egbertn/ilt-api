using FluentAssertions;
using ILT.Application.Configuration;
using ILT.Application.Services;
using ILT.Domain.Models;
using Xunit;

namespace ILT.Tests;

public class IltCalculatorTests
{
    private static IltCalculator BuildSut(IltConfiguration config)
    {
        var monitor = TestHelpers.Monitor(config);
        var classifier = new CategoryClassifier(monitor);
        return new IltCalculator(classifier, monitor);
    }

    private static Transaction Tx(string date, decimal amount, string category, string counter = "NL00OTHER") =>
        new()
        {
            DateTime = DateTimeOffset.Parse(date),
            AccountNumber = "NL67BANK0123456789",
            CounterAccountNumber = counter,
            Amount = amount,
            Category = category
        };

    [Fact]
    public void Income_and_expenses_are_categorised_per_month()
    {
        var sut = BuildSut(TestHelpers.DefaultConfig());

        var transactions = new[]
        {
            Tx("2025-01-25", 3600m, "Salaris"),
            Tx("2025-01-01", -1200m, "Huur"),
            Tx("2025-02-25", 3600m, "Salaris"),
            Tx("2025-02-01", -1200m, "Huur"),
            Tx("2025-02-15", -200m, "Boodschappen"),
        };

        var result = sut.Calculate(transactions, new[] { "NL67BANK0123456789" });

        result.Months.Should().HaveCount(2);

        var jan = result.Months.Single(m => m.Period == "2025-01");
        jan.Income.Should().Be(3600m);
        jan.Expenses.Should().Be(1200m);
        jan.Disposable.Should().Be(2400m);

        var feb = result.Months.Single(m => m.Period == "2025-02");
        feb.Income.Should().Be(3600m);
        feb.Expenses.Should().Be(1400m);
        feb.Disposable.Should().Be(2200m);

        result.AverageMonthlyIncome.Should().Be(3600m);
        result.AverageMonthlyExpenses.Should().Be(1300m);
        result.AverageMonthlyDisposable.Should().Be(2300m);
    }

    [Fact]
    public void Ignored_categories_are_excluded_but_counted()
    {
        var sut = BuildSut(TestHelpers.DefaultConfig());

        var transactions = new[]
        {
            Tx("2025-01-25", 3600m, "Salaris"),
            Tx("2025-01-08", -400m, "Sparen"),
        };

        var result = sut.Calculate(transactions, ["NL67BANK0123456789"]);

        result.Months.Single().Expenses.Should().Be(0m);
        result.Months.Single().Income.Should().Be(3600m);
        result.IgnoredTransactionCount.Should().Be(1);
        result.UnknownCategories.Should().BeEmpty();
    }

    [Fact]
    public void Unknown_categories_are_reported()
    {
        var sut = BuildSut(TestHelpers.DefaultConfig());

        var transactions = new[]
        {
            Tx("2025-01-25", 3600m, "Salaris"),
            Tx("2025-01-30", -520m, "Elektronica"),
            Tx("2025-02-05", -50m, "Streaming"),
        };

        var result = sut.Calculate(transactions, ["NL67BANK0123456789"]);

        result.UnknownCategories.Should().BeEquivalentTo(["Elektronica", "Streaming"]);
        result.IgnoredTransactionCount.Should().Be(2);
    }

    [Fact]
    public void Sign_of_amount_does_not_change_classification()
    {
        // Some banks send expenses as positive numbers. Classification is
        // category-driven, not sign-driven; income/expense should still total
        // to absolute values.
        var sut = BuildSut(TestHelpers.DefaultConfig());

        var transactions = new[]
        {
            Tx("2025-01-25", 3600m, "Salaris"),
            Tx("2025-01-01", 1200m, "Huur"), // sent as positive
        };

        var result = sut.Calculate(transactions, ["NL67BANK0123456789"]);

        var month = result.Months.Single();
        month.Income.Should().Be(3600m);
        month.Expenses.Should().Be(1200m);
    }

    [Fact]
    public void MinAbsoluteAmount_filter_is_applied()
    {
        var config = TestHelpers.DefaultConfig();
        config.MinAbsoluteAmount = 100m;
        var sut = BuildSut(config);

        var transactions = new[]
        {
            Tx("2025-01-25", 3600m, "Salaris"),
            Tx("2025-01-15", -50m, "Boodschappen"), // below threshold
            Tx("2025-01-20", -150m, "Boodschappen"),
        };

        var result = sut.Calculate(transactions, ["NL67BANK0123456789"]);

        result.Months.Single().Expenses.Should().Be(150m);
        result.IgnoredTransactionCount.Should().Be(1);
    }

    [Fact]
    public void Excluded_counter_accounts_are_skipped()
    {
        var config = TestHelpers.DefaultConfig();
        config.ExcludedCounterAccounts.Add("NL77SAVE0000000012");
        var sut = BuildSut(config);

        var transactions = new[]
        {
            Tx("2025-01-25", 3600m, "Salaris"),
            Tx("2025-01-08", -400m, "Boodschappen", counter: "NL77SAVE0000000012"),
        };

        var result = sut.Calculate(transactions, ["NL67BANK0123456789"]);

        result.Months.Single().Expenses.Should().Be(0m);
        result.IgnoredTransactionCount.Should().Be(1);
    }

    [Fact]
    public void SpreadEvenly_distributes_amount_across_period_months()
    {
        var config = TestHelpers.DefaultConfig();
        config.MultiMonthHandling = MultiMonthHandling.SpreadEvenly;
        var sut = BuildSut(config);

        var halfYearInsurance = new Transaction
        {
            DateTime = DateTimeOffset.Parse("2025-01-15"),
            AccountNumber = "NL67BANK0123456789",
            CounterAccountNumber = "NL00OTHER",
            Amount = -600m, // 100/month
            Category = "Huur",
            PeriodStart = DateTimeOffset.Parse("2025-01-01"),
            PeriodEnd = DateTimeOffset.Parse("2025-06-01")
        };

        var result = sut.Calculate([halfYearInsurance], ["NL67BANK0123456789"]);

        result.Months.Should().HaveCount(6);
        result.Months.Should().AllSatisfy(m => m.Expenses.Should().Be(100m));
    }

    [Fact]
    public void Empty_transactions_yields_zero_averages()
    {
        var sut = BuildSut(TestHelpers.DefaultConfig());

        var result = sut.Calculate([], ["NL67BANK0123456789"]);

        result.Months.Should().BeEmpty();
        result.AverageMonthlyIncome.Should().Be(0m);
        result.AverageMonthlyExpenses.Should().Be(0m);
        result.AverageMonthlyDisposable.Should().Be(0m);
    }
}
