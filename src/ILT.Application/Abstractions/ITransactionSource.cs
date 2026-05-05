using ILT.Domain.Models;

namespace ILT.Application.Abstractions;

public interface ITransactionSource
{
    Task<IReadOnlyList<Transaction>> GetTransactionsAsync(
        string accountNumber,
        CancellationToken cancellationToken);
}
