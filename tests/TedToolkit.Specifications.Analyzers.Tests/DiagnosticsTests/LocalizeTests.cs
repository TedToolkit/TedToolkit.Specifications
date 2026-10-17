// -----------------------------------------------------------------------
// <copyright file="LocalizeTests.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;

using Microsoft.CodeAnalysis;

namespace TedToolkit.Specifications.Analyzers.Tests.DiagnosticsTests;

internal sealed class LocalizeTests
{
    /// <summary>
    /// Verifies the generated resources follow Localizations culture selection and base-text fallback.
    /// </summary>
    [Test]
    [NotInParallel]
    [Arguments("en-US", false)]
    [Arguments("en-GB", false)]
    [Arguments("fr-FR", false)]
    [Arguments("zh-CN", true)]
    [Arguments("zh-SG", false)]
    [Arguments("zh-Hans", false)]
    [Arguments("zh-Hans-CN", false)]
    [Arguments("zh-TW", false)]
    public async Task Should_localize_all_titles_and_messages_with_fallbackAsync(string cultureName, bool chinese)
    {
        var before = Localizations.LocalizationSettings.Culture;
        try
        {
            Localizations.LocalizationSettings.Culture = cultureName;
            (string Title, string Message)[] texts =
            [
                (Localization.TSPEC001.Title, Localization.TSPEC001.Message),
                (Localization.TSPEC002.Title, Localization.TSPEC002.Message),
                (Localization.TSPEC003.Title, Localization.TSPEC003.Message),
                (Localization.TSPEC004.Title, Localization.TSPEC004.Message),
                (Localization.TSPEC005.Title, Localization.TSPEC005.Message),
                (Localization.TSPEC006.Title, Localization.TSPEC006.Message),
                (Localization.TSPEC007.Title, Localization.TSPEC007.Message),
                (Localization.TSPEC008.Title, Localization.TSPEC008.Message),
                (Localization.TSPEC009.Title, Localization.TSPEC009.Message),
                (Localization.TSPEC010.Title, Localization.TSPEC010.Message),
                (Localization.TSPEC011.Title, Localization.TSPEC011.Message),
                (Localization.TSPEC012.Title, Localization.TSPEC012.Message),
                (Localization.TSPEC013.Title, Localization.TSPEC013.Message),
                (Localization.TSPEC014.Title, Localization.TSPEC014.Message),
                (Localization.TSPEC015.Title, Localization.TSPEC015.Message),
                (Localization.TSPEC016.Title, Localization.TSPEC016.Message),
                (Localization.TSPEC017.Title, Localization.TSPEC017.Message),
                (Localization.TSPEC018.Title, Localization.TSPEC018.Message),
            ];
            foreach (var (title, format) in texts)
            {
                var message = string.Format(CultureInfo.GetCultureInfo(cultureName), CompositeFormat.Parse(format),
                    "ExampleMember", "ExampleType");
                await Assert.That(title.Any(character => character is >= '\u4e00' and <= '\u9fff')).IsEqualTo(chinese);
                await Assert.That(message.Any(character => character is >= '\u4e00' and <= '\u9fff')).IsEqualTo(chinese);
                await Assert.That(message.Contains("{0}", StringComparison.Ordinal)).IsFalse();
                await Assert.That(message.Contains("{1}", StringComparison.Ordinal)).IsFalse();
            }

            var property = string.Format(CultureInfo.InvariantCulture, CompositeFormat.Parse(Localization.TSPEC002.Message),
                "Cost", "Money");
            var expected = chinese
                ? "属性“Cost”的类型“Money”不受支持；请改为公开可查询的标量属性"
                : "Property 'Cost' has unsupported type 'Money'; expose a queryable scalar instead";
            await Assert.That(property).IsEqualTo(expected);
        }
        finally
        {
            Localizations.LocalizationSettings.Culture = before;
        }
    }

    /// <summary>
    /// Verifies actual Analyzer diagnostics use generated strings directly without changing localization settings.
    /// </summary>
    [Test]
    [NotInParallel]
    public async Task Should_use_generated_strings_without_mutating_settingsAsync()
    {
        var before = Localizations.LocalizationSettings.Culture;
        var probe = await CompilationProbe.RunAsync("""
            using TedToolkit.Specifications;
            public static class Usage
            {
                public static void Run() { if (Selection.For<int>() > 0) { } }
            }
            """, includeSql: false).ConfigureAwait(false);
        var diagnostic = probe.Diagnostics.Single();
        await Assert.That(diagnostic.Id).IsEqualTo("TSPEC017");
        await Assert.That(diagnostic.Descriptor.Title.ToString(CultureInfo.InvariantCulture)).IsEqualTo(Localization.TSPEC017.Title);
        await Assert.That(diagnostic.GetMessage(CultureInfo.InvariantCulture)).IsEqualTo(Localization.TSPEC017.Message);

        var property = Diagnostic.Create(Diagnostics.PropertyType, Location.None, "Cost", "Money");
        var expected = string.Format(CultureInfo.InvariantCulture, CompositeFormat.Parse(Localization.TSPEC002.Message), "Cost", "Money");
        await Assert.That(property.GetMessage(CultureInfo.InvariantCulture)).IsEqualTo(expected);
        await Assert.That(Localizations.LocalizationSettings.Culture).IsEqualTo(before);
    }
}