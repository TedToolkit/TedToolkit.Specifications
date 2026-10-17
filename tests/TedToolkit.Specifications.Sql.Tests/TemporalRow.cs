// -----------------------------------------------------------------------
// <copyright file="TemporalRow.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Sql.Tests;

internal sealed record TemporalRow(
    int Id,
    decimal? Amount,
    DateOnly? Date,
    TimeOnly? Time,
    DateTime? Timestamp,
    DateTimeOffset? Instant,
    double? Measurement,
    ulong? Quantity);