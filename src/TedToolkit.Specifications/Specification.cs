// -----------------------------------------------------------------------
// <copyright file="Specification.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Represents Boolean business predicates and their logical composition.
/// </summary>
public static class Specification
{
    /// <summary>
    /// Gets a specification that matches every valid candidate.
    /// </summary>
    /// <typeparam name = "T">The root candidate type.</typeparam>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<T> True<T>()
    {
        return ConstantSpecification<T>.True;
    }

    /// <summary>
    /// Gets a specification that matches no candidate.
    /// </summary>
    /// <typeparam name = "T">The root candidate type.</typeparam>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<T> False<T>()
    {
        return ConstantSpecification<T>.False;
    }
}