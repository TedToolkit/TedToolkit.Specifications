// -----------------------------------------------------------------------
// <copyright file="StringSpecification.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Matches a selected string with ordinal literal semantics.
/// </summary>
/// <typeparam name = "TCandidate">The root candidate type.</typeparam>
/// <typeparam name = "TValue">The selected value type.</typeparam>
/// <param name = "operand">The selected operand or child predicate.</param>
/// <param name = "value">The captured comparison or matching value.</param>
/// <param name = "operation">The comparison or matching operation.</param>
internal sealed class StringSpecification<TCandidate, TValue>(
    Selection<TCandidate, TValue> operand,
    string value,
    StringOperator operation) : StringSpecification<TCandidate>(operand.Path, value, operation)
{
    /// <inheritdoc/>
    private protected override bool IsSatisfiedByCore(TCandidate candidate)
    {
        if (!operand.TryGetValue(candidate, out var read) || read is not string actual)
        {
            return false;
        }

        return Operation switch
        {
            StringOperator.CONTAINS => actual.Contains(Value, StringComparison.Ordinal),
            StringOperator.STARTS_WITH => actual.StartsWith(Value, StringComparison.Ordinal),
            StringOperator.ENDS_WITH => actual.EndsWith(Value, StringComparison.Ordinal),
            _ => throw new InvalidOperationException("Unknown string operation."),
        };
    }
}