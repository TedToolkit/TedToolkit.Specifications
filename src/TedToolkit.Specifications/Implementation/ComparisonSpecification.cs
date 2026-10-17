// -----------------------------------------------------------------------
// <copyright file="ComparisonSpecification.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Compares a selected value while preserving two-valued null semantics.
/// </summary>
/// <typeparam name = "TCandidate">The root candidate type.</typeparam>
/// <typeparam name = "TValue">The selected value type.</typeparam>
/// <param name = "operand">The selected operand or child predicate.</param>
/// <param name = "value">The captured comparison or matching value.</param>
/// <param name = "operation">The comparison or matching operation.</param>
internal sealed class ComparisonSpecification<TCandidate, TValue>(
    Selection<TCandidate, TValue> operand,
    TValue value,
    ComparisonOperator operation) : ComparisonSpecification<TCandidate>(operand.Path, typeof(TValue), value, operation)
{
    /// <inheritdoc/>
    private protected override bool IsSatisfiedByCore(TCandidate candidate)
    {
        var present = operand.TryGetValue(candidate, out var actual);
        var equal = present ? EqualityComparer<TValue>.Default.Equals(actual, value) : value is null;
        if (Operation == ComparisonOperator.EQUAL)
        {
            return equal;
        }

        if (Operation == ComparisonOperator.NOT_EQUAL)
        {
            return !equal;
        }

        if (!present || actual is null || value is null)
        {
            return false;
        }

        var comparison = Comparer<TValue>.Default.Compare(actual, value);
        return Operation switch
        {
            ComparisonOperator.GREATER_THAN => comparison > 0,
            ComparisonOperator.GREATER_THAN_OR_EQUAL => comparison >= 0,
            ComparisonOperator.LESS_THAN => comparison < 0,
            ComparisonOperator.LESS_THAN_OR_EQUAL => comparison <= 0,
            _ => throw new InvalidOperationException("Unknown comparison operation."),
        };
    }
}