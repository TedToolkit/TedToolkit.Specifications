// -----------------------------------------------------------------------
// <copyright file="StringOperator.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Identifies ordinal literal string matching operations.
/// </summary>
internal enum StringOperator
{
    /// <summary>
    /// Matches an ordinal literal substring.
    /// </summary>
    CONTAINS = 0,

    /// <summary>
    /// Matches an ordinal literal prefix.
    /// </summary>
    STARTS_WITH = 1,

    /// <summary>
    /// Matches an ordinal literal suffix.
    /// </summary>
    ENDS_WITH = 2,
}