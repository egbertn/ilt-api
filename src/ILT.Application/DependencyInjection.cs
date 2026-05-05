using ILT.Application.Abstractions;
using ILT.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ILT.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIltApplication(this IServiceCollection services)
    {
        services.AddSingleton<ICategoryClassifier, CategoryClassifier>();
        services.AddSingleton<IIltCalculator, IltCalculator>();
        services.AddSingleton<IIltService, IltService>();
        return services;
    }
}
