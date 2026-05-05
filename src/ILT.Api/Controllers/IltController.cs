using ILT.Application.Abstractions;
using ILT.Application.Configuration;
using ILT.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ILT.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class IltController(
    IIltService iltService,
    IOptionsMonitor<TransactionsApiOptions> apiOptions,
    ILogger<IltController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IltResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IltResult>> Get(
        [FromQuery] string[]? accounts,
        CancellationToken cancellationToken)
    {
        var configured = apiOptions.CurrentValue.AccountNumbers;
        var requested = (accounts is { Length: > 0 } ? accounts : [.. configured])
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requested.Length == 0)
        {
            return BadRequest("No account numbers were provided or configured.");
        }

        try
        {
            var result = await iltService.CalculateAsync(requested, cancellationToken);

            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Upstream transactions API failed.");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Upstream transactions API failed.",
                detail = ex.Message
            });
        }
    }
}
