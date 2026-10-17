// -----------------------------------------------------------------------
// <copyright file="PublicApiTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationGeneratorTests;

internal sealed class PublicApiTests
{
    /// <summary>
    /// Verifies external consumers cannot read the member selection's implementation metadata.
    /// </summary>
    [Test]
    [Arguments("Parent")]
    [Arguments("Name")]
    public async Task Should_hide_member_metadata_from_consumersAsync(string member)
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public static class Usage
            {
                public static object Read(MemberSelection<string, string, int> selection) => selection.
            """ + member + "; }", includeSql: false).ConfigureAwait(false);
        var errors = probe.Compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
        await Assert.That(errors.Length).IsEqualTo(1);
        await Assert.That(errors[0].Id is "CS0122" or "CS1061").IsTrue();
        await Assert.That(probe.Diagnostics).IsEmpty();
    }

    /// <summary>
    /// Verifies generated accessors remain callable across assemblies and expose the abstract selection type.
    /// </summary>
    [Test]
    public async Task Should_generate_name_and_parent_selections_without_exposing_the_implementationAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public sealed record Entity(string? Name, Entity? Parent);
            public static class Usage
            {
                public static bool Evaluate(Entity candidate)
                {
                    var name = Selection.For<Entity>().Parent.Name;
                    return name.EqualTo("Ada").IsSatisfiedBy(candidate);
                }
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        var tree = probe.Compilation.SyntaxTrees.First();
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        var declaration = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
        var type = (INamedTypeSymbol)probe.Compilation.GetSemanticModel(tree).GetTypeInfo(declaration.Initializer!.Value).Type!;
        await Assert.That(type.OriginalDefinition.MetadataName).IsEqualTo("Selection`2");
        await Assert.That(type.ContainingAssembly.Name).IsEqualTo("TedToolkit.Specifications");
        var image = new MemoryStream();
        await using var lifetime = image.ConfigureAwait(false);
        await Assert.That(probe.Compilation.Emit(image).Success).IsTrue();
    }
}