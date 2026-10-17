// -----------------------------------------------------------------------
// <copyright file="DepthTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Sql.Tests.SqlSpecificationTests;

internal sealed class DepthTests
{
    /// <summary>
    /// Verifies large mixed specifications translate without recursion or a framework nesting limit.
    /// </summary>
    [Test]
    [Arguments(SqlDialect.SQLITE)]
    [Arguments(SqlDialect.POSTGRE_SQL)]
    public async Task Should_translate_deep_compositionsAsync(SqlDialect dialect)
    {
        var selection = Selection.For<int>();
        var rule = selection > 0;
        for (var index = 0; index < 100_000; index++)
        {
            rule = (index % 3) switch
            {
                0 => rule.And(Specification.True<int>()),
                1 => rule.Or(Specification.False<int>()),
                _ => !rule,
            };
        }

        var sql = DatabaseCases.Translate(rule, new SqlColumnMap<int>().Map(selection, "value"), dialect);
        await Assert.That(sql.Predicate.Length).IsGreaterThan(100_000);
        await Assert.That(sql.Parameters.Count).IsEqualTo(1);
        await Assert.That(sql.Parameters["spec0"]).IsEqualTo(0L);
        await Assert.That(rule.IsSatisfiedBy(1)).IsFalse();
        await Assert.That(rule.IsSatisfiedBy(-1)).IsTrue();
    }

    /// <summary>
    /// Verifies a 512-level mixed specification and its complement match memory on real SQLite.
    /// </summary>
    [Test]
    public async Task Should_match_deep_compositions_in_sqliteAsync()
    {
        await VerifyAsync(SqlDialect.SQLITE, 512).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies a 1000-level mixed specification and its complement match memory on real PostgreSQL.
    /// </summary>
    [Test]
    [RequiresPostgres]
    public async Task Should_match_deep_compositions_in_postgresqlAsync()
    {
        await VerifyAsync(SqlDialect.POSTGRE_SQL, 1000).ConfigureAwait(false);
    }

    private static async Task VerifyAsync(SqlDialect dialect, int depth)
    {
        var leaf = Selection.For<Row>().Number > 0;
        var rule = leaf;
        for (var index = 1; index < depth; index++)
        {
            rule = (index % 3) switch
            {
                0 => rule.And(leaf),
                1 => rule.Or(leaf),
                _ => !rule,
            };
        }

        var connection = await DatabaseCases.OpenAsync(dialect).ConfigureAwait(false);
        await using var connectionLifetime = connection.ConfigureAwait(false);
        await DatabaseCases.SeedAsync(connection, dialect).ConfigureAwait(false);
        (string, Specification<Row>)[] rules = [("deep mixed", rule), ("deep complement", !rule),];
        await DatabaseCases.VerifyAsync(connection, DatabaseCases.Rows, row => row.Id, rules, DatabaseCases.Columns(), dialect)
            .ConfigureAwait(false);
    }
}