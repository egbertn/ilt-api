using ILT.Domain.Models;

namespace ILT.Application.Abstractions;

public interface IIltService
{
    Task<IltResult> CalculateAsync(
        IReadOnlyList<string> accountNumbers,
        CancellationToken cancellationToken);
}
