// -----------------------------------------------------------------------
// <copyright file="CompilationProbe.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Runtime.Loader;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TedToolkit.Specifications.Analyzers.Tests;

internal static class CompilationProbe
{
    private static readonly MetadataReference[] _references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Append(typeof(Specification).Assembly.Location)
        .Where(path => Path.GetFileNameWithoutExtension(path) != "TedToolkit.Specifications.Sql")
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(path => MetadataReference.CreateFromFile(path))
        .ToArray();

    internal static async Task<ProbeResult> RunAsync(
        string source,
        LanguageVersion language = LanguageVersion.CSharp14,
        IEnumerable<MetadataReference>? additionalReferences = null,
        bool includeSql = true)
    {
        var parse = new CSharpParseOptions(language);
        var references = _references.Concat(additionalReferences ?? []);
        if (includeSql)
        {
            references = references.Append(MetadataReference.CreateFromFile(typeof(Sql.SqlTranslator).Assembly.Location));
        }

        var compilation = CSharpCompilation.Create(
            "Probe_" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source, parse),],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new SpecificationGenerator().AsSourceGenerator(),], parseOptions: parse);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var generated, out var generationDiagnostics);
        var analysis = await generated.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new SpecificationAnalyzer()))
            .GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
        return new(generated, generationDiagnostics.Concat(analysis).ToArray(), driver.GetRunResult());
    }

    internal sealed record ProbeResult(Compilation Compilation, Diagnostic[] Diagnostics, GeneratorDriverRunResult Generation)
    {
        internal string[] Errors { get; } = Compilation.GetDiagnostics()
            .Concat(Diagnostics)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .ToArray();

        internal bool Evaluate()
        {
            using var image = new MemoryStream();
            var emitted = Compilation.Emit(image);
            if (!emitted.Success)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
            }

            var context = new AssemblyLoadContext(Compilation.AssemblyName, isCollectible: true);
            try
            {
                image.Position = 0;
                var assembly = context.LoadFromStream(image);
                var run = assembly.GetType("Usage")!.GetMethod("Run")!.CreateDelegate<Func<bool>>();
                return run();
            }
            finally
            {
                context.Unload();
            }
        }
    }
}