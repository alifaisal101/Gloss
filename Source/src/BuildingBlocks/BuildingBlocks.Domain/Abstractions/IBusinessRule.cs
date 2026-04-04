using BuildingBlocks.Domain.Errors;

namespace BuildingBlocks.Domain.Abstractions;

public interface IBusinessRule
{
    /// <summary>
    /// Returns a specific DomainError if the rule is broken, otherwise DomainError.None.
    /// </summary>
    DomainError Check();
}