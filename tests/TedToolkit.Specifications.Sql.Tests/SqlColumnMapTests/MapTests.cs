// -----------------------------------------------------------------------
// <copyright file="MapTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Data.Sqlite;

namespace TedToolkit.Specifications.Sql.Tests.SqlColumnMapTests;

internal sealed class MapTests
{
    /// <summary>
    /// Verifies quoted identifiers are literal names even when they contain hostile-looking SQL.
    /// </summary>
    [Test]
    public async Task Should_quote_columns_and_aliases_as_identifiersAsync()
    {
        const string column = "x\"; DROP TABLE items; --";
        const string alias = "table\"alias";
        var fields = Selection.For<Row>();
        var map = new SqlColumnMap<Row>().Map(fields.Number, column, alias);
        var sql = DatabaseCases.Translate(fields.Number == 7, map, SqlDialect.SQLITE);
        var connection = new SqliteConnection("Data Source=:memory:");

        await using var connectionLifetime = connection.ConfigureAwait(false);
        await connection.OpenAsync().ConfigureAwait(false);
        var query = DatabaseCases.CreateQuery(connection, sql,
                    "(SELECT 0 AS id, 7 AS \"x\"\"; DROP TABLE items; --\") AS \"table\"\"alias\"");

        await using var queryLifetime = query.ConfigureAwait(false);
        var reader = await query.ExecuteReaderAsync().ConfigureAwait(false);

        await using var readerLifetime = reader.ConfigureAwait(false);
        await Assert.That(await reader.ReadAsync().ConfigureAwait(false)).IsTrue();
        await Assert.That(reader.GetBoolean(1)).IsTrue();
    }

    /// <summary>
    /// Verifies adding bindings does not mutate a map already used elsewhere.
    /// </summary>
    [Test]
    public async Task Should_keep_maps_immutable_and_reject_duplicate_bindingsAsync()
    {
        var fields = Selection.For<Row>();
        var empty = new SqlColumnMap<Row>();
        var mapped = empty.Map(fields.Number, "number");
        var original = DatabaseCases.Translate(fields.Number == 1, empty, SqlDialect.SQLITE);
        var modified = DatabaseCases.Translate(fields.Number == 1, mapped, SqlDialect.SQLITE);
        await Assert.That(original.Predicate.Contains("\"Number\"", StringComparison.Ordinal)).IsTrue();
        await Assert.That(modified.Predicate.Contains("\"number\"", StringComparison.Ordinal)).IsTrue();
        await Assert.That(() => mapped.Map(fields.Number, "other")).Throws<ArgumentException>();
        await Assert.That(() => empty.Map(fields.Number, "")).Throws<ArgumentException>();
    }
}