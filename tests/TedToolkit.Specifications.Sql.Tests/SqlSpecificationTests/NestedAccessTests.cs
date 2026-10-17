// -----------------------------------------------------------------------
// <copyright file="NestedAccessTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;

namespace TedToolkit.Specifications.Sql.Tests.SqlSpecificationTests;

internal sealed class NestedAccessTests
{
    /// <summary>
    /// Verifies nested member paths and missing parents produce identical results in memory and SQLite.
    /// </summary>
    [Test]
    public async Task Should_match_nested_access_in_sqliteAsync()
    {
        await VerifyAsync(SqlDialect.SQLITE).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies nested member paths retain two-valued null semantics on PostgreSQL.
    /// </summary>
    [Test]
    [RequiresPostgres]
    public async Task Should_match_nested_access_in_postgresqlAsync()
    {
        await VerifyAsync(SqlDialect.POSTGRE_SQL).ConfigureAwait(false);
    }

    private static async Task VerifyAsync(SqlDialect dialect)
    {
        NestedRow[] rows =
        [
            new(0, null, null),
            new(1, new(null, 0, false), null),
            new(2, new(new(null), 1, true), new(new("Ada"), 0, false)),
            new(3, new(new(""), -1, true), new(null, 0, false)),
            new(4, new(new("Ada"), 2, false), new(new("other"), 0, true)),
            new(5, new(new("ada"), 0, true), new(new(null), 1, false)),
            new(6, new(new("a%b_c😀"), 8, true), null),
        ];
        var root = Selection.For<NestedRow>();
        var customer = root.Customer;
        var name = customer.Address.Name;
        var wrapped = (root.Customer ?? root.Alternate)!.Address.Name;
        var columns = new SqlColumnMap<NestedRow>().Map(name, "name").Map(customer.Score, "score").Map(customer.Active, "active")
            .Map(customer, "present")
            .Map(root.Alternate.Address.Name, "other_name");
        var leaves = new List<(string Name, Specification<NestedRow> Rule)>()
        {
            ("missing parent", customer.IsNull()),
            ("present parent", customer.IsNotNull()),
            ("null nested name", name.EqualTo(null)),
            ("not null nested name", name.NotEqualTo(null)),
            ("name equality", name == "Ada"),
            ("name inequality", name != "Ada"),
            ("empty text", name.Contains("")),
            ("ordinal prefix", name.StartsWith("a")),
            ("literal wildcard", name.Contains("%b_")),
            ("unicode suffix", name.EndsWith("😀")),
            ("membership with null", name.In([null, "Ada", "",])),
            ("default value equality", customer.Score == 0),
            ("default value inequality", customer.Score != 0),
            ("ordering", customer.Score > 0),
            ("missing non-nullable value", customer.Score.IsNull()),
            ("membership without null", customer.Score.In([0, 1,])),
            ("boolean value", customer.Active.EqualTo(true)),
            ("different path of same member type", root.Alternate.Address.Name == "Ada"),
            ("wrapped path equality", wrapped == "Ada"),
            ("mixed wrapped operators", (wrapped != "Ada").Not() && (customer.Score > 0 || customer.Active.EqualTo(true))),
        };
        var rules = new List<(string Name, Specification<NestedRow> Rule)>(leaves);
        foreach (var leaf in leaves)
        {
            rules.Add(("NOT " + leaf.Name, !leaf.Rule));
        }

        for (var index = 0; index < leaves.Count; index++)
        {
            var left = leaves[index].Rule;
            var right = leaves[(index + 5) % leaves.Count].Rule;
            rules.Add(("AND " + index, left & right));
            rules.Add(("OR " + index, left | right));
            rules.Add(("nested NOT " + index, !(left | !right)));
        }

        var connection = await DatabaseCases.OpenAsync(dialect).ConfigureAwait(false);

        await using var connectionLifetime = connection.ConfigureAwait(false);
        var create = connection.CreateCommand();
        await using (create.ConfigureAwait(false))
        {
            if (dialect == SqlDialect.SQLITE)
            {
                create.CommandText = "CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT COLLATE NOCASE, "
                    + "score INTEGER, active INTEGER, present INTEGER, other_name TEXT)";
            }
            else
            {
                create.CommandText = "CREATE TEMP TABLE items (id integer PRIMARY KEY, name text, "
                    + "score integer, active boolean, present integer, other_name text)";
            }

            await create.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        foreach (var row in rows)
        {
            var insert = connection.CreateCommand();

            await using var insertLifetime = insert.ConfigureAwait(false);
            insert.CommandText = "INSERT INTO items VALUES (@id, @name, @score, @active, @present, @other_name)";
            DatabaseCases.Add(insert, "id", row.Id);
            DatabaseCases.Add(insert, "name", row.Customer?.Address?.Name);
            DatabaseCases.Add(insert, "score", row.Customer?.Score);
            DatabaseCases.Add(insert, "active", row.Customer?.Active);
            DatabaseCases.Add(insert, "present", row.Customer is null ? null : 1);
            DatabaseCases.Add(insert, "other_name", row.Alternate?.Address?.Name);
            await insert.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        await DatabaseCases.VerifyAsync(connection, rows, row => row.Id, rules, columns, dialect).ConfigureAwait(false);
    }
}