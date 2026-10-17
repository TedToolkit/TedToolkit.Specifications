// -----------------------------------------------------------------------
// <copyright file="ModelTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationGeneratorTests;

internal sealed class ModelTests
{
    /// <summary>
    /// Verifies public interface properties remain selectable through diamonds and private hidden declarations.
    /// </summary>
    [Test]
    public async Task Should_select_inherited_public_properties_without_private_interface_membersAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public interface IBase { int Score { get; } }
            public interface ILeft : IBase { private new int Score => 0; }
            public interface IRight : IBase { }
            public interface IEntity : ILeft, IRight { }
            public static class Usage
            {
                public static Specification<IEntity> Build() => Selection.For<IEntity>().Score > 0;
            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Generation.GeneratedTrees.Length).IsEqualTo(1);
    }

    /// <summary>
    /// Verifies nullable unsigned enum selections support SQL equality and membership for both underlying extremes.
    /// </summary>
    [Test]
    [Arguments("byte")]
    [Arguments("ulong")]
    public async Task Should_accept_unsigned_enum_selectionsAsync(string underlyingType)
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public enum Status :
            """ + underlyingType + """
             { VALUE = 1 }
            public sealed record Entity(Status? State);
            public static class Usage
            {
                public static Specification<Entity> Build()
                {
                    var state = Selection.For<Entity>().State;
                    return state.EqualTo(Status.VALUE) && state.In(new Status?[] { null, Status.VALUE });
                }
            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
    }
}