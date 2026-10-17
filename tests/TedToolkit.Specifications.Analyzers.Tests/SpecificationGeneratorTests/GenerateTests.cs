// -----------------------------------------------------------------------
// <copyright file="GenerateTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationGeneratorTests;

internal sealed class GenerateTests
{
    /// <summary>
    /// Verifies emitted extension properties compile with inheritance, nullability, keywords, and ignored state.
    /// </summary>
    [Test]
    public async Task Should_compile_generated_fields_for_supported_entity_shapesAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            namespace Models;
            public class Parent { public virtual int Number { get; set; } }

            public sealed class Entity : Parent
            {
                public override int Number { get; set; }
                public string? @event { get; set; }
                 public object? State { get; set; }
            }
            public static class Usage
            {
                public static bool Evaluate(Entity entity)
                {
                    var fields = Selection.For<Entity>();
                    return ((fields.Number >= 3) & fields.@event.Contains("x")).IsSatisfiedBy(entity);
                }
            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        var image = new MemoryStream();

        await using var imageLifetime = image.ConfigureAwait(false);
        var emitted = probe.Compilation.Emit(image);
        await Assert.That(emitted.Success).IsTrue();
        await Assert.That(probe.Generation.GeneratedTrees.Length).IsEqualTo(2);
    }

    /// <summary>
    /// Verifies a consumer discovers public properties on a referenced entity without marker metadata.
    /// </summary>
    [Test]
    public async Task Should_allow_fields_from_another_assembly_without_runtime_marker_metadataAsync()
    {
        var model = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            namespace Models;
             public sealed record Entity(int Id);
            """).ConfigureAwait(false);
        var image = new MemoryStream();

        await using var imageLifetime = image.ConfigureAwait(false);
        await Assert.That(model.Compilation.Emit(image).Success).IsTrue();
        var consumer = await CompilationProbe.RunAsync("""
            using Models;
            using TedToolkit.Specifications;
            public static class Usage
            {
                public static bool Evaluate(Entity entity) => (Selection.For<Entity>().Id == 1).IsSatisfiedBy(entity);
            }
            """, additionalReferences: [MetadataReference.CreateFromImage(image.ToArray()),]).ConfigureAwait(false);
        await Assert.That(consumer.Errors).IsEmpty();
        await Assert.That(consumer.Diagnostics.Select(diagnostic => diagnostic.Id)).DoesNotContain("TSPEC006");
    }

    /// <summary>
    /// Verifies unsupported model and property forms receive targeted generation diagnostics.
    /// </summary>
    [Test]
    [Arguments("file class Entity { }", "Selection.For<Entity>()", "TSPEC001")]
    [Arguments(
        "public class Entity { public object? State { get; set; } }",
        "Selection.For<Entity>().State.EqualTo(new object())",
        "TSPEC002")]
    [Arguments(
        "public class Entity { public System.TimeSpan Duration { get; set; } }",
        "Selection.For<Entity>().Duration.EqualTo(default)",
        "TSPEC002")]
    [Arguments(
        "public class Entity { public int[]? Values { get; set; } }",
        "Selection.For<Entity>().Values.EqualTo(new int[0])",
        "TSPEC002")]
    [Arguments("public class Entity { public int Number { private get; set; } }", "Selection.For<Entity>().Number", "TSPEC003")]
    [Arguments(
        "public class Parent { public int Value { get; set; } } public class Entity : Parent { public new string? Value { get; set; } }",
        "Selection.For<Entity>()",
        "TSPEC009")]
    public async Task Should_diagnose_unsupported_modelsAsync(string declaration, string expression, string id)
    {
        var probe = await CompilationProbe.RunAsync(
            "using TedToolkit.Specifications; " + declaration + " public class Usage { public object Run() => " + expression + "; }")
            .ConfigureAwait(false);
        await Assert.That(probe.Diagnostics.Select(diagnostic => diagnostic.Id)).Contains(id);
        await Assert.That(probe.Diagnostics.Select(diagnostic => diagnostic.Id)).DoesNotContain("AD0001");
    }

    /// <summary>
    /// Verifies an older language version produces a clear diagnostic instead of invalid generated syntax.
    /// </summary>
    [Test]
    public async Task Should_require_csharp14Async()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public record Entity(int Id);
            public class Usage { public object Run() => Selection.For<Entity>(); }
            """, LanguageVersion.CSharp13).ConfigureAwait(false);
        await Assert.That(probe.Diagnostics.Select(diagnostic => diagnostic.Id)).Contains("TSPEC004");
        await Assert.That(probe.Generation.GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Verifies interface parameter declarations also trigger automatic extension-property discovery.
    /// </summary>
    [Test]
    public async Task Should_discover_properties_from_interface_parametersAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public record Entity(int Id);
            public static class Usage
            {
                public static Specification<Entity> Build(Selection<Entity, Entity> fields) => fields.Id > 1;
            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Generation.GeneratedTrees.Length).IsEqualTo(1);
    }
}