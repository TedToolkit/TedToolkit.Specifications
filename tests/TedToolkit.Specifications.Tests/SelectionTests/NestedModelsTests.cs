// -----------------------------------------------------------------------
// <copyright file="NestedModelsTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Tests.SelectionTests;

internal sealed class NestedModelsTests
{
    /// <summary>
    /// Verifies conditional selection, suppression, and conditional member access execute with missing parents under Native AOT.
    /// </summary>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Should_evaluate_wrapped_nested_propertiesAsync(bool choose)
    {
        var root = Selection.For<ChainRoot>();
        var child = choose ? root.Child! : root.Other!;
        var name = child.Value?.Text;
        var specification = (name! == "Ada") && child.Value.Number >= 1;
        await Assert.That(specification.IsSatisfiedBy(new(new(new(1, "Ada")), new(new(0, "Bob"))))).IsEqualTo(choose);
        await Assert.That(specification.IsSatisfiedBy(new(null, null))).IsFalse();
        await Assert.That(specification.IsSatisfiedBy(new(new(null), new(null)))).IsFalse();
    }

    /// <summary>
    /// Verifies native integers and dynamic metadata need no reflection or runtime binder in generated getters.
    /// </summary>
    [Test]
    public async Task Should_evaluate_special_type_properties_without_dynamic_bindingAsync()
    {
        var fields = Selection.For<SourceValues>();
        var specification = fields.Value.EqualTo("payload") && fields.Signed.EqualTo(1) && fields.Unsigned.EqualTo(2);
        await Assert.That(specification.IsSatisfiedBy(new("payload", 1, 2, []))).IsTrue();
        await Assert.That(specification.IsSatisfiedBy(new("other", 1, 2, []))).IsFalse();
        await Assert.That(specification.IsSatisfiedBy(new("payload", 0, 2, []))).IsFalse();
    }

    /// <summary>
    /// Verifies nested generic member access and candidate typing also execute on each runtime and Native AOT.
    /// </summary>
    [Test]
    public async Task Should_evaluate_nested_generic_model_propertiesAsync()
    {
        var item = Selection.For<GenericContainer<int>.Item<string>>();
        var specification = item.Value > 0 && item.Name.StartsWith("A");
        await Assert.That(specification.IsSatisfiedBy(new(7, "Ada"))).IsTrue();
        await Assert.That(specification.IsSatisfiedBy(new(0, "Ada"))).IsFalse();
        await Assert.That(specification.IsSatisfiedBy(new(7, "Bob"))).IsFalse();
    }
}