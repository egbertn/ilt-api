using ILT.Domain.Models;

namespace ILT.Application.Abstractions;

public interface IIltCalculator
{
    IltResult Calculate(IEnumerable<Transaction> transactions, IReadOnlyList<string> accountNumbers);
}
