using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ILT.Application.Abstractions;
using ILT.Application.Configuration;
using ILT.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ILT.Infrastructure;

public sealed class HttpTransactionSource(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<TransactionsApiOptions> options,
    ILogger<HttpTransactionSource> logger) : ITransactionSource
{
    public const string HttpClientName = "TransactionsApi";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter() }
    };


    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync(
        string accountNumber,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            throw new ArgumentException("Account number is required.", nameof(accountNumber));
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        var path = options.CurrentValue.PathTemplate.Replace("{accountNumber}", accountNumber);

        logger.LogInformation("Fetching transactions for {Account} via {Path}", accountNumber, path);

        using var response = await client.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dtos = await response.Content
            .ReadFromJsonAsync<List<TransactionDto>>(JsonOptions, cancellationToken)
            ?? [];

        return dtos
            .Select(d => d.ToDomain(accountNumber))
            .ToList();
    }

    private sealed class TransactionDto
    {
        public DateTimeOffset Date { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? OwnAccount { get; set; }
        public string? CounterpartyAccount { get; set; }

        // Forward-compat: any future field lands here so deserialization
        // doesn't fail when the contract is extended.
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }

        public Transaction ToDomain(string fallbackAccount)
        {
            var extras = Extra is null
                ? new Dictionary<string, object?>()
                : Extra.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value.ToString());

            // Optional period fields anticipated for multi-month transactions.
            DateTimeOffset? periodStart = TryReadDate(extras, "period_start");
            DateTimeOffset? periodEnd = TryReadDate(extras, "period_end");

            if (Description is not null)
            {
                extras["description"] = Description;
            }
            if (Currency is not null)
            {
                extras["currency"] = Currency;
            }

            return new Transaction
            {
                DateTime = Date,
                AccountNumber = OwnAccount ?? fallbackAccount,
                CounterAccountNumber = CounterpartyAccount ?? string.Empty,
                Amount = Amount,
                Category = Category ?? string.Empty,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                AdditionalProperties = extras
            };
        }

        private static DateTimeOffset? TryReadDate(Dictionary<string, object?> extras, string key)
        {
            if (extras.TryGetValue(key, out var raw) &&
                raw is string s &&
                DateTimeOffset.TryParse(s, out var parsed))
            {
                return parsed;
            }
            return null;
        }
    }
}
