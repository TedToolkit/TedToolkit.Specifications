// -----------------------------------------------------------------------
// <copyright file="StringSpecification{TCandidate}.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Describes the operand and operation of a string predicate.
/// </summary>
/// <typeparam name = "TCandidate">The root candidate type.</typeparam>
/// <param name = "path">The complete member path; empty for the root.</param>
/// <param name = "value">The captured comparison or matching value.</param>
/// <param name = "operation">The comparison or matching operation.</param>
internal abstract class StringSpecification<TCandidate>(string path, string value, StringOperator operation) : Specification<TCandidate>
{
    /// <summary>
    /// Gets the complete member path, or an empty path for the root.
    /// </summary>
    internal string MemberPath { get; } = path;

    /// <summary>
    /// Gets the captured operand value.
    /// </summary>
    internal string Value { get; } = value;

    /// <summary>
    /// Gets the selected comparison or matching operation.
    /// </summary>
    internal StringOperator Operation { get; } = operation;
}