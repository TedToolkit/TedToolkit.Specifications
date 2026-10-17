// -----------------------------------------------------------------------
// <copyright file="NotSpecification.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Evaluates the exact Boolean complement of a child predicate.
/// </summary>
/// <typeparam name = "T">The root candidate type.</typeparam>
internal sealed class NotSpecification<T> : Specification<T>
{
    /// <summary>
    /// Creates the immutable selection or predicate from its operands.
    /// </summary>
    /// <param name = "operand">The selected operand or child predicate.</param>
    internal NotSpecification(Specification<T> operand)
    {
        Operand = operand;
    }

    /// <summary>
    /// Gets the child predicate to negate.
    /// </summary>
    internal Specification<T> Operand { get; }
}