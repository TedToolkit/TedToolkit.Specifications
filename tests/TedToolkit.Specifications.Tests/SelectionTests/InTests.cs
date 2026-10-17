// -----------------------------------------------------------------------
// <copyright file="InTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Tests.SelectionTests;

internal sealed class InTests
{
    /// <summary>
    /// Verifies that membership does not change after the caller mutates its input.
    /// </summary>
    [Test]
    public async Task Should_snapshot_membership_valuesAsync()
    {
        int?[] values = [null, 1, 1,];
        var rule = Selection.For<Entity>().Number.In(values);
        values[0] = 3;
        values[1] = 4;
        await Assert.That(rule.IsSatisfiedBy(new Entity(null, null))).IsTrue();
        await Assert.That(rule.IsSatisfiedBy(new Entity(1, null))).IsTrue();
        await Assert.That(rule.IsSatisfiedBy(new Entity(3, null))).IsFalse();
    }

    /// <summary>
    /// Verifies empty membership is always false and its complement is always true.
    /// </summary>
    [Test]
    public async Task Should_treat_empty_membership_as_falseAsync()
    {
        var rule = Selection.For<Entity>().Number.In([]);
        var entity = new Entity(null, null);
        await Assert.That(rule.IsSatisfiedBy(entity)).IsFalse();
        await Assert.That((!rule).IsSatisfiedBy(entity)).IsTrue();
    }
}