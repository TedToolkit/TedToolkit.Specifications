// -----------------------------------------------------------------------
// <copyright file="DefaultColumnsTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Sql.Tests.SqlSpecificationTests;

internal sealed class DefaultColumnsTests
{
    /// <summary>
    /// Verifies default property paths and partial overrides against real SQLite columns.
    /// </summary>
    [Test]
    public async Task Should_use_default_columns_in_sqliteAsync()
    {
        await VerifyAsync(SqlDialect.SQLITE).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies default property paths and partial overrides against case-sensitive PostgreSQL identifiers.
    /// </summary>
    [Test]
    [RequiresPostgres]
    public async Task Should_use_default_columns_in_postgresqlAsync()
    {
        await VerifyAsync(SqlDialect.POSTGRE_SQL).ConfigureAwait(false);
    }

    private static async Task VerifyAsync(SqlDialect dialect)
    {
        var connection = await DatabaseCases.OpenAsync(dialect).ConfigureAwait(false);

        await using var connectionLifetime = connection.ConfigureAwait(false);
        var create = connection.CreateCommand();

        await using var createLifetime = create.ConfigureAwait(false);
        create.CommandText = "CREATE TEMP TABLE items (id integer, \"Amount\" integer, \"Customer.Name\" text, stored_amount integer)";
        await create.ExecuteNonQueryAsync().ConfigureAwait(false);
        DefaultRow[] rows = [new(0, 0, null), new(1, 1, new(null)), new(2, 2, new("Ada")), new(3, -1, new("Bob")),];
        foreach (var row in rows)
        {
            var insert = connection.CreateCommand();

            await using var insertLifetime = insert.ConfigureAwait(false);
            insert.CommandText = "INSERT INTO items VALUES (@id, @amount, @name, @amount)";
            DatabaseCases.Add(insert, "id", row.Id);
            DatabaseCases.Add(insert, "amount", row.Amount);
            DatabaseCases.Add(insert, "name", row.Customer?.Name);
            await insert.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        var selection = Selection.For<DefaultRow>();
        (string, Specification<DefaultRow>)[] rules =
        [
            ("amount", selection.Amount > 0),
            ("nested path", selection.Customer.Name == "Ada"),
            ("nested null", selection.Customer.Name.IsNull()),
            ("composition", selection.Amount > 0 && (selection.Customer.Name.Contains("Ada") || selection.Customer.Name.IsNull())),
        ];
        await DatabaseCases.VerifyAsync(connection, rows, row => row.Id, rules, null, dialect).ConfigureAwait(false);
        var map = new SqlColumnMap<DefaultRow>().Map(selection.Amount, "stored_amount");
        await DatabaseCases.VerifyAsync(connection, rows, row => row.Id, rules, map, dialect).ConfigureAwait(false);

        var translator = DatabaseCases.Translator(dialect);
        await VerifyProjectionAsync(translator.ToSql(selection.Amount), row => row.Amount).ConfigureAwait(false);
        await VerifyProjectionAsync(translator.ToSql(selection.Amount, map), row => row.Amount).ConfigureAwait(false);
        await VerifyProjectionAsync(translator.ToSql(selection.Customer.Name), row => row.Customer?.Name).ConfigureAwait(false);

        async Task VerifyProjectionAsync(string projection, Func<DefaultRow, object?> expected)
        {
            var query = DatabaseCases.CreateQuery(connection, (projection, new Dictionary<string, object?>()));
            await using var queryLifetime = query.ConfigureAwait(false);
            var reader = await query.ExecuteReaderAsync().ConfigureAwait(false);
            await using var readerLifetime = reader.ConfigureAwait(false);
            var index = 0;
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var value = expected(rows[index++]);
                var actual = await reader.IsDBNullAsync(1).ConfigureAwait(false)
                    ? null : Convert.ToString(reader.GetValue(1), System.Globalization.CultureInfo.InvariantCulture);
                var expectedText = value is null ? null : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                await Assert.That(actual).IsEqualTo(expectedText);
            }

            await Assert.That(index).IsEqualTo(rows.Length);
        }
    }
}