// -----------------------------------------------------------------------
// <copyright file="SqlColumnMap.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Sql;

/// <summary>
/// Represents immutable overrides from generated property paths to database identifiers.
/// </summary>
/// <typeparam name = "TEntity">The mapped entity.</typeparam>
public sealed class SqlColumnMap<TEntity>
{
    /// <summary>
    /// Gets the shared immutable map without column overrides.
    /// </summary>
    internal static SqlColumnMap<TEntity> Empty { get; } = new();

    private readonly Dictionary<string, string> _columns;

    /// <summary>
    /// Creates an empty column map.
    /// </summary>
    public SqlColumnMap()
    {
        _columns = new(StringComparer.Ordinal);
    }

    private SqlColumnMap(Dictionary<string, string> columns)
    {
        _columns = columns;
    }

    /// <summary>
    /// Returns a new map containing one property-to-column binding.
    /// </summary>
    /// <param name = "field">A generated entity field.</param>
    /// <param name = "column">One identifier, not a SQL expression. Quotes are escaped.</param>
    /// <param name = "tableAlias">An optional, separately quoted table alias.</param>
    /// <returns>A new map; this instance remains unchanged.</returns>
    /// <exception cref = "ArgumentException">The identifier is empty, contains NUL, or the field is already mapped.</exception>
    /// <typeparam name = "TValue">The selected value type.</typeparam>
    public SqlColumnMap<TEntity> Map<TValue>(Selection<TEntity, TValue> field, string column, string? tableAlias = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        return Add(field.Path, column, tableAlias);
    }

    private SqlColumnMap<TEntity> Add(string path, string column, string? tableAlias)
    {
        ValidateIdentifier(column);
        if (tableAlias is not null)
        {
            ValidateIdentifier(tableAlias);
        }

        var copy = new Dictionary<string, string>(_columns, StringComparer.Ordinal);
        var identifier = tableAlias is null ? Quote(column) : Quote(tableAlias) + "." + Quote(column);
        if (!copy.TryAdd(path, identifier))
        {
            throw new ArgumentException($"Member path '{path}' already has a column mapping.", nameof(path));
        }

        return new(copy);
    }

    /// <summary>
    /// Resolves an override or quotes the complete member path as a column name.
    /// </summary>
    /// <param name = "path">The complete member path; empty for the root.</param>
    /// <returns>The quoted SQL column identifier with its optional table alias.</returns>
    /// <exception cref="InvalidOperationException">A scalar root has no configured SQL column binding.</exception>
    internal string Resolve(string path)
    {
        if (_columns.TryGetValue(path, out var column))
        {
            return column;
        }

        if (path.Length == 0)
        {
            throw new InvalidOperationException("A scalar root has no member name; map it to its SQL column explicitly.");
        }

        return Quote(path);
    }

    private static void ValidateIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        if (!identifier.Contains('\0', StringComparison.Ordinal))
        {
            return;
        }

        throw new ArgumentException("SQL identifiers cannot contain zero characters.", nameof(identifier));
    }

    private static string Quote(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}