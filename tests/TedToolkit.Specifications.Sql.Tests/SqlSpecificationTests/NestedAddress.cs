// -----------------------------------------------------------------------
// <copyright file="NestedAddress.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;

namespace TedToolkit.Specifications.Sql.Tests.SqlSpecificationTests;

internal sealed record NestedAddress(string? Name);