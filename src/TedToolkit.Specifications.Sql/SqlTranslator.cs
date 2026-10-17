// -----------------------------------------------------------------------
// <copyright file="SqlTranslator.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace TedToolkit.Specifications.Sql;

/// <summary>
/// Translates typed selections and specifications using database-specific SQL syntax.
/// </summary>
public abstract class SqlTranslator
{
    /// <summary>
    /// Creates a stateless SQL translator.
    /// </summary>
    protected SqlTranslator()
    {
    }

    /// <summary>
    /// Gets the parameter limit supported by this database.
    /// </summary>
    protected abstract int MaximumParameters { get; }

    /// <summary>
    /// Translates a selection into a column expression for SELECT.
    /// </summary>
    /// <typeparam name="TCandidate">The root candidate type.</typeparam>
    /// <typeparam name="TValue">The selected value type.</typeparam>
    /// <param name="selection">The selected column or member path.</param>
    /// <param name="columns">Optional overrides of column names and table aliases.</param>
    /// <returns>A quoted column expression without a SELECT or FROM clause.</returns>
    public virtual string ToSql<TCandidate, TValue>(Selection<TCandidate, TValue> selection, SqlColumnMap<TCandidate>? columns = null)
    {
        ArgumentNullException.ThrowIfNull(selection);
        return (columns ?? SqlColumnMap<TCandidate>.Empty).Resolve(selection.Path);
    }

    /// <summary>
    /// Translates a specification into a parameterized condition for WHERE or HAVING.
    /// </summary>
    /// <typeparam name="TCandidate">The root candidate type.</typeparam>
    /// <param name="specification">The predicate to translate.</param>
    /// <param name="parameters">The immutable parameter names and provider-ready values, without @ prefixes.</param>
    /// <param name="columns">Optional overrides of column names and table aliases.</param>
    /// <param name="parameterPrefix">An ASCII identifier prefix unique within the containing command.</param>
    /// <param name="maxParameters">A positive parameter budget within the database's limit.</param>
    /// <returns>The SQL condition without a WHERE keyword.</returns>
    /// <exception cref="ArgumentException">The parameter prefix is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The parameter budget is invalid.</exception>
    /// <exception cref="NotSupportedException">A selected type or predicate has no supported SQL representation.</exception>
    public string ToSql<TCandidate>(Specification<TCandidate> specification, out IReadOnlyDictionary<string, object?> parameters,
        SqlColumnMap<TCandidate>? columns = null, string parameterPrefix = "spec", int maxParameters = 1000)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterPrefix);
        if (!IsIdentifierStart(parameterPrefix[0])
            || parameterPrefix.Any(character => !IsIdentifierStart(character) && !char.IsAsciiDigit(character)))
        {
            throw new ArgumentException("The parameter prefix must be an ASCII identifier.", nameof(parameterPrefix));
        }

        if (maxParameters <= 0 || maxParameters > MaximumParameters)
        {
            throw new ArgumentOutOfRangeException(nameof(maxParameters));
        }

        var writer = new Writer<TCandidate>(this, columns ?? SqlColumnMap<TCandidate>.Empty, parameterPrefix, maxParameters);
        var predicate = writer.Write(specification);
        parameters = new ReadOnlyDictionary<string, object?>(writer.Parameters);
        return predicate;
    }

    /// <summary>
    /// Applies ordinal text collation to an expression.
    /// </summary>
    /// <param name="expression">The SQL expression.</param>
    /// <returns>The collated expression.</returns>
    protected abstract string Collate(string expression);

    /// <summary>
    /// Normalizes a stored or parameterized instant for comparison.
    /// </summary>
    /// <param name="expression">The SQL expression.</param>
    /// <returns>The comparable instant expression.</returns>
    protected virtual string FormatInstant(string expression)
    {
        return expression;
    }

    /// <summary>
    /// Builds literal substring matching.
    /// </summary>
    /// <param name="column">The collated column expression.</param>
    /// <param name="parameter">The bound parameter expression.</param>
    /// <returns>The substring condition.</returns>
    protected abstract string Contains(string column, string parameter);

    /// <summary>
    /// Builds literal prefix matching.
    /// </summary>
    /// <param name="column">The collated column expression.</param>
    /// <param name="parameter">The bound parameter expression.</param>
    /// <returns>The prefix condition.</returns>
    protected abstract string StartsWith(string column, string parameter);

    /// <summary>
    /// Builds literal suffix matching.
    /// </summary>
    /// <param name="column">The collated column expression.</param>
    /// <param name="parameter">The bound parameter expression.</param>
    /// <returns>The suffix condition.</returns>
    protected abstract string EndsWith(string column, string parameter);

    private static bool IsIdentifierStart(char character)
    {
        return char.IsAsciiLetter(character) || character == '_';
    }

    private sealed class Writer<TEntity>(SqlTranslator translator, SqlColumnMap<TEntity> columns, string prefix, int maxParameters)
    {
        internal Dictionary<string, object?> Parameters { get; } = new(StringComparer.Ordinal);

        internal string Write(Specification<TEntity> specification)
        {
            var sql = new StringBuilder();
            var pending = new Stack<(Specification<TEntity>? Predicate, string? Text)>();
            pending.Push((specification, null));
            while (pending.TryPop(out var part))
            {
                switch (part.Predicate)
                {
                    case null:
                        sql.Append(part.Text);
                        break;

                    case AndSpecification<TEntity> and:
                        sql.Append('(');
                        pending.Push((null, ")"));
                        pending.Push((and.Right, null));
                        pending.Push((null, " AND "));
                        pending.Push((and.Left, null));
                        break;

                    case OrSpecification<TEntity> or:
                        sql.Append('(');
                        pending.Push((null, ")"));
                        pending.Push((or.Right, null));
                        pending.Push((null, " OR "));
                        pending.Push((or.Left, null));
                        break;

                    case NotSpecification<TEntity> not:
                        sql.Append("(NOT ");
                        pending.Push((null, ")"));
                        pending.Push((not.Operand, null));
                        break;

                    default:
                        sql.Append(WriteLeaf(part.Predicate));
                        break;
                }
            }

            return sql.ToString();
        }

        private string WriteLeaf(Specification<TEntity> specification)
        {
            return specification switch
            {
                ConstantSpecification<TEntity> constant => constant.Value ? "(1 = 1)" : "(1 = 0)",
                ComparisonSpecification<TEntity> comparison => Comparison(comparison),
                InSpecification<TEntity> membership => Membership(membership),
                StringSpecification<TEntity> text => StringMatch(text),
                NullSpecification<TEntity> empty => "(" + columns.Resolve(empty.MemberPath) + " IS NULL)",
                _ => throw new NotSupportedException("The specification contains an unsupported implementation."),
            };
        }

        private string Column(string path, SqlValueKind kind)
        {
            var column = columns.Resolve(path);
            return kind switch
            {
                SqlValueKind.DATE_TIME_OFFSET => translator.FormatInstant(column),
                SqlValueKind.STRING => translator.Collate(column),
                _ => column,
            };
        }

        private string Comparison(ComparisonSpecification<TEntity> node)
        {
            var column = Column(node.MemberPath, SqlValue.Kind(node.ValueType));
            if (node.Operation == ComparisonOperator.NOT_EQUAL)
            {
                return "(NOT " + Equal(column, SqlValue.Kind(node.ValueType), node.Value) + ")";
            }

            if (node.Operation == ComparisonOperator.EQUAL)
            {
                return Equal(column, SqlValue.Kind(node.ValueType), node.Value);
            }

            if (node.Value is null)
            {
                return "(1 = 0)";
            }

            var operation = node.Operation switch
            {
                ComparisonOperator.GREATER_THAN => ">",
                ComparisonOperator.GREATER_THAN_OR_EQUAL => ">=",
                ComparisonOperator.LESS_THAN => "<",
                ComparisonOperator.LESS_THAN_OR_EQUAL => "<=",
                _ => throw new NotSupportedException("Unknown comparison operation."),
            };
            return "(" + column + " IS NOT NULL AND " + column + " " + operation + " "
                + Parameter(SqlValue.Kind(node.ValueType), node.Value)
                + ")";
        }

        private string Equal(string column, SqlValueKind kind, object? value)
        {
            return value is null
                ? "(" + column + " IS NULL)"
                : "(" + column + " IS NOT NULL AND " + column + " = " + Parameter(kind, value) + ")";
        }

        private string Membership(InSpecification<TEntity> node)
        {
            var column = Column(node.MemberPath, SqlValue.Kind(node.ValueType));
            var hasNull = false;
            var parameters = new List<string>();
            foreach (var value in node.Values.Distinct())
            {
                if (value is null)
                {
                    hasNull = true;
                }
                else
                {
                    parameters.Add(Parameter(SqlValue.Kind(node.ValueType), value));
                }
            }

            if (parameters.Count == 0)
            {
                return hasNull ? "(" + column + " IS NULL)" : "(1 = 0)";
            }

            var nonNull = "(" + column + " IS NOT NULL AND " + column + " IN (" + string.Join(", ", parameters) + "))";
            return hasNull ? "(" + column + " IS NULL OR " + nonNull + ")" : nonNull;
        }

        private string StringMatch(StringSpecification<TEntity> node)
        {
            var column = Column(node.MemberPath, SqlValueKind.STRING);
            if (node.Value.Length == 0)
            {
                return "(" + column + " IS NOT NULL)";
            }

            var parameter = Parameter(SqlValueKind.STRING, node.Value);
            var predicate = node.Operation switch
            {
                StringOperator.CONTAINS => translator.Contains(column, parameter),
                StringOperator.STARTS_WITH => translator.StartsWith(column, parameter),
                StringOperator.ENDS_WITH => translator.EndsWith(column, parameter),
                _ => throw new InvalidOperationException("Unknown string operation."),
            };
            return "(" + column + " IS NOT NULL AND " + predicate + ")";
        }

        private string Parameter(SqlValueKind kind, object value)
        {
            if (Parameters.Count == maxParameters)
            {
                throw new InvalidOperationException("The specification exceeds the configured SQL parameter budget.");
            }

            SqlValue.Validate(value);
            value = kind switch
            {
                SqlValueKind.INTEGER => Convert.ToInt64(value, CultureInfo.InvariantCulture),
                SqlValueKind.ENUM => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
                SqlValueKind.DECIMAL when value is ulong unsigned => (decimal)unsigned,
                SqlValueKind.FLOATING_POINT => Convert.ToDouble(value, CultureInfo.InvariantCulture),
                SqlValueKind.DATE_TIME_OFFSET => ((DateTimeOffset)value).ToUniversalTime(),
                SqlValueKind.DATE_TIME => DateTime.SpecifyKind((DateTime)value, DateTimeKind.Unspecified),
                _ => value,
            };
            var name = prefix + Parameters.Count.ToString(CultureInfo.InvariantCulture);
            Parameters.Add(name, value);
            return kind == SqlValueKind.DATE_TIME_OFFSET ? translator.FormatInstant("@" + name) : "@" + name;
        }
    }
}