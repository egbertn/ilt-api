using ILT.Application;
using ILT.Application.Configuration;
using ILT.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("ilt-config.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services
    .AddOptions<IltConfiguration>()
    .Bind(builder.Configuration.GetSection(IltConfiguration.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<TransactionsApiOptions>()
    .Bind(builder.Configuration.GetSection(TransactionsApiOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "TransactionsApi.BaseUrl is required.")
    .Validate(o => o.AccountNumbers.Count > 0, "TransactionsApi.AccountNumbers must contain at least one entry.")
    .ValidateOnStart();

builder.Services.AddIltApplication();
builder.Services.AddIltInfrastructure();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "ILT API", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "ILT API v1");
});

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory-based integration testing.
public partial class Program;
