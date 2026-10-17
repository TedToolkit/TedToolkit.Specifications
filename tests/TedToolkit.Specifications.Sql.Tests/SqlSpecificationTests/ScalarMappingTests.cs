// -----------------------------------------------------------------------
// <copyright file="ScalarMappingTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Sql.Tests.SqlSpecificationTests;

internal sealed class ScalarMappingTests
{
    /// <summary>
    /// Verifies normal decimal, floating-point, unsigned, date, time, and offset mappings in SQLite.
    /// </summary>
    [Test]
    public async Task Should_match_standard_sqlite_scalar_storageAsync()
    {
        var connection = await DatabaseCases.OpenAsync(SqlDialect.SQLITE).ConfigureAwait(false);

        await using var connectionLifetime = connection.ConfigureAwait(false);
        await TemporalCases.SeedAsync(connection, SqlDialect.SQLITE).ConfigureAwait(false);
        await DatabaseCases.VerifyAsync(
            connection,
            TemporalCases.Rows,
            row => row.Id,
            TemporalCases.Rules(),
            TemporalCases.Columns(),
            SqlDialect.SQLITE).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies native PostgreSQL scalar types and equivalent instants with different offsets.
    /// </summary>
    [Test]
    [RequiresPostgres]
    public async Task Should_match_native_postgresql_scalar_storageAsync()
    {
        var connection = await DatabaseCases.OpenAsync(SqlDialect.POSTGRE_SQL).ConfigureAwait(false);

        await using var connectionLifetime = connection.ConfigureAwait(false);
        await TemporalCases.SeedAsync(connection, SqlDialect.POSTGRE_SQL).ConfigureAwait(false);
        await DatabaseCases.VerifyAsync(
            connection,
            TemporalCases.Rows,
            row => row.Id,
            TemporalCases.Rules(),
            TemporalCases.Columns(),
            SqlDialect.POSTGRE_SQL).ConfigureAwait(false);
    }
}