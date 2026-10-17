// -----------------------------------------------------------------------
// <copyright file="ChainRoot.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Tests.SelectionTests;

internal sealed record ChainRoot(ChainChild? Child, ChainChild? Other);