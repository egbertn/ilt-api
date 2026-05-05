using ILT.Domain.Models;

namespace ILT.Application.Abstractions;

public interface ICategoryClassifier
{
    CategoryType Classify(string category);
}
