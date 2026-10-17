// -----------------------------------------------------------------------
// <copyright file="Specification{TCandidate}.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace TedToolkit.Specifications;

/// <summary>
/// Represents Boolean business predicates and their logical composition.
/// </summary>
/// <typeparam name = "TCandidate">The root candidate type.</typeparam>
public abstract class Specification<TCandidate>
{
    /// <summary>
    /// Creates a predicate within the framework's closed implementation hierarchy.
    /// </summary>
    private protected Specification()
    {
    }

    /// <summary>
    /// Combines this predicate with another using AND.
    /// </summary>
    /// <param name="other">The other predicate.</param>
    /// <returns>The composed specification.</returns>
    public Specification<TCandidate> BitwiseAnd(Specification<TCandidate> other)
    {
        return And(other);
    }

    /// <summary>
    /// Combines this predicate with another using OR.
    /// </summary>
    /// <param name="other">The other predicate.</param>
    /// <returns>The composed specification.</returns>
    public Specification<TCandidate> BitwiseOr(Specification<TCandidate> other)
    {
        return Or(other);
    }

    /// <summary>
    /// Takes the exact Boolean complement.
    /// </summary>
    /// <returns>The negated specification.</returns>
    public Specification<TCandidate> LogicalNot()
    {
        return Not();
    }

    /// <summary>
    /// Tests whether a candidate satisfies this specification.
    /// </summary>
    /// <param name = "candidate">The root candidate to evaluate.</param>
    /// <returns>Whether the candidate satisfies this specification.</returns>
    public bool IsSatisfiedBy(TCandidate candidate)
    {
        if (!typeof(TCandidate).IsValueType && typeof(TCandidate) != typeof(string))
        {
            ArgumentNullException.ThrowIfNull(candidate);
        }

        return IsSatisfiedByCore(candidate);
    }

    /// <summary>
    /// Evaluates a composite iteratively, preserving left-to-right short-circuiting; leaves override this method.
    /// </summary>
    /// <param name = "candidate">The root candidate to evaluate.</param>
    /// <returns>Whether the candidate satisfies this predicate.</returns>
    private protected virtual bool IsSatisfiedByCore(TCandidate candidate)
    {
        var pending = new Stack<(Specification<TCandidate>? Right, bool ContinueOn)>();
        Specification<TCandidate>? current = this;
        var result = false;
        while (current is not null)
        {
            switch (current)
            {
                case AndSpecification<TCandidate> and:
                    pending.Push((and.Right, true));
                    current = and.Left;
                    break;

                case OrSpecification<TCandidate> or:
                    pending.Push((or.Right, false));
                    current = or.Left;
                    break;

                case NotSpecification<TCandidate> not:
                    pending.Push((null, false));
                    current = not.Operand;
                    break;

                default:
                    result = current.IsSatisfiedByCore(candidate);
                    current = null;
                    break;
            }

            while (current is null && pending.TryPop(out var continuation))
            {
                if (continuation.Right is null)
                {
                    result = !result;
                }
                else if (result == continuation.ContinueOn)
                {
                    current = continuation.Right;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Requires both predicates to match, short-circuiting in memory.
    /// </summary>
    /// <param name = "other">The predicate to compose.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> And(Specification<TCandidate> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new AndSpecification<TCandidate>(this, other);
    }

    /// <summary>
    /// Requires either predicate to match, short-circuiting in memory.
    /// </summary>
    /// <param name = "other">The predicate to compose.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> Or(Specification<TCandidate> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new OrSpecification<TCandidate>(this, other);
    }

    /// <summary>
    /// Builds the Boolean complement of this predicate.
    /// </summary>
    /// <returns>A specification representing the requested predicate.</returns>
    public Specification<TCandidate> Not()
    {
        return new NotSpecification<TCandidate>(this);
    }

    /// <summary>
    /// Builds a logical conjunction.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<TCandidate> operator &(Specification<TCandidate> left, Specification<TCandidate> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.And(right);
    }

    /// <summary>
    /// Builds a logical disjunction.
    /// </summary>
    /// <param name = "left">The left operand.</param>
    /// <param name = "right">The right operand.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<TCandidate> operator |(Specification<TCandidate> left, Specification<TCandidate> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.Or(right);
    }

    /// <summary>
    /// Preserves both operands when constructing a conditional OR specification.
    /// </summary>
    /// <param name = "value">The captured comparison or matching value.</param>
    /// <returns>Whether construction may omit the other operand.</returns>
    [SuppressMessage("Usage", "CA2225",
        Justification = "Conditional composition has no Boolean value without a candidate; use IsSatisfiedBy for evaluation.")]
    public static bool operator true(Specification<TCandidate> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return false;
    }

    /// <summary>
    /// Preserves both operands when constructing a conditional AND specification.
    /// </summary>
    /// <param name = "value">The captured comparison or matching value.</param>
    /// <returns>Whether construction may omit the other operand.</returns>
    public static bool operator false(Specification<TCandidate> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return false;
    }

    /// <summary>
    /// Builds the Boolean complement.
    /// </summary>
    /// <param name = "value">The captured comparison or matching value.</param>
    /// <returns>A specification representing the requested predicate.</returns>
    public static Specification<TCandidate> operator !(Specification<TCandidate> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Not();
    }
}