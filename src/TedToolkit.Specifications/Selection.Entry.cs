// -----------------------------------------------------------------------
// <copyright file="Selection.Entry.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Selects typed values from a root candidate and constructs comparison predicates.
/// </summary>
public static class Selection
{
    /// <summary>
    /// Creates a selection of the root candidate.
    /// </summary>
    /// <typeparam name = "T">The root candidate type.</typeparam>
    /// <returns>The typed root selection.</returns>
    public static Selection<T, T> For<T>()
    {
        return IdentitySelection<T>.Instance;
    }
}