// -----------------------------------------------------------------------
// <copyright file="PublicApiTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;

namespace TedToolkit.Specifications.Sql.Tests.SqlSpecificationTests;

internal sealed class PublicApiTests
{
    private static readonly Type[] _expectedSqlTypes = [typeof(SqlTranslator), typeof(SqlColumnMap<>),];

    /// <summary>
    /// Verifies SQL exports translation and mapping contracts while keeping writers and storage classification internal.
    /// </summary>
    [Test]
    public async Task Should_keep_sql_implementation_details_out_of_the_public_contractAsync()
    {
        await Assert.That(typeof(SqlTranslator).Assembly.ExportedTypes.ToArray()).IsEquivalentTo(_expectedSqlTypes);
        var sqlite = typeof(Sqlite.SqliteTranslator);
        var postgresql = typeof(PostgreSql.PostgreSqlTranslator);
        await Assert.That(sqlite.Assembly.ExportedTypes.Single()).IsEqualTo(sqlite);
        await Assert.That(postgresql.Assembly.ExportedTypes.Single()).IsEqualTo(postgresql);

        var entryPoints = typeof(SqlTranslator).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        await Assert.That(entryPoints.Length).IsEqualTo(2);
        await Assert.That(entryPoints.All(method => method.Name == "ToSql" && method.ReturnType == typeof(string))).IsTrue();
    }
}