// -----------------------------------------------------------------------
// <copyright file="PostgreSqlTranslator.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Sql.PostgreSql;

/// <summary>
/// Translates selections and specifications into PostgreSQL SQL.
/// </summary>
public sealed class PostgreSqlTranslator : SqlTranslator
{
    /// <inheritdoc />
    protected override int MaximumParameters
    {
        get
        {
            return 65535;
        }
    }

    /// <inheritdoc />
    protected override string Collate(string expression)
    {
        return $"({expression} COLLATE \"C\")";
    }

    /// <inheritdoc />
    protected override string Contains(string column, string parameter)
    {
        return $"strpos({column}, {parameter}) > 0";
    }

    /// <inheritdoc />
    protected override string StartsWith(string column, string parameter)
    {
        return $"left({column}, length({parameter})) COLLATE \"C\" = {parameter}";
    }

    /// <inheritdoc />
    protected override string EndsWith(string column, string parameter)
    {
        return $"right({column}, length({parameter})) COLLATE \"C\" = {parameter}";
    }
}