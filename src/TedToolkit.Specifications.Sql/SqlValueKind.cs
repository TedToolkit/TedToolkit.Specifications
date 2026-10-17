// -----------------------------------------------------------------------
// <copyright file="SqlValueKind.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Sql;

/// <summary>
/// Represents the scalar storage categories understood by translators.
/// </summary>
internal enum SqlValueKind
{
    /// <summary>
    /// A signed 64-bit-compatible integer.
    /// </summary>
    INTEGER = 0,

    /// <summary>
    /// A Boolean.
    /// </summary>
    BOOLEAN = 1,

    /// <summary>
    /// A valid Unicode string without zero characters.
    /// </summary>
    STRING = 2,

    /// <summary>
    /// A fixed-precision decimal.
    /// </summary>
    DECIMAL = 3,

    /// <summary>
    /// A finite floating-point number.
    /// </summary>
    FLOATING_POINT = 4,

    /// <summary>
    /// A universally unique identifier.
    /// </summary>
    GUID = 5,

    /// <summary>
    /// A calendar date.
    /// </summary>
    DATE = 6,

    /// <summary>
    /// A wall-clock time.
    /// </summary>
    TIME = 7,

    /// <summary>
    /// A timestamp compared by wall-clock ticks.
    /// </summary>
    DATE_TIME = 8,

    /// <summary>
    /// An instant with a UTC offset.
    /// </summary>
    DATE_TIME_OFFSET = 9,

    /// <summary>
    /// An enum backed by a signed 64-bit-compatible integer.
    /// </summary>
    ENUM = 10,
}