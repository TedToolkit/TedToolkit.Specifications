// -----------------------------------------------------------------------
// <copyright file="IdentitySelection.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Selects the root candidate without accessing a member.
/// </summary>
/// <typeparam name = "T">The root candidate type.</typeparam>
internal sealed class IdentitySelection<T> : Selection<T, T>
{
    /// <summary>
    /// Creates the immutable selection or predicate from its operands.
    /// </summary>
    private IdentitySelection()
        : base("")
    {
    }

    /// <summary>
    /// Gets the shared root identity selection.
    /// </summary>
    internal static IdentitySelection<T> Instance { get; } = new();

    /// <inheritdoc/>
    internal override bool TryGetValue(T candidate, out T value)
    {
        value = candidate;
        return true;
    }
}