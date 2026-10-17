// -----------------------------------------------------------------------
// <copyright file="InSpecification.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Frozen;

namespace TedToolkit.Specifications;

/// <summary>
/// Tests a selected value against a frozen membership snapshot.
/// </summary>
/// <typeparam name = "TCandidate">The root candidate type.</typeparam>
/// <typeparam name = "TValue">The selected value type.</typeparam>
/// <param name = "operand">The selected operand or child predicate.</param>
/// <param name = "values">The membership values to snapshot.</param>
internal sealed class InSpecification<TCandidate, TValue>(
    Selection<TCandidate, TValue> operand,
    TValue[] values) : InSpecification<TCandidate>(
        operand.Path,
        typeof(TValue),
        Array.AsReadOnly(values.Select(value => (object?)value).ToArray()))
{
    private readonly FrozenSet<TValue> _values = values.ToFrozenSet();

    private readonly bool _hasNull = values.Any(value => value is null);

    /// <inheritdoc/>
    private protected override bool IsSatisfiedByCore(TCandidate candidate)
    {
        if (!operand.TryGetValue(candidate, out var value))
        {
            return _hasNull;
        }

        return _values.Contains(value);
    }
}