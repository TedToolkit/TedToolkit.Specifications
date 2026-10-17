// -----------------------------------------------------------------------
// <copyright file="ScalarTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;

namespace TedToolkit.Specifications.Sql.Tests.SqlSpecificationTests;

internal sealed class ScalarTests
{
    /// <summary>
    /// Verifies scalar interface specifications translate against a mapped SQLite value column.
    /// </summary>
    [Test]
    public async Task Should_translate_scalar_specs_in_sqliteAsync()
    {
        await VerifyAsync(SqlDialect.SQLITE).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies scalar interface specifications translate against a mapped PostgreSQL value column.
    /// </summary>
    [Test]
    [RequiresPostgres]
    public async Task Should_translate_scalar_specs_in_postgresqlAsync()
    {
        await VerifyAsync(SqlDialect.POSTGRE_SQL).ConfigureAwait(false);
    }

    private static async Task VerifyAsync(SqlDialect dialect)
    {
        var connection = await DatabaseCases.OpenAsync(dialect).ConfigureAwait(false);

        await using var connectionLifetime = connection.ConfigureAwait(false);
        await DatabaseCases.SeedAsync(connection, dialect).ConfigureAwait(false);
        var number = Selection.For<int?>();
        Specification<int?>[] numbers = [number > 1, number.GreaterThan(1).Not(), number.In([null, 1, -1,]), number.IsNull(), number
            .EqualTo(null),];
        var numericColumns = new SqlColumnMap<int?>().Map(number, "number");
        foreach (var rule in numbers)
        {
            await CheckAsync(connection, dialect, rule, numericColumns, DatabaseCases.Rows.Select(row => row.Number).ToArray())
                .ConfigureAwait(false);
        }

        var text = Selection.For<string?>();
        Specification<string?>[] strings = [text.Contains("a"), !text.Contains("a"), text.IsNull(), text.EqualTo(null), text.StartsWith(
            ""),];
        var stringColumns = new SqlColumnMap<string?>().Map(text, "text");
        foreach (var rule in strings)
        {
            await CheckAsync(connection, dialect, rule, stringColumns, DatabaseCases.Rows.Select(row => row.Text).ToArray()).ConfigureAwait(
                false);
        }
    }

    private static async Task CheckAsync<T>(
        DbConnection connection,
        SqlDialect dialect,
        Specification<T> rule,
        SqlColumnMap<T> columns,
        IReadOnlyList<T> values)
    {
        var sql = DatabaseCases.Translate(rule, columns, dialect);
        var command = DatabaseCases.CreateQuery(connection, sql);

        await using var commandLifetime = command.ConfigureAwait(false);
        var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

        await using var readerLifetime = reader.ConfigureAwait(false);
        var index = 0;
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (await reader.IsDBNullAsync(1).ConfigureAwait(false) || reader.GetBoolean(1) != rule.IsSatisfiedBy(values[index++]))
            {
                throw new InvalidOperationException("Scalar SQL and in-memory results differ.");
            }
        }

        if (index == values.Count)
        {
            return;
        }

        throw new InvalidOperationException("Scalar query row count differs.");
    }
}