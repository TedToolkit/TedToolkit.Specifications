// -----------------------------------------------------------------------
// <copyright file="ComparisonOperator.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Identifies a comparison between a selected value and an operand.
/// </summary>
public enum ComparisonOperator
{
    /// <summary>
    /// Matches equal values, including null or absent members.
    /// </summary>
    EQUAL = 0,

    /// <summary>
    /// Matches the exact complement of equality.
    /// </summary>
    NOT_EQUAL = 1,

    /// <summary>
    /// Matches values strictly greater than the operand.
    /// </summary>
    GREATER_THAN = 2,

    /// <summary>
    /// Matches values greater than or equal to the operand.
    /// </summary>
    GREATER_THAN_OR_EQUAL = 3,

    /// <summary>
    /// Matches values strictly less than the operand.
    /// </summary>
    LESS_THAN = 4,

    /// <summary>
    /// Matches values less than or equal to the operand.
    /// </summary>
    LESS_THAN_OR_EQUAL = 5,
}