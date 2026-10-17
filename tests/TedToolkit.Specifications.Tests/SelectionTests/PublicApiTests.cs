// -----------------------------------------------------------------------
// <copyright file="PublicApiTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;

namespace TedToolkit.Specifications.Tests.SelectionTests;

internal sealed class PublicApiTests
{
    private static readonly Type[] _expectedTypes =
    [
        typeof(Selection), typeof(Selection<,>), typeof(Specification), typeof(Specification<>),
        typeof(MemberSelection<,,>), typeof(ComparisonOperator),
    ];

    /// <summary>
    /// Guards the runtime boundary against leaking concrete predicate implementations and construction metadata.
    /// </summary>
    [Test]
    public async Task Should_expose_only_the_selection_and_specification_contractsAsync()
    {
        await Assert.That(typeof(Selection).Assembly.ExportedTypes.ToArray()).IsEquivalentTo(_expectedTypes);
        var member = typeof(MemberSelection<,,>);
        await Assert.That(member.IsPublic && member.IsSealed).IsTrue();
        await Assert.That(member.GetConstructors().Length).IsEqualTo(1);
        await Assert.That(member.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)).IsEmpty();
        await Assert.That(member.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)).IsEmpty();
        await Assert.That(typeof(Specification<>).GetProperty("IsTrue")).IsNull();
    }
}