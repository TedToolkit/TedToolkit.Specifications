// -----------------------------------------------------------------------
// <copyright file="ConstantSpecification.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Returns a fixed truth value for every valid candidate.
/// </summary>
/// <typeparam name = "T">The root candidate type.</typeparam>
internal sealed class ConstantSpecification<T> : Specification<T>
{
    /// <summary>
    /// Creates the immutable selection or predicate from its operands.
    /// </summary>
    /// <param name = "value">The captured comparison or matching value.</param>
    private ConstantSpecification(bool value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the shared predicate that matches every valid candidate.
    /// </summary>
    internal static ConstantSpecification<T> True { get; } = new(true);

    /// <summary>
    /// Gets the shared predicate that matches no candidate.
    /// </summary>
    internal static ConstantSpecification<T> False { get; } = new(false);

    /// <summary>
    /// Gets the captured operand value.
    /// </summary>
    internal bool Value { get; }

    /// <inheritdoc/>
    private protected override bool IsSatisfiedByCore(T candidate)
    {
        return Value;
    }
}