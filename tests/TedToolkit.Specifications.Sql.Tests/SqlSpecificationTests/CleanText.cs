// -----------------------------------------------------------------------
// <copyright file="CleanText.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Sql.Tests.SqlSpecificationTests;

internal readonly record struct CleanText(string Value)
{
    public static explicit operator CleanText(string value)
    {
        return new(value.Replace("\0", "", StringComparison.Ordinal));
    }

    public static explicit operator string(in CleanText value)
    {
        return value.Value;
    }
}