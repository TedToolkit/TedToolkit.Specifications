// -----------------------------------------------------------------------
// <copyright file="InSpecification{TCandidate}.cs" company="TedToolkit">
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
/// <param name = "path">The complete member path; empty for the root.</param>
/// <param name = "valueType">The CLR type of the selected value.</param>
/// <param name = "values">The membership values to snapshot.</param>
internal abstract class InSpecification<TCandidate>(
    string path,
    Type valueType,
    IReadOnlyList<object?> values) : Specification<TCandidate>
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
    /// Gets the captured membership values.
    /// </summary>
    internal IReadOnlyList<object?> Values { get; } = values;
}