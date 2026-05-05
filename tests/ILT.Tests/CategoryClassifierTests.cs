using FluentAssertions;
using ILT.Application.Services;
using ILT.Domain.Models;
using Xunit;

namespace ILT.Tests;

public class CategoryClassifierTests
{
    [Theory]
    [InlineData("Salaris", CategoryType.Income)]
    [InlineData("salaris", CategoryType.Income)]
    [InlineData("Huur", CategoryType.Expense)]
    [InlineData("Sparen", CategoryType.Ignored)]
    [InlineData("Onbekend", CategoryType.Ignored)]
    [InlineData("", CategoryType.Ignored)]
    public void Classify_returns_expected_type(string category, CategoryType expected)
    {
        var sut = new CategoryClassifier(TestHelpers.Monitor(TestHelpers.DefaultConfig()));

        sut.Classify(category).Should().Be(expected);
    }
}
