// -----------------------------------------------------------------------
// <copyright file="DatabaseCases.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;

using Microsoft.Data.Sqlite;

using Npgsql;

namespace TedToolkit.Specifications.Sql.Tests;

internal static class DatabaseCases
{
    internal static readonly Guid Key = Guid.Parse("7ce318a7-2414-49a6-8330-007932ce5058");

    internal static readonly string?[] Texts =
        [null, "", "Ada", "ada", "A", "a", "a ", "a%b_c\\d", "' OR 1=1 --", "x\"y", "😀é", "é", "e\u0301", " ", "xx", "longer text",];

    internal static readonly Row[] Rows = Texts.Select((text, index) => new Row(
        index,
        index % 4 == 0 ? null : index - 8,
        text,
        index % 3 == 0 ? null : index % 2 == 0,
        GetKey(index),
        index % 3 == 0 ? null : (Status)(index % 3)))
        .Concat([
            new Row(Texts.Length, int.MinValue, "min", true, Key, Status.NEW),
            new Row(Texts.Length + 1, int.MaxValue, "max", false, null, Status.FAILED),
        ])
        .ToArray();

    private static Guid? GetKey(int index)
    {
        if (index % 3 == 0)
        {
            return null;
        }

        return index % 2 == 0 ? Key : Guid.Empty;
    }

    internal static SqlColumnMap<Row> Columns()
    {
        var fields = Selection.For<Row>();
        return new SqlColumnMap<Row>().Map(fields.Number, "number").Map(fields.Text, "text").Map(fields.Enabled, "enabled")
            .Map(fields.Key, "key")
            .Map(fields.Status, "status");
    }

    internal static List<(string Name, Specification<Row> Rule)> Rules()
    {
        var fields = Selection.For<Row>();
        var rules = new List<(string, Specification<Row>)>()
        {
            ("all", Specification.True<Row>()),
            ("none", Specification.False<Row>()),
        };
        foreach (var value in new int?[]
        {
            null,
            int.MinValue,
            -1,
            0,
            1,
            8,
            int.MaxValue,
        }

        )
        {
            rules.AddRange([
                ("number equal " + value, fields.Number == value),
                ("number not equal " + value, fields.Number != value),
                ("number greater " + value, fields.Number > value),
                ("number greater equal " + value, fields.Number >= value),
                ("number less " + value, fields.Number < value),
                ("number less equal " + value, fields.Number <= value),
            ]);
        }

        rules.AddRange([
            ("number in empty", fields.Number.In([])),
            ("number in null", fields.Number.In([null,])),
            ("number in values", fields.Number.In([1, 1, -1, int.MinValue,])),
            ("number in mixed", fields.Number.In([null, 1, -1,])),
            ("number null", fields.Number.IsNull()),
            ("number nonnull", fields.Number.IsNotNull()),
        ]);
        foreach (var text in Texts)
        {
            rules.Add(("text equal " + text, fields.Text == text));
            rules.Add(("text not equal " + text, fields.Text != text));
            rules.Add(("text membership " + text, fields.Text.In([text, null, "Ada",])));
            if (text is null)
            {
                continue;
            }

            rules.AddRange([
                ("contains " + text, fields.Text.Contains(text)),
                ("prefix " + text, fields.Text.StartsWith(text)),
                ("suffix " + text, fields.Text.EndsWith(text)),
            ]);
        }

        foreach (var value in new bool?[]
        {
            null,
            false,
            true,
        }

        )
        {
            rules.AddRange([("boolean equal " + value, fields.Enabled == value), ("boolean unequal " + value, fields
                .Enabled != value), ("boolean membership " + value, fields.Enabled.In([value, null,])),]);
        }

        rules.AddRange([
            ("key equality", fields.Key == Key),
            ("key null", fields.Key.IsNull()),
            ("key mixed membership", fields.Key.In([null, Key, Guid.Empty,])),
            ("enum equal", fields.Status == Status.DONE),
            ("enum mixed membership", fields.Status.In([null, Status.NEW, Status.FAILED,])),
        ]);
        var leaves = rules.ToArray();
        rules.AddRange(leaves.Select(item => ("not " + item.Item1, !item.Item2)));
        for (var index = 0; index < leaves.Length; index++)
        {
            var left = leaves[index];
            var right = leaves[((index * 7) + 3) % leaves.Length];
            rules.Add(("and " + index, left.Item2 && right.Item2));
            rules.Add(("or " + index, left.Item2 || right.Item2));
            rules.Add(("nested " + index, !(left.Item2 && !right.Item2) || fields.Text.IsNull()));
        }

        return rules;
    }

    internal static async Task<DbConnection> OpenAsync(SqlDialect dialect)
    {
        DbConnection connection = dialect == SqlDialect.SQLITE
            ? new SqliteConnection("Data Source=:memory:")
            : new NpgsqlConnection(Environment.GetEnvironmentVariable("SPECIFICATION_POSTGRES"));
        await connection.OpenAsync().ConfigureAwait(false);
        return connection;
    }

    internal static async Task SeedAsync(DbConnection connection, SqlDialect dialect)
    {
        var create = connection.CreateCommand();

        await using var createLifetime = create.ConfigureAwait(false);
        if (dialect == SqlDialect.SQLITE)
        {
            create.CommandText = "CREATE TABLE items (id INTEGER PRIMARY KEY, number INTEGER, text TEXT COLLATE NOCASE, "
                + "enabled INTEGER, key TEXT, status INTEGER)";
        }
        else
        {
            create.CommandText =
                "CREATE TEMP TABLE items (id integer PRIMARY KEY, number integer, text text, enabled boolean, key uuid, status integer)";
        }

        await create.ExecuteNonQueryAsync().ConfigureAwait(false);
        foreach (var row in Rows)
        {
            var insert = connection.CreateCommand();

            await using var insertLifetime = insert.ConfigureAwait(false);
            insert.CommandText = "INSERT INTO items VALUES (@id, @number, @text, @enabled, @key, @status)";
            Add(insert, "id", row.Id);
            Add(insert, "number", row.Number);
            Add(insert, "text", row.Text);
            Add(insert, "enabled", row.Enabled);
            Add(insert, "key", row.Key);
            Add(insert, "status", row.Status is null ? null : (int)row.Status);
            await insert.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }

    internal static void Add(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    internal static async Task VerifyAsync<TEntity>(
        DbConnection connection,
        IReadOnlyList<TEntity> rows,
        Func<TEntity, int> id,
        IReadOnlyList<(string Name, Specification<TEntity> Rule)> rules,
        SqlColumnMap<TEntity>? columns,
        SqlDialect dialect)
    {
        foreach (var (name, rule) in rules)
        {
            var translation = columns is null
                ? DatabaseCases.Translate(rule, dialect)
                : DatabaseCases.Translate(rule, columns, dialect);
            var query = CreateQuery(connection, translation);

            await using var queryLifetime = query.ConfigureAwait(false);
            var reader = await query.ExecuteReaderAsync().ConfigureAwait(false);

            await using var readerLifetime = reader.ConfigureAwait(false);
            var count = 0;
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var row = rows[count++];
                if (await reader.IsDBNullAsync(1).ConfigureAwait(false))
                {
                    throw new InvalidOperationException($"{dialect}: '{name}' produced SQL UNKNOWN.");
                }

                var actual = reader.GetBoolean(1);
                var expected = rule.IsSatisfiedBy(row);
                if (reader.GetInt32(0) != id(row) || actual != expected)
                {
                    throw new InvalidOperationException(
                        $"{dialect}: '{name}' differs for row {id(row)}. SQL={actual}, memory={expected}. {translation.Predicate}");
                }
            }

            if (count != rows.Count)
            {
                throw new InvalidOperationException("The query did not return every seeded row.");
            }
        }

        Console.WriteLine($"{dialect}: verified {rules.Count} rules across {rows.Count} rows, including Boolean results under NOT.");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100",
        Justification = "Tests execute the library's translated predicate with bound values; FROM clauses are test-owned constants.")]
    internal static DbCommand CreateQuery(DbConnection connection,
        (string Predicate, IReadOnlyDictionary<string, object?> Parameters) translation, string from = "items")
    {
        var query = connection.CreateCommand();
        query.CommandText = $"SELECT id, {translation.Predicate} FROM {from} ORDER BY id";
        foreach (var binding in translation.Parameters)
        {
            Add(query, binding.Key, binding.Value);
        }

        return query;
    }

    internal static SqlTranslator Translator(SqlDialect dialect)
    {
        return dialect switch
        {
            SqlDialect.SQLITE => new Sqlite.SqliteTranslator(),
            SqlDialect.POSTGRE_SQL => new PostgreSql.PostgreSqlTranslator(),
            _ => throw new ArgumentOutOfRangeException(nameof(dialect)),
        };
    }

    internal static (string Predicate, IReadOnlyDictionary<string, object?> Parameters) Translate<TCandidate>(
        Specification<TCandidate> rule, SqlColumnMap<TCandidate> columns, SqlDialect dialect,
        string parameterPrefix = "spec", int maxParameters = 1000)
    {
        var predicate = Translator(dialect).ToSql(rule, out var parameters, columns, parameterPrefix, maxParameters);
        return (predicate, parameters);
    }

    internal static (string Predicate, IReadOnlyDictionary<string, object?> Parameters) Translate<TCandidate>(
        Specification<TCandidate> rule, SqlDialect dialect, string parameterPrefix = "spec", int maxParameters = 1000)
    {
        return Translate(rule, new SqlColumnMap<TCandidate>(), dialect, parameterPrefix, maxParameters);
    }
}