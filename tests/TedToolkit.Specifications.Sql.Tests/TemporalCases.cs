// -----------------------------------------------------------------------
// <copyright file="TemporalCases.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;

namespace TedToolkit.Specifications.Sql.Tests;

internal static class TemporalCases
{
    private static readonly DateTime _timestamp = new(2026, 10, 17, 8, 0, 0, DateTimeKind.Unspecified);

    private static readonly DateTimeOffset _instant = new(2026, 10, 17, 8, 0, 0, TimeSpan.FromHours(8));

    internal static readonly TemporalRow[] Rows =
    [
        new(0, null, null, null, null, null, null, null),
        new(1, 0m, new DateOnly(2026, 10, 17), new TimeOnly(8, 0), _timestamp, _instant, 0.25, 1),
        new(2, 1.25m, new DateOnly(2026, 10, 18), new TimeOnly(9, 0), _timestamp.AddDays(1), _instant.AddHours(1), 0.5, 2),
        new(3, -10.5m, new DateOnly(2025, 1, 1), TimeOnly.MinValue, _timestamp.AddDays(-1),
            _instant.ToOffset(TimeSpan.FromHours(-7)), -1.5, 0),
        new(4, 1.25m, new DateOnly(2026, 10, 17), new TimeOnly(8, 0), DateTime.SpecifyKind(_timestamp, DateTimeKind.Utc),
            _instant.ToUniversalTime(), 0.1, 256),
    ];

    internal static SqlColumnMap<TemporalRow> Columns()
    {
        var fields = Selection.For<TemporalRow>();
        return new SqlColumnMap<TemporalRow>().Map(fields.Amount, "amount").Map(fields.Date, "date").Map(fields.Time, "time").Map(
            fields.Timestamp,
            "timestamp").Map(fields.Instant, "instant").Map(fields.Measurement, "measurement").Map(fields.Quantity, "quantity");
    }

    internal static List<(string Name, Specification<TemporalRow> Rule)> Rules()
    {
        var fields = Selection.For<TemporalRow>();
        var rules = new List<(string Name, Specification<TemporalRow> Rule)>();
        Add(rules, fields.Amount, new decimal?[] { null, -10.5m, 0m, 1.25m, 10m, });
        Add(rules, fields.Date, new DateOnly?[] { null, new(2026, 10, 17), new(2026, 10, 18), });
        Add(rules, fields.Time, new TimeOnly?[] { null, new(8, 0), new(9, 0), });
        Add(rules, fields.Timestamp, new DateTime?[] { null, _timestamp, DateTime.SpecifyKind(_timestamp.AddDays(1), DateTimeKind.Utc), });
        Add(rules, fields.Instant,
            new DateTimeOffset?[] { null, _instant, _instant.ToOffset(TimeSpan.FromHours(-7)), _instant.AddHours(1), });
        Add(rules, fields.Measurement, new double?[] { null, -1.5, 0.1, 0.25, 0.5, });
        Add(rules, fields.Quantity, new ulong?[] { null, 0, 1, 2, 256, });
        var count = rules.Count;
        for (var index = 0; index < count; index++)
        {
            var (name, rule) = rules[index];
            rules.Add(("not " + name, !rule));
        }

        return rules;
    }

    private static void Add<TValue>(
        List<(string Name, Specification<TemporalRow> Rule)> rules,
        Selection<TemporalRow, TValue> field,
        TValue[] values)
    {
        foreach (var value in values)
        {
            rules.AddRange([
                (typeof(TValue).Name + " equal " + value, field.EqualTo(value)),
                (typeof(TValue).Name + " not equal " + value, field.NotEqualTo(value)),
                (typeof(TValue).Name + " greater " + value, field.GreaterThan(value)),
                (typeof(TValue).Name + " greater equal " + value, field.GreaterThanOrEqual(value)),
                (typeof(TValue).Name + " less " + value, field.LessThan(value)),
                (typeof(TValue).Name + " less equal " + value, field.LessThanOrEqual(value)),
            ]);
        }

        rules.Add((typeof(TValue).Name + " membership", field.In(values)));
    }

    internal static async Task SeedAsync(DbConnection connection, SqlDialect dialect)
    {
        var create = connection.CreateCommand();

        await using var createLifetime = create.ConfigureAwait(false);
        if (dialect == SqlDialect.SQLITE)
        {
            create.CommandText = "CREATE TABLE items (id INTEGER PRIMARY KEY, amount NUMERIC, date TEXT, time TEXT, "
                + "timestamp TEXT, instant TEXT, measurement REAL, quantity NUMERIC)";
        }
        else
        {
            create.CommandText = "CREATE TEMP TABLE items (id integer PRIMARY KEY, amount numeric, date date, time time, "
                + "timestamp timestamp, instant timestamptz, measurement double precision, quantity numeric(20))";
        }

        await create.ExecuteNonQueryAsync().ConfigureAwait(false);
        foreach (var row in Rows)
        {
            var insert = connection.CreateCommand();

            await using var insertLifetime = insert.ConfigureAwait(false);
            insert.CommandText = "INSERT INTO items VALUES (@id, @amount, @date, @time, @timestamp, @instant, @measurement, @quantity)";
            DatabaseCases.Add(insert, "id", row.Id);
            DatabaseCases.Add(insert, "amount", row.Amount);
            DatabaseCases.Add(insert, "date", row.Date);
            DatabaseCases.Add(insert, "time", row.Time);
            DatabaseCases.Add(
                insert,
                "timestamp",
                row.Timestamp is null ? null : DateTime.SpecifyKind(row.Timestamp.Value, DateTimeKind.Unspecified));
            DatabaseCases.Add(insert, "instant", row.Instant?.ToUniversalTime());
            DatabaseCases.Add(insert, "measurement", row.Measurement);
            DatabaseCases.Add(insert, "quantity", row.Quantity is null ? null : (decimal)row.Quantity);
            await insert.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }
}