using ILT.Domain.Models;

namespace ILT.Tests;

internal static class SamplePayload
{
    // Verbatim from a curl against the live endpoint for NL67BANK0123456789.
    public static readonly Transaction[] Transactions =
    {
        Make("2024-10-25", 3600.00m, "Salaris", "NL12EMPL0000000001"),
        Make("2024-11-01", -1200.00m, "Huur", "NL34LAND0000000002"),
        Make("2024-11-25", 3600.00m, "Salaris", "NL12EMPL0000000001"),
        Make("2024-12-01", -1200.00m, "Huur", "NL34LAND0000000002"),
        Make("2024-12-05", -45.60m, "Streaming", "NL56STRM0000000003"),
        Make("2024-12-20", -78.40m, "Boodschappen", "NL78SHOP0000000004"),
        Make("2025-01-01", -1200.00m, "Huur", "NL34LAND0000000002"),
        Make("2025-01-25", 3600.00m, "Salaris", "NL12EMPL0000000001"),
        Make("2025-01-30", -520.00m, "Elektronica", "NL90TECH0000000005"),
        Make("2025-02-01", -1200.00m, "Huur", "NL34LAND0000000002"),
        Make("2025-02-10", -130.25m, "Nutsen", "NL11UTIL0000000006"),
        Make("2025-02-25", 3600.00m, "Salaris", "NL12EMPL0000000001"),
        Make("2025-03-01", -1200.00m, "Huur", "NL34LAND0000000002"),
        Make("2025-03-12", 800.00m, "Belastingteruggave", "NL22TAXR0000000007"),
        Make("2025-03-25", 3600.00m, "Salaris", "NL12EMPL0000000001"),
        Make("2025-04-01", -1200.00m, "Huur", "NL34LAND0000000002"),
        Make("2025-04-08", -49.90m, "Abonnement", "NL33MOBL0000000008"),
        Make("2025-04-25", 3600.00m, "Salaris", "NL12EMPL0000000001"),
        Make("2025-05-01", -1200.00m, "Huur", "NL34LAND0000000002"),
        Make("2025-05-15", -260.75m, "Boodschappen", "NL78SHOP0000000004"),
        Make("2025-05-25", 3600.00m, "Salaris", "NL12EMPL0000000001"),
        Make("2025-06-01", -1200.00m, "Huur", "NL34LAND0000000002"),
        Make("2025-06-20", -65.00m, "Brandstof", "NL44FUEL0000000009"),
        Make("2025-06-25", 3600.00m, "Salaris", "NL12EMPL0000000001"),
        Make("2025-07-01", -1200.00m, "Huur", "NL34LAND0000000002"),
        Make("2025-07-10", 150.00m, "Dividend", "NL55INVX0000000010"),
        Make("2025-07-25", 3600.00m, "Salaris", "NL12EMPL0000000001"),
        Make("2025-08-01", -1200.00m, "Huur", "NL34LAND0000000002"),
        Make("2025-08-25", 3600.00m, "Salaris", "NL12EMPL0000000001"),
        Make("2025-09-01", -1200.00m, "Huur", "NL34LAND0000000002"),
        Make("2025-09-11", -95.00m, "Verzekering", "NL66INSR0000000011"),
        Make("2025-09-25", 3600.00m, "Salaris", "NL12EMPL0000000001"),
        Make("2025-10-01", -1200.00m, "Huur", "NL34LAND0000000002"),
        Make("2025-10-08", -400.00m, "Sparen", "NL77SAVE0000000012"),
        Make("2025-10-10", 3600.00m, "Salaris", "NL12EMPL0000000001"),
    };

    private static Transaction Make(string date, decimal amount, string category, string counter) =>
        new()
        {
            DateTime = DateTimeOffset.Parse(date),
            AccountNumber = "NL67BANK0123456789",
            CounterAccountNumber = counter,
            Amount = amount,
            Category = category
        };
}
