// -----------------------------------------------------------------------
// <copyright file="Row.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Sql.Tests;

internal sealed record Row(int Id, int? Number, string? Text, bool? Enabled, Guid? Key, Status? Status);