// -----------------------------------------------------------------------
// <copyright file="Diagnostics.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace TedToolkit.Specifications.Analyzers;

/// <summary>
/// Defines specification construction and SQL portability diagnostics.
/// </summary>
internal static class Diagnostics
{
    /// <summary>
    /// Describes TSPEC001.
    /// </summary>
    internal static readonly DiagnosticDescriptor Entity = new(
        "TSPEC001",
        Localization.TSPEC001.Title,
        Localization.TSPEC001.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC002.
    /// </summary>
    internal static readonly DiagnosticDescriptor PropertyType = new(
        "TSPEC002",
        Localization.TSPEC002.Title,
        Localization.TSPEC002.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC003.
    /// </summary>
    internal static readonly DiagnosticDescriptor Property = new(
        "TSPEC003",
        Localization.TSPEC003.Title,
        Localization.TSPEC003.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC004.
    /// </summary>
    internal static readonly DiagnosticDescriptor Language = new(
        "TSPEC004",
        Localization.TSPEC004.Title,
        Localization.TSPEC004.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC005.
    /// </summary>
    internal static readonly DiagnosticDescriptor Accessor = new(
        "TSPEC005",
        Localization.TSPEC005.Title,
        Localization.TSPEC005.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC006.
    /// </summary>
    internal static readonly DiagnosticDescriptor Incomplete = new(
        "TSPEC006",
        Localization.TSPEC006.Title,
        Localization.TSPEC006.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC007.
    /// </summary>
    internal static readonly DiagnosticDescriptor NullCheck = new(
        "TSPEC007",
        Localization.TSPEC007.Title,
        Localization.TSPEC007.Message,
        "Specification", DiagnosticSeverity.Warning, true);

    /// <summary>
    /// Describes TSPEC008.
    /// </summary>
    internal static readonly DiagnosticDescriptor ObjectMethod = new(
        "TSPEC008",
        Localization.TSPEC008.Title,
        Localization.TSPEC008.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC009.
    /// </summary>
    internal static readonly DiagnosticDescriptor HiddenProperty = new(
        "TSPEC009",
        Localization.TSPEC009.Title,
        Localization.TSPEC009.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC010.
    /// </summary>
    internal static readonly DiagnosticDescriptor StringValue = new(
        "TSPEC010",
        Localization.TSPEC010.Title,
        Localization.TSPEC010.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC011.
    /// </summary>
    internal static readonly DiagnosticDescriptor Implementation = new(
        "TSPEC011",
        Localization.TSPEC011.Title,
        Localization.TSPEC011.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC012.
    /// </summary>
    internal static readonly DiagnosticDescriptor Computed = new(
        "TSPEC012",
        Localization.TSPEC012.Title,
        Localization.TSPEC012.Message,
        "Specification", DiagnosticSeverity.Warning, true);

    /// <summary>
    /// Describes TSPEC013.
    /// </summary>
    internal static readonly DiagnosticDescriptor UnsupportedOperation = new(
        "TSPEC013",
        Localization.TSPEC013.Title,
        Localization.TSPEC013.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC014.
    /// </summary>
    internal static readonly DiagnosticDescriptor NumberValue = new(
        "TSPEC014",
        Localization.TSPEC014.Title,
        Localization.TSPEC014.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC015.
    /// </summary>
    internal static readonly DiagnosticDescriptor NullPattern = new(
        "TSPEC015",
        Localization.TSPEC015.Title,
        Localization.TSPEC015.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC016.
    /// </summary>
    internal static readonly DiagnosticDescriptor ReferenceEquality = new(
        "TSPEC016",
        Localization.TSPEC016.Title,
        Localization.TSPEC016.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC017.
    /// </summary>
    internal static readonly DiagnosticDescriptor CandidateRequired = new(
        "TSPEC017",
        Localization.TSPEC017.Title,
        Localization.TSPEC017.Message,
        "Specification", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Describes TSPEC018.
    /// </summary>
    internal static readonly DiagnosticDescriptor MemberCollision = new(
        "TSPEC018",
        Localization.TSPEC018.Title,
        Localization.TSPEC018.Message,
        "Specification", DiagnosticSeverity.Error, true);
}