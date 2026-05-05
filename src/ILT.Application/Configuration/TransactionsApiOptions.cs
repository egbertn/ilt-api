namespace ILT.Application.Configuration;

public sealed class TransactionsApiOptions
{
    public const string SectionName = "TransactionsApi";

    public string BaseUrl { get; set; } = string.Empty;

    public string PathTemplate { get; set; } = "{accountNumber}.json?/q=/Transactions";

    public List<string> AccountNumbers { get; set; } = new();

    public int TimeoutSeconds { get; set; } = 30;
}
