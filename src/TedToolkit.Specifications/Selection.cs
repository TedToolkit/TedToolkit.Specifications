// -----------------------------------------------------------------------
// <copyright file="Selection.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace TedToolkit.Specifications;

/// <summary>
/// Selects typed values from a root candidate and constructs comparison predicates.
/// </summary>
/// <typeparam name = "TCandidate">The root candidate type.</typeparam>
/// <typeparam name = "TValue">The selected value type.</typeparam>
public abstract class Selection<TCandidate, TValue>
{
    /// <summary>
    /// Creates a selection with its complete member path.
    /// </summary>
    /// <param name = "path">The complete member path; empty for the root.</param>
    private protected Selection(string path)
    {
        Path = path;
    }

    /// <summary>
    /// Gets the complete member path, or an empty path for the root.
    /// </summary>
    internal string Path { get; }

    /// <summary>
    /// Attempts to read a selected value without substituting defaults for absent parents.
    /// </summary>
    /// <param name = "candidate">The root candidate to evaluate.</param>
    /// <param name = "value">The selected value when present; otherwise its default value.</param>
    /// <returns>Whether the selected value is present.</returns>
    internal abstract bool TryGetValue(TCandidate candidate, out TValue value);

    /// <summary>
    /// Tests equality, treating an absent parent as null.
    /// </summary>
    /// <param name = "value">The captured comparison or matching value.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> EqualTo(TValue value)
    {
        return Compare(value, ComparisonOperator.EQUAL);
    }

    /// <summary>
    /// Builds the exact Boolean complement of equality.
    /// </summary>
    /// <param name = "value">The captured comparison or matching value.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> NotEqualTo(TValue value)
    {
        return Compare(value, ComparisonOperator.NOT_EQUAL);
    }

    /// <summary>
    /// Builds strict numeric or temporal ordering.
    /// </summary>
    /// <param name = "value">The captured comparison or matching value.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> GreaterThan(TValue value)
    {
        return Compare(value, ComparisonOperator.GREATER_THAN);
    }

    /// <summary>
    /// Builds inclusive numeric or temporal ordering.
    /// </summary>
    /// <param name = "value">The captured comparison or matching value.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> GreaterThanOrEqual(TValue value)
    {
        return Compare(value, ComparisonOperator.GREATER_THAN_OR_EQUAL);
    }

    /// <summary>
    /// Builds strict reverse numeric or temporal ordering.
    /// </summary>
    /// <param name = "value">The captured comparison or matching value.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> LessThan(TValue value)
    {
        return Compare(value, ComparisonOperator.LESS_THAN);
    }

    /// <summary>
    /// Builds inclusive reverse numeric or temporal ordering.
    /// </summary>
    /// <param name = "value">The captured comparison or matching value.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> LessThanOrEqual(TValue value)
    {
        return Compare(value, ComparisonOperator.LESS_THAN_OR_EQUAL);
    }

    /// <summary>
    /// Tests membership in an immediate immutable snapshot of the supplied values.
    /// </summary>
    /// <param name = "values">The membership values to snapshot.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> In(IEnumerable<TValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var snapshot = values.ToArray();
        return new InSpecification<TCandidate, TValue>(this, snapshot);
    }

    /// <summary>
    /// Matches a null selected value or an absent member parent.
    /// </summary>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> IsNull()
    {
        return new NullSpecification<TCandidate>(Path, HasNullValue);
    }

    /// <summary>
    /// Matches a present non-null selected value.
    /// </summary>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> IsNotNull()
    {
        return IsNull().Not();
    }

    /// <summary>
    /// Builds ordinal literal substring matching.
    /// </summary>
    /// <param name = "value">The captured comparison or matching value.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> Contains(string value)
    {
        return Match(value, StringOperator.CONTAINS);
    }

    /// <summary>
    /// Builds ordinal literal prefix matching.
    /// </summary>
    /// <param name = "value">The captured comparison or matching value.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> StartsWith(string value)
    {
        return Match(value, StringOperator.STARTS_WITH);
    }

    /// <summary>
    /// Builds ordinal literal suffix matching.
    /// </summary>
    /// <param name = "value">The captured comparison or matching value.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> EndsWith(string value)
    {
        return Match(value, StringOperator.ENDS_WITH);
    }

    /// <summary>
    /// Builds typed equality.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    [SuppressMessage("Design", "S3875", Justification = "Equality constructs a typed predicate rather than comparing selection identity.")]
    public static Specification<TCandidate> operator ==([NotNull] Selection<TCandidate, TValue>? left, TValue right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.EqualTo(right);
    }

    /// <summary>
    /// Builds typed inequality.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<TCandidate> operator !=([NotNull] Selection<TCandidate, TValue>? left, TValue right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.NotEqualTo(right);
    }

    /// <summary>
    /// Builds a strict ordered comparison.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<TCandidate> operator >(Selection<TCandidate, TValue> left, TValue right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.GreaterThan(right);
    }

    /// <summary>
    /// Builds a strict ordered comparison.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<TCandidate> operator <(Selection<TCandidate, TValue> left, TValue right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.LessThan(right);
    }

    /// <summary>
    /// Builds an inclusive ordered comparison.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<TCandidate> operator >=(Selection<TCandidate, TValue> left, TValue right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.GreaterThanOrEqual(right);
    }

    /// <summary>
    /// Builds an inclusive ordered comparison.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<TCandidate> operator <=(Selection<TCandidate, TValue> left, TValue right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.LessThanOrEqual(right);
    }

    /// <summary>
    /// Builds typed equality.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    [SuppressMessage("Design", "S3875", Justification = "Equality constructs a typed predicate rather than comparing selection identity.")]
    public static Specification<TCandidate> operator ==(TValue left, [NotNull] Selection<TCandidate, TValue>? right)
    {
        ArgumentNullException.ThrowIfNull(right);
        return right.EqualTo(left);
    }

    /// <summary>
    /// Builds typed inequality.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<TCandidate> operator !=(TValue left, [NotNull] Selection<TCandidate, TValue>? right)
    {
        ArgumentNullException.ThrowIfNull(right);
        return right.NotEqualTo(left);
    }

    /// <summary>
    /// Builds a strict ordered comparison.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<TCandidate> operator >(TValue left, Selection<TCandidate, TValue> right)
    {
        ArgumentNullException.ThrowIfNull(right);
        return right.LessThan(left);
    }

    /// <summary>
    /// Builds a strict ordered comparison.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<TCandidate> operator <(TValue left, Selection<TCandidate, TValue> right)
    {
        ArgumentNullException.ThrowIfNull(right);
        return right.GreaterThan(left);
    }

    /// <summary>
    /// Builds an inclusive ordered comparison.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<TCandidate> operator >=(TValue left, Selection<TCandidate, TValue> right)
    {
        ArgumentNullException.ThrowIfNull(right);
        return right.LessThanOrEqual(left);
    }

    /// <summary>
    /// Builds an inclusive ordered comparison.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<TCandidate> operator <=(TValue left, Selection<TCandidate, TValue> right)
    {
        ArgumentNullException.ThrowIfNull(right);
        return right.GreaterThanOrEqual(left);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
    }

    /// <summary>
    /// Builds a comparison chosen at runtime.
    /// </summary>
    /// <param name="value">The comparison operand.</param>
    /// <param name="operation">The comparison to construct.</param>
    /// <returns>The corresponding typed specification.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The comparison operation is undefined.</exception>
    /// <exception cref="NotSupportedException">The selected scalar category does not support ordering.</exception>
    public Specification<TCandidate> Compare(TValue value, ComparisonOperator operation)
    {
        if (!Enum.IsDefined(operation))
        {
            throw new ArgumentOutOfRangeException(nameof(operation));
        }

        var type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        if (operation is not (ComparisonOperator.EQUAL or ComparisonOperator.NOT_EQUAL)
            && (type == typeof(string) || type == typeof(bool) || type == typeof(Guid) || type.IsEnum))
        {
            throw new NotSupportedException("This scalar category does not support ordered specifications.");
        }

        return new ComparisonSpecification<TCandidate, TValue>(this, value, operation);
    }

    private bool HasNullValue(TCandidate candidate)
    {
        return !TryGetValue(candidate, out var value) || value is null;
    }

    private Specification<TCandidate> Match(string value, StringOperator operation)
    {
        if (typeof(TValue) != typeof(string))
        {
            throw new NotSupportedException("String matching requires a string selection.");
        }

        ArgumentNullException.ThrowIfNull(value);
        return new StringSpecification<TCandidate, TValue>(this, value, operation);
    }
}