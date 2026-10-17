// -----------------------------------------------------------------------
// <copyright file="NullSpecification.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Matches a null value or an absent member parent.
/// </summary>
/// <typeparam name = "TCandidate">The root candidate type.</typeparam>
/// <param name="path">The complete member path.</param>
/// <param name="isNull">The selection's fixed null check.</param>
internal sealed class NullSpecification<TCandidate>(string path, Func<TCandidate, bool> isNull) : Specification<TCandidate>
{
    /// <summary>
    /// Gets the selected member path.
    /// </summary>
    internal string MemberPath { get; } = path;

    /// <inheritdoc/>
    private protected override bool IsSatisfiedByCore(TCandidate candidate)
    {
        return isNull(candidate);
    }
}