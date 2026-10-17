// -----------------------------------------------------------------------
// <copyright file="TranslateTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Sql.Tests.SqlSpecificationTests;

internal sealed class TranslateTests
{
    private static readonly string[] _expectedParameterNames = ["filter_0", "filter_1",];

    /// <summary>
    /// Verifies conditional composition retains the right predicate and its SQL parameters even with constant left operands.
    /// </summary>
    [Test]
    [Arguments(SqlDialect.SQLITE)]
    [Arguments(SqlDialect.POSTGRE_SQL)]
    public async Task Should_translate_both_conditional_operandsAsync(SqlDialect dialect)
    {
        var comparison = Selection.For<Row>().Number == 7;
        var and = Specification.False<Row>() && comparison;
        var or = Specification.True<Row>() || comparison;
        var conjunction = DatabaseCases.Translate(and, DatabaseCases.Columns(), dialect);
        var disjunction = DatabaseCases.Translate(or, DatabaseCases.Columns(), dialect);
        await Assert.That(conjunction.Parameters["spec0"]).IsEqualTo(7L);
        await Assert.That(disjunction.Parameters["spec0"]).IsEqualTo(7L);
        await Assert.That(conjunction.Predicate.Contains("AND", StringComparison.Ordinal)).IsTrue();
        await Assert.That(disjunction.Predicate.Contains("OR", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>
    /// Verifies the complete portable grammar against real SQLite, including a conflicting column collation.
    /// </summary>
    [Test]
    public async Task Should_match_memory_in_sqliteAsync()
    {
        var connection = await DatabaseCases.OpenAsync(SqlDialect.SQLITE).ConfigureAwait(false);

        await using var connectionLifetime = connection.ConfigureAwait(false);
        await DatabaseCases.SeedAsync(connection, SqlDialect.SQLITE).ConfigureAwait(false);
        await DatabaseCases.VerifyAsync(
            connection,
            DatabaseCases.Rows,
            row => row.Id,
            DatabaseCases.Rules(),
            DatabaseCases.Columns(),
            SqlDialect.SQLITE).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies the same grammar and null truth tables against a real PostgreSQL server.
    /// </summary>
    [Test]
    [RequiresPostgres]
    public async Task Should_match_memory_in_postgresqlAsync()
    {
        var connection = await DatabaseCases.OpenAsync(SqlDialect.POSTGRE_SQL).ConfigureAwait(false);

        await using var connectionLifetime = connection.ConfigureAwait(false);
        await DatabaseCases.SeedAsync(connection, SqlDialect.POSTGRE_SQL).ConfigureAwait(false);
        await DatabaseCases.VerifyAsync(
            connection,
            DatabaseCases.Rows,
            row => row.Id,
            DatabaseCases.Rules(),
            DatabaseCases.Columns(),
            SqlDialect.POSTGRE_SQL).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies values are bound as parameters and cannot alter the generated SQL.
    /// </summary>
    [Test]
    [Arguments(SqlDialect.SQLITE)]
    [Arguments(SqlDialect.POSTGRE_SQL)]
    public async Task Should_parameterize_untrusted_literalsAsync(SqlDialect dialect)
    {
        const string value = "' OR 1=1; DROP TABLE items; -- %_\\";
        var sql = DatabaseCases.Translate(Selection.For<Row>().Text.Contains(value), DatabaseCases.Columns(), dialect);
        await Assert.That(sql.Predicate.Contains(value, StringComparison.Ordinal)).IsFalse();
        await Assert.That(sql.Parameters["spec0"]).IsEqualTo(value);
        await Assert.That(() => ((IDictionary<string, object?>)sql.Parameters).Add("other", 1)).Throws<NotSupportedException>();
    }

    /// <summary>
    /// Verifies a scalar root requires an explicit column name because it has no member path.
    /// </summary>
    [Test]
    public async Task Should_require_a_column_for_scalar_rootsAsync()
    {
        await Assert.That(() => DatabaseCases.Translate(Selection.For<int>() == 1, SqlDialect.SQLITE))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies invalid parameter budgets and prefixes are rejected.
    /// </summary>
    [Test]
    public async Task Should_validate_translation_optionsAsync()
    {
        var rule = Selection.For<Row>().Number.In([1, 2, 3,]);
        await Assert.That(() => DatabaseCases.Translate(rule, DatabaseCases.Columns(), SqlDialect.SQLITE, "p;--"))
            .Throws<ArgumentException>();
        await Assert.That(() => DatabaseCases.Translate(rule, DatabaseCases.Columns(), SqlDialect.SQLITE, maxParameters: 0))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => DatabaseCases.Translate(rule, DatabaseCases.Columns(), SqlDialect.SQLITE, maxParameters: 2))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies membership copies values before translation and distinct prefixes avoid command collisions.
    /// </summary>
    [Test]
    public async Task Should_freeze_values_and_honor_parameter_prefixesAsync()
    {
        int?[] values = [1, 2, 2, null,];
        var rule = Selection.For<Row>().Number.In(values);
        values[0] = 99;
        var sql = DatabaseCases.Translate(rule, DatabaseCases.Columns(), SqlDialect.SQLITE, "filter_");
        await Assert.That(sql.Parameters.Keys.Order(StringComparer.Ordinal).SequenceEqual(_expectedParameterNames)).IsTrue();
        await Assert.That(sql.Parameters.Values.Contains(99L)).IsFalse();
    }

    /// <summary>
    /// Verifies iterative translation retains parentheses and left-to-right parameter binding through mixed operators.
    /// </summary>
    [Test]
    public async Task Should_preserve_parameter_order_in_mixed_compositionsAsync()
    {
        var number = Selection.For<Row>().Number;
        var rule = number.EqualTo(1).Not().Or(number.GreaterThan(2).And(number.LessThan(3)));
        var sql = DatabaseCases.Translate(rule, DatabaseCases.Columns(), SqlDialect.SQLITE);
        await Assert.That(sql.Predicate).IsEqualTo(
            "((NOT (\"number\" IS NOT NULL AND \"number\" = @spec0)) OR "
            + "((\"number\" IS NOT NULL AND \"number\" > @spec1) AND (\"number\" IS NOT NULL AND \"number\" < @spec2)))");
        await Assert.That(sql.Parameters.Count).IsEqualTo(3);
        await Assert.That(sql.Parameters["spec0"]).IsEqualTo(1L);
        await Assert.That(sql.Parameters["spec1"]).IsEqualTo(2L);
        await Assert.That(sql.Parameters["spec2"]).IsEqualTo(3L);
    }
}