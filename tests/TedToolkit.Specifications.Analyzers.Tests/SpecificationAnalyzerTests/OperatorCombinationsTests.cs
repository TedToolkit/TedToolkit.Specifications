// -----------------------------------------------------------------------
// <copyright file="OperatorCombinationsTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Analyzers.Tests.SpecificationAnalyzerTests;

internal sealed class OperatorCombinationsTests
{
    /// <summary>
    /// Verifies nested comparisons and mixed logical operators compile without diagnostics and match direct candidate values.
    /// </summary>
    [Test]
    public async Task Should_preserve_nested_operator_truth_tables_without_false_positivesAsync()
    {
        var probe = await CompilationProbe.RunAsync("""
            using System;
            using TedToolkit.Specifications;
            public sealed record Root(Child? Child);
            public sealed record Child(Leaf? Leaf, int Score);
            public sealed record Leaf(string? Name);
            public static class Usage
            {
                public static bool Run()
                {
                    var root = Selection.For<Root>();
                    var score = root.Child.Score;
                    var name = root.Child.Leaf.Name;
                    Root[] candidates = [new(null), new(new(null, 0)), new(new(new(null), 1)),
                        new(new(new("Ada"), 2)), new(new(new("Bob"), -1))];
                    foreach (var candidate in candidates)
                    {
                        bool present = candidate.Child is not null;
                        int value = candidate.Child?.Score ?? 0;
                        string? text = candidate.Child?.Leaf?.Name;
                        Specification<Root>[] rules = [score == 1, 1 == score, score != 1, 1 != score,
                            score > 1, 1 > score, score >= 1, 1 >= score, score < 1, 1 < score, score <= 1, 1 <= score,
                            score.EqualTo(1), score.NotEqualTo(1), score.GreaterThan(1), score.GreaterThanOrEqual(1),
                            score.LessThan(1), score.LessThanOrEqual(1), score.IsNull(), score.IsNotNull(),
                            name == null, null == name, name != "Ada", "Ada" != name];
                        bool[] expected = [present && value == 1, present && value == 1,
                            !(present && value == 1), !(present && value == 1), present && value > 1, present && 1 > value,
                            present && value >= 1, present && 1 >= value, present && value < 1, present && 1 < value,
                            present && value <= 1, present && 1 <= value, present && value == 1, !(present && value == 1),
                            present && value > 1, present && value >= 1, present && value < 1, present && value <= 1,
                            !present, present, text == null, text == null, text != "Ada", text != "Ada"];
                        for (int index = 0; index < rules.Length; index++)
                            if (rules[index].IsSatisfiedBy(candidate) != expected[index]) return false;
                        var positive = score > 0;
                        var named = name == "Ada";
                        bool a = present && value > 0;
                        bool b = text == "Ada";
                        Specification<Root>[] combinations = [positive && named, positive || named, positive & named,
                            positive | named, !positive, positive.And(named), positive.Or(named), positive.Not(),
                            positive.BitwiseAnd(named), positive.BitwiseOr(named), positive.LogicalNot(),
                            !(positive && named) || positive.Not().And(named)];
                        bool[] combined = [a && b, a || b, a && b, a || b, !a, a && b, a || b, !a, a && b, a || b, !a,
                            !(a && b) || !a && b];
                        for (int index = 0; index < combinations.Length; index++)
                            if (combinations[index].IsSatisfiedBy(candidate) != combined[index]) return false;
                    }
                    return true;
                }
            }
            """).ConfigureAwait(false);
        await Assert.That(probe.Errors).IsEmpty();
        await Assert.That(probe.Diagnostics).IsEmpty();
        await Assert.That(probe.Evaluate()).IsTrue();
    }
}