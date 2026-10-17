// -----------------------------------------------------------------------
// <copyright file="ContainsTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Tests.SelectionTests;

internal sealed class ContainsTests
{
    /// <summary>
    /// Verifies core string specifications preserve ordinary .NET values independently of SQL storage restrictions.
    /// </summary>
    [Test]
    [Arguments(0)]
    [Arguments(0xd800)]
    [Arguments(0xdc00)]
    public async Task Should_preserve_dotnet_strings_in_memoryAsync(int character)
    {
        var value = new string((char)character, 1);
        var specification = Selection.For<Entity>().Text.Contains(value);
        await Assert.That(specification.IsSatisfiedBy(new Entity(null, value))).IsTrue();
    }
}