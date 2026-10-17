// -----------------------------------------------------------------------
// <copyright file="OperandTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationAnalyzerTests;

internal sealed class OperandTests
{
    /// <summary>
    /// Verifies user-defined conversions are evaluated before SQL validation rather than treated as transparent casts.
    /// </summary>
    [Test]
    public async Task Should_allow_sanitized_user_defined_conversion_operandsAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using System;
            using TedToolkit.Specifications;
            public readonly record struct CleanText(string Value)
            {
                public static explicit operator CleanText(string value) => new(value.Replace("\0", "", StringComparison.Ordinal));
                public static explicit operator string(CleanText value) => value.Value;
            }
            public static class Usage
            {
                public static bool Run() => Selection.For<string>().EqualTo((string)(CleanText)"\0").IsSatisfiedBy("");
            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Evaluate()).IsTrue();
    }

    /// <summary>
    /// Verifies computed operands are not rejected because their intermediate inputs contain invalid SQL constants.
    /// </summary>
    [Test]
    [Arguments("text.EqualTo(\"\\0\".Replace(\"\\0\", \"\"))")]
    [Arguments("text.In([\"\\0\".Replace(\"\\0\", \"\")])")]
    [Arguments("number.EqualTo(double.IsNaN(double.NaN) ? 1 : 0)")]
    [Arguments("number.In(new[] { double.IsNaN(double.NaN) ? 1d : 0d })")]
    [Arguments("number.EqualTo(Identity(double.IsNaN(double.NaN) ? 1 : 0))")]
    public async Task Should_allow_valid_computed_operandsAsync(string expression)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public static class Usage
            {
                private static double Identity(double value) => value;
                public static object Run()
                {
                    var text = Selection.For<string>();
                    var number = Selection.For<double>();
                    return {{expression}};
                }
            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
    }

    /// <summary>
    /// Verifies direct invalid values remain diagnosed through nullable conversions and membership initializers.
    /// </summary>
    [Test]
    [Arguments("text.In([\"\\0\"])", "TSPEC010")]
    [Arguments("text.In(new System.Collections.Generic.List<string> { \"\\0\" })", "TSPEC010")]
    [Arguments("number.EqualTo((double?)double.NaN)", "TSPEC014")]
    [Arguments("number.In(new double?[] { double.PositiveInfinity })", "TSPEC014")]
    [Arguments("number.In([double.NaN])", "TSPEC014")]
    public async Task Should_reject_invalid_actual_operandsAsync(string expression, string id)
    {
        var probe = await CompilationProbe.RunAsync($$"""
            using TedToolkit.Specifications;
            public static class Usage
            {
                public static object Run()
                {
                    var text = Selection.For<string>();
                    var number = Selection.For<double?>();
                    return {{expression}};
                }
            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Diagnostics.Select(diagnostic => diagnostic.Id)).Contains(id);
        await Assert.That(probe.Diagnostics.Any(diagnostic => diagnostic.Id == "AD0001")).IsFalse();
    }
}