// -----------------------------------------------------------------------
// <copyright file="AndSpecification.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Requires both child predicates to match, short-circuiting evaluation.
/// </summary>
/// <typeparam name = "T">The root candidate type.</typeparam>
internal sealed class AndSpecification<T> : Specification<T>
{
    /// <summary>
    /// Creates the immutable selection or predicate from its operands.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    internal AndSpecification(Specification<T> left, Specification<T> right)
    {
        (Left, Right) = (left, right);
    }

    /// <summary>
    /// Gets the first child predicate.
    /// </summary>
    internal Specification<T> Left { get; }

    /// <summary>
    /// Gets the second child predicate.
    /// </summary>
    internal Specification<T> Right { get; }
}