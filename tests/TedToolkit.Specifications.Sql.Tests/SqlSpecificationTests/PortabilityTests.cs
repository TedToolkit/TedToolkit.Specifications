// -----------------------------------------------------------------------
// <copyright file="PortabilityTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Sql.Tests.SqlSpecificationTests;

internal sealed class PortabilityTests
{
    /// <summary>
    /// Verifies sanitized inline text operands remain usable in real SQLite queries.
    /// </summary>
    [Test]
    public async Task Should_translate_computed_operands_in_sqliteAsync()
    {
        await VerifyComputedOperandsAsync(SqlDialect.SQLITE).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies sanitized inline text operands retain in-memory semantics in real PostgreSQL queries.
    /// </summary>
    [Test]
    [RequiresPostgres]
    public async Task Should_translate_computed_operands_in_postgresqlAsync()
    {
        await VerifyComputedOperandsAsync(SqlDialect.POSTGRE_SQL).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies SQL-only text restrictions are enforced at translation, after ordinary in-memory evaluation remains available.
    /// </summary>
    [Test]
    [Arguments(0)]
    [Arguments(0xd800)]
    [Arguments(0xdc00)]
    public async Task Should_reject_unrepresentable_text_only_when_translatingAsync(int character)
    {
        var value = new string((char)character, 1);
        var selection = Selection.For<string>();
        var specification = selection.Contains(value);
        await Assert.That(specification.IsSatisfiedBy(value)).IsTrue();
        foreach (var dialect in new[]
        {
            SqlDialect.SQLITE,
            SqlDialect.POSTGRE_SQL,
        }

        )
        {
            await Assert.That(() => DatabaseCases.Translate(specification, new SqlColumnMap<string>().Map(selection, "value"), dialect))
                .Throws<ArgumentException>();
        }
    }

    /// <summary>
    /// Verifies non-finite values retain .NET equality but are rejected by portable SQL translation.
    /// </summary>
    [Test]
    [Arguments(double.NaN)]
    [Arguments(double.PositiveInfinity)]
    [Arguments(double.NegativeInfinity)]
    public async Task Should_keep_numeric_storage_validation_in_sqlAsync(double value)
    {
        var selection = Selection.For<double>();
        var specification = selection.EqualTo(value);
        await Assert.That(specification.IsSatisfiedBy(value)).IsTrue();
        foreach (var dialect in new[]
        {
            SqlDialect.SQLITE,
            SqlDialect.POSTGRE_SQL,
        }

        )
        {
            await Assert.That(() => DatabaseCases.Translate(specification, new SqlColumnMap<double>().Map(selection, "value"), dialect))
                .Throws<ArgumentException>();
        }
    }

    /// <summary>
    /// Verifies core value-object equality can be used in memory without inventing a SQL encoding.
    /// </summary>
    [Test]
    public async Task Should_reject_unsupported_sql_types_without_restricting_core_equalityAsync()
    {
        var selection = Selection.For<ValueKey>();
        var value = new ValueKey(42);
        var specification = Equal(selection, value);
        await Assert.That(specification.IsSatisfiedBy(value)).IsTrue();
        await Assert.That(
            () => DatabaseCases.Translate(specification, new SqlColumnMap<ValueKey>().Map(selection, "value"), SqlDialect.SQLITE))
            .Throws<NotSupportedException>();
        static Specification<T> Equal<T>(Selection<T, T> input, T expected)
        {
            return input.EqualTo(expected);
        }
    }

    private static async Task VerifyComputedOperandsAsync(SqlDialect dialect)
    {
        var fields = Selection.For<Row>();
        var equal = fields.Text.EqualTo("\0".Replace("\0", "", StringComparison.Ordinal));
        var membership = fields.Text.In(["\0".Replace("\0", "", StringComparison.Ordinal), "Ada",]);
        var conversion = fields.Text.EqualTo((string)(CleanText)"\0");
        var connection = await DatabaseCases.OpenAsync(dialect).ConfigureAwait(false);

        await using var lifetime = connection.ConfigureAwait(false);
        await DatabaseCases.SeedAsync(connection, dialect).ConfigureAwait(false);
        await DatabaseCases.VerifyAsync(connection, DatabaseCases.Rows, row => row.Id,
            [("sanitized equality", equal), ("negated sanitized equality", !equal), ("sanitized membership", membership),
                ("sanitized conversion", conversion), ("negated sanitized conversion", !conversion),],
            DatabaseCases.Columns(), dialect).ConfigureAwait(false);
    }
}