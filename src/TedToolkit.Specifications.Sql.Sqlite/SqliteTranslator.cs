// -----------------------------------------------------------------------
// <copyright file="SqliteTranslator.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Sql.Sqlite;

/// <summary>
/// Translates selections and specifications into SQLite SQL.
/// </summary>
public sealed class SqliteTranslator : SqlTranslator
{
    /// <inheritdoc />
    protected override int MaximumParameters
    {
        get
        {
            return 32766;
        }
    }

    /// <inheritdoc />
    protected override string Collate(string expression)
    {
        return $"({expression} COLLATE BINARY)";
    }

    /// <inheritdoc />
    protected override string FormatInstant(string expression)
    {
        return $"julianday({expression})";
    }

    /// <inheritdoc />
    protected override string Contains(string column, string parameter)
    {
        return $"instr({column}, {parameter}) > 0";
    }

    /// <inheritdoc />
    protected override string StartsWith(string column, string parameter)
    {
        return $"substr({column}, 1, length({parameter})) COLLATE BINARY = {parameter}";
    }

    /// <inheritdoc />
    protected override string EndsWith(string column, string parameter)
    {
        return $"substr({column}, length({column}) - length({parameter}) + 1) COLLATE BINARY = {parameter}";
    }
}