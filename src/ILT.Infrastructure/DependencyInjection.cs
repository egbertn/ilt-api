using ILT.Application.Abstractions;
using ILT.Application.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ILT.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIltInfrastructure(this IServiceCollection services)
    {
        _ = services.AddHttpClient(HttpTransactionSource.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<TransactionsApiOptions>>().CurrentValue;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/";
                client.BaseAddress = new Uri(baseUrl);
            }
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddSingleton<ITransactionSource, HttpTransactionSource>();
        return services;
    }
}
