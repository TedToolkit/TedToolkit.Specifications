// -----------------------------------------------------------------------
// <copyright file="MemberSelection.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications;

/// <summary>
/// Selects a member from its typed parent through a generated direct accessor.
/// </summary>
/// <typeparam name = "TCandidate">The root candidate type.</typeparam>
/// <typeparam name = "TParent">The parent selection's value type.</typeparam>
/// <typeparam name = "TValue">The selected member type.</typeparam>
public sealed class MemberSelection<TCandidate, TParent, TValue> : Selection<TCandidate, TValue>
{
    private readonly Func<TParent, TValue> _getter;

    /// <summary>
    /// Creates the member selection emitted by a generated extension property.
    /// </summary>
    /// <param name = "parent">The immediate parent selection.</param>
    /// <param name = "name">One property name.</param>
    /// <param name = "getter">The generated direct property accessor.</param>
    /// <remarks>
    /// This constructor is public because generated accessors compile into the consuming assembly.
    /// Use generated properties in application code. TSPEC005 rejects handwritten accessors.
    /// </remarks>
    public MemberSelection(Selection<TCandidate, TParent> parent, string name, Func<TParent, TValue> getter)
        : base(MemberPath(parent, name))
    {
        ArgumentNullException.ThrowIfNull(getter);
        Parent = parent;
        Name = name;
        _getter = getter;
    }

    /// <summary>
    /// Gets the immediate parent selection.
    /// </summary>
    internal Selection<TCandidate, TParent> Parent { get; }

    /// <summary>
    /// Gets the property name.
    /// </summary>
    internal string Name { get; }

    /// <inheritdoc/>
    internal override bool TryGetValue(TCandidate candidate, out TValue value)
    {
        if (!Parent.TryGetValue(candidate, out var parent) || parent is null)
        {
            value = default!;
            return false;
        }

        value = _getter(parent);
        return true;
    }

    private static string MemberPath(Selection<TCandidate, TParent> parent, string name)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return parent.Path.Length == 0 ? name : parent.Path + "." + name;
    }
}