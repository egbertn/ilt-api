using ILT.Application.Abstractions;
using ILT.Application.Configuration;
using ILT.Domain.Models;
using Microsoft.Extensions.Options;

namespace ILT.Application.Services;

public sealed class CategoryClassifier(IOptionsMonitor<IltConfiguration> options) : ICategoryClassifier
{
    public CategoryType Classify(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return CategoryType.Ignored;
        }

        var config = options.CurrentValue;

        if (config.IgnoredCategories.Contains(category))
        {
            return CategoryType.Ignored;
        }

        if (config.IncomeCategories.Contains(category))
        {
            return CategoryType.Income;
        }

        if (config.ExpenseCategories.Contains(category))
        {
            return CategoryType.Expense;
        }

        return CategoryType.Ignored;
    }
}
