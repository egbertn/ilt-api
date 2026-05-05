using ILT.Application.Configuration;
using Microsoft.Extensions.Options;

namespace ILT.Tests;

internal static class TestHelpers
{
    public static IOptionsMonitor<T> Monitor<T>(T value) where T : class
        => new StaticMonitor<T>(value);

    private sealed class StaticMonitor<T>(T value) : IOptionsMonitor<T> where T : class
    {
        public T CurrentValue { get; } = value;
        public T Get(string? name) => CurrentValue;
        public IDisposable OnChange(Action<T, string?> listener) => new NoopDisposable();

        private sealed class NoopDisposable : IDisposable { public void Dispose() { } }
    }

    public static IltConfiguration DefaultConfig() => new()
    {
        IncomeCategories = new(StringComparer.OrdinalIgnoreCase) { "Salaris", "Dividend" },
        ExpenseCategories = new(StringComparer.OrdinalIgnoreCase) { "Huur", "Boodschappen" },
        IgnoredCategories = new(StringComparer.OrdinalIgnoreCase) { "Sparen" },
        MultiMonthHandling = MultiMonthHandling.AssignToTransactionDate,
        MinAbsoluteAmount = 0m,
        ExcludedCounterAccounts = new(StringComparer.OrdinalIgnoreCase)
    };
}
