// -----------------------------------------------------------------------
// <copyright file="RequiresPostgresAttribute.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using TUnit.Core;

namespace TedToolkit.Specifications.Sql.Tests;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class RequiresPostgresAttribute() : SkipAttribute("Set SPECIFICATION_POSTGRES to run real PostgreSQL integration.")
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        return Task.FromResult(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SPECIFICATION_POSTGRES")));
    }
}