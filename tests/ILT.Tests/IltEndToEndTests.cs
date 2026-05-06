using FluentAssertions;
using ILT.Application.Configuration;
using ILT.Application.Services;
using Xunit;

namespace ILT.Tests;

public class IltEndToEndTests
{
    [Fact]
    public void Real_sample_payload_yields_expected_monthly_aggregates()
    {
        // Mirrors src/ILT.Api/ilt-config.json so this test stays in sync with
        // the deployed configuration.
        var config = new IltConfiguration
        {
            IncomeCategories = new(StringComparer.OrdinalIgnoreCase)
                { "Salaris", "Belastingteruggave", "Dividend" },
            ExpenseCategories = new(StringComparer.OrdinalIgnoreCase)
                { "Huur", "Boodschappen", "Streaming", "Nutsen", "Abonnement",
                  "Brandstof", "Verzekering" },
            IgnoredCategories = new(StringComparer.OrdinalIgnoreCase) { "Sparen" }
        };

        var monitor = TestHelpers.Monitor(config);
        var classifier = new CategoryClassifier(monitor);
        var calculator = new IltCalculator(classifier, monitor);

        var transactions = SamplePayload.Transactions;

        var result = calculator.Calculate(transactions, ["NL67BANK0123456789"]);

        // Sanity: 35 transactions in the curl output.
        result.TransactionCount.Should().Be(35);

        // Elektronica is intentionally not in any list — should surface as unknown.
        result.UnknownCategories.Should().Contain("Elektronica");

        // 2025-01: salary 3600 + huur 1200 + Elektronica (unknown→ignored)
        var jan = result.Months.Single(m => m.Period == "2025-01");
        jan.Income.Should().Be(3600m);
        jan.Expenses.Should().Be(1200m);
        jan.Disposable.Should().Be(2400m);

        // 2025-03: salary 3600 + belastingteruggave 800 = 4400 income; huur 1200 expense
        var mar = result.Months.Single(m => m.Period == "2025-03");
        mar.Income.Should().Be(4400m);
        mar.Expenses.Should().Be(1200m);

        // 2025-10: Sparen 400 should be ignored
        var oct = result.Months.Single(m => m.Period == "2025-10");
        oct.Income.Should().Be(3600m);
        oct.Expenses.Should().Be(1200m);
    }
}
