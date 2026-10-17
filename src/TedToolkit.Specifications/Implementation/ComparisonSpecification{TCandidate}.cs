// -----------------------------------------------------------------------
// <copyright file="ComparisonSpecification{TCandidate}.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Compares a selected value while preserving two-valued null semantics.
/// </summary>
/// <typeparam name = "TCandidate">The root candidate type.</typeparam>
/// <param name = "path">The complete member path; empty for the root.</param>
/// <param name = "valueType">The CLR type of the selected value.</param>
/// <param name = "value">The captured comparison or matching value.</param>
/// <param name = "operation">The comparison or matching operation.</param>
internal abstract class ComparisonSpecification<TCandidate>(
    string path,
    Type valueType,
    object? value,
    ComparisonOperator operation) : Specification<TCandidate>
{
    /// <summary>
    /// Gets the complete member path, or an empty path for the root.
    /// </summary>
    internal string MemberPath { get; } = path;

    /// <summary>
    /// Gets the CLR type of the selected value.
    /// </summary>
    internal Type ValueType { get; } = valueType;

    /// <summary>
    /// Gets the captured operand value.
    /// </summary>
    internal object? Value { get; } = value;

    /// <summary>
    /// Gets the selected comparison or matching operation.
    /// </summary>
    internal ComparisonOperator Operation { get; } = operation;
}