using System.Net;
using System.Text;
using FluentAssertions;
using ILT.Application.Configuration;
using ILT.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ILT.Tests;

public class HttpTransactionSourceTests
{
    private const string SampleJson = """
    [
      {"date":"2024-10-25T00:00:00","amount":3600.00,"currency":"EUR","category":"Salaris","description":"Maandelijkse salarisstorting","own_account":"NL67BANK0123456789","counterparty_account":"NL12EMPL0000000001"},
      {"date":"2024-11-01T00:00:00","amount":-1200.00,"currency":"EUR","category":"Huur","description":"Huur maand November","own_account":"NL67BANK0123456789","counterparty_account":"NL34LAND0000000002"}
    ]
    """;

    [Fact]
    public async Task Parses_snake_case_payload_per_OpenAPI_spec()
    {
        var source = BuildSut(SampleJson);

        var transactions = await source.GetTransactionsAsync(
            "NL67BANK0123456789",
            CancellationToken.None);

        transactions.Should().HaveCount(2);

        var salary = transactions[0];
        salary.Amount.Should().Be(3600m);
        salary.Category.Should().Be("Salaris");
        salary.AccountNumber.Should().Be("NL67BANK0123456789");
        salary.CounterAccountNumber.Should().Be("NL12EMPL0000000001");
        salary.AdditionalProperties.Should().ContainKey("description");
        salary.AdditionalProperties.Should().ContainKey("currency");
    }

    [Fact]
    public async Task Tolerates_unknown_future_fields()
    {
        const string future = """
        [
          {
            "date":"2025-01-25T00:00:00",
            "amount":100.00,
            "currency":"EUR",
            "category":"Salaris",
            "description":"x",
            "own_account":"NL67BANK0123456789",
            "counterparty_account":"NL00",
            "tags":["bonus","one-off"],
            "merchant_id":"M-9001"
          }
        ]
        """;

        var source = BuildSut(future);
        var transactions = await source.GetTransactionsAsync(
            "NL67BANK0123456789",
            CancellationToken.None);

        transactions.Should().HaveCount(1);
        transactions[0].AdditionalProperties.Should().ContainKey("tags");
        transactions[0].AdditionalProperties.Should().ContainKey("merchant_id");
    }

    private static HttpTransactionSource BuildSut(string responseJson)
    {
        var handler = new StubHandler(responseJson);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        var factory = new StubHttpClientFactory(client);
        var options = TestHelpers.Monitor(new TransactionsApiOptions
        {
            BaseUrl = "http://localhost/",
            PathTemplate = "{accountNumber}.json"
        });

        return new HttpTransactionSource(factory, options, NullLogger<HttpTransactionSource>.Instance);
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        private readonly HttpClient _client = client;

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StubHandler(string payload) : HttpMessageHandler
    {
        private readonly string _payload = payload;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_payload, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
