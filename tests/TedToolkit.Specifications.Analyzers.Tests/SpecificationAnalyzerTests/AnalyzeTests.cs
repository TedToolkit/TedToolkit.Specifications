// -----------------------------------------------------------------------
// <copyright file="AnalyzeTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationAnalyzerTests;

internal sealed class AnalyzeTests
{
    /// <summary>
    /// Verifies every Boolean control-flow context requires explicit candidate evaluation.
    /// </summary>
    [Test]
    [Arguments("if (rule) { }")]
    [Arguments("if (rule && rule) { }")]
    [Arguments("if (!rule) { }")]
    [Arguments("while (rule) { break; }")]
    [Arguments("do { } while (rule);")]
    [Arguments("for (; rule;) { break; }")]
    [Arguments("var value = rule ? 1 : 0;")]
    [Arguments("try { } catch (System.Exception) when (rule) { }")]
    [Arguments("switch (1) { case 1 when rule: break; }")]
    [Arguments("var value = 1 switch { _ when rule => 1, _ => 0 };")]
    public async Task Should_require_a_candidate_in_boolean_conditionsAsync(string statement)
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public class Usage
            {
                public void Run()
                {
                    var rule = Selection.For<int>() > 0;
            """ + statement + " } }", includeSql: false).ConfigureAwait(false);
        await Assert.That(
            probe.Compilation.GetDiagnostics().Where(item => item.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)).IsEmpty();
        await Assert.That(probe.Diagnostics.Single().Id).IsEqualTo("TSPEC017");
    }

    /// <summary>
    /// Verifies conditional composition and explicitly evaluated conditions remain valid.
    /// </summary>
    [Test]
    public async Task Should_allow_conditional_composition_with_explicit_evaluationAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public class Usage
            {
                public int Run(int candidate)
                {
                    var value = Selection.For<int>();
                    Specification<int> rule = value > 0 && value < 10 || value == 20;
                    if (rule.IsSatisfiedBy(candidate)) return 1;
                    return rule.IsSatisfiedBy(candidate) ? 2 : 0;
                }
            }
            """, includeSql: false).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
    }

    /// <summary>
    /// Verifies unsafe field construction, misleading object operations, and invalid literals are diagnosed.
    /// </summary>
    [Test]
    [Arguments("var field = new MemberSelection<Entity, Entity, int>(fields, \"Id\", e => e.Id + 1);", "TSPEC005")]
    [Arguments("MemberSelection<Entity, Entity, int> field = new(fields, \"Id\", e => e.Id + 1);", "TSPEC005")]
    [Arguments("var ignored = fields.Id.Equals(1);", "TSPEC008")]
    [Arguments("var ignored = object.Equals(fields.Id, 1);", "TSPEC008")]
    [Arguments("var ignored = Selection.For<int>().IsNull();", "TSPEC007")]
    [Arguments("var rule = fields.Name.Contains(\"\\0\");", "TSPEC010")]
    [Arguments("var rule = fields.Name == \"\\ud800\";", "TSPEC010")]
    [Arguments("var rule = fields.Name.In(new[] { \"\\0\" });", "TSPEC010")]
    [Arguments("var rule = fields.Id.Contains(\"x\");", "TSPEC013")]
    [Arguments("var rule = fields.Name.GreaterThan(\"x\");", "TSPEC013")]
    [Arguments(
        "var rule = fields.Name.Compare(\"x\", ComparisonOperator." + nameof(ComparisonOperator.GREATER_THAN) + ");",
        "TSPEC013")]
    [Arguments("var rule = fields.Measurement.EqualTo(double.NaN);", "TSPEC014")]
    [Arguments(
        "var rule = fields.Measurement.Compare(double.NaN, ComparisonOperator." + nameof(ComparisonOperator.EQUAL) + ");",
        "TSPEC014")]
    [Arguments("var rule = fields.Measurement.In(new[] { double.PositiveInfinity });", "TSPEC014")]
    [Arguments("var rule = fields.Name.Contains(null!);", "TSPEC015")]
    [Arguments("var rule = fields.Name > \"x\";", "TSPEC013")]
    [Arguments("var rule = Selection.For<int>().IsSatisfiedBy(2);", "TSPEC006")]
    [Arguments("var rule = (object)Selection.For<string?>() == null;", "TSPEC016")]
    [Arguments("var rule = Selection.For<string?>() is null;", "TSPEC016")]
    [Arguments("var rule = Selection.For<string?>() is not null;", "TSPEC016")]
    public async Task Should_report_unsafe_usageAsync(string statement, string id)
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
             public sealed record Entity(int Id, string? Name, double Measurement);
            public static class Usage
            {
                public static void Run()
                {
                    var fields = Selection.For<Entity>();
            """ + statement + " } }").ConfigureAwait(false);
        await Assert.That(probe.Diagnostics.Select(diagnostic => diagnostic.Id)).Contains(id);
        await Assert.That(probe.Diagnostics.Select(diagnostic => diagnostic.Id)).DoesNotContain("AD0001");
    }

    /// <summary>
    /// Verifies ordinary entities receive generated extension properties without marker attributes.
    /// </summary>
    [Test]
    public async Task Should_discover_entities_without_attributesAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public record Entity(int Id);
            public class Usage { public Specification<Entity> Run() => Selection.For<Entity>().Id > 1; }
            """).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
    }

    /// <summary>
    /// Verifies the Analyzer rejects implementations whose behavior has no SQL translation.
    /// </summary>
    [Test]
    public async Task Should_prevent_external_specification_implementationsAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public sealed class UnsafeRule : Specification<int>
            {

            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Diagnostics.Select(diagnostic => diagnostic.Id)).Contains("TSPEC011");
        await Assert.That(
            probe.Compilation.GetDiagnostics().Any(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error))
            .IsTrue();
    }

    /// <summary>
    /// Verifies computed entity properties are surfaced before the user maps them to SQL.
    /// </summary>
    [Test]
    public async Task Should_report_computed_gettersAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public class Entity { public int Number => 42; }
            public class Usage { public object Run() => Selection.For<Entity>().Number; }
            """).ConfigureAwait(false);
        await Assert.That(probe.Diagnostics.Select(diagnostic => diagnostic.Id)).Contains("TSPEC012");
    }

    /// <summary>
    /// Verifies core-only consumers do not receive SQL storage or computed-column diagnostics.
    /// </summary>
    [Test]
    public async Task Should_enable_sql_portability_checks_only_for_sql_consumersAsync()
    {
        const string source = """
            using TedToolkit.Specifications;
            public record Entity(string Name, double Number) { public int Computed => 42; }
            public class Usage
            {
                public void Run()
                {
                    var selection = Selection.For<Entity>();
                    var text = selection.Name.Contains("\0");
                    var number = selection.Number.EqualTo(double.NaN);
                    var computed = selection.Computed > 0;
                }
            }
            """;
        var core = await CompilationProbe.RunAsync(source, includeSql: false).ConfigureAwait(false);
        await Assert.That(core.Errors).IsEmpty();
        await Assert.That(core.Diagnostics).IsEmpty();
        var sql = await CompilationProbe.RunAsync(source).ConfigureAwait(false);
        await Assert.That(sql.Diagnostics.Select(item => item.Id)).Contains("TSPEC010");
        await Assert.That(sql.Diagnostics.Select(item => item.Id)).Contains("TSPEC012");
        await Assert.That(sql.Diagnostics.Select(item => item.Id)).Contains("TSPEC014");
    }
}