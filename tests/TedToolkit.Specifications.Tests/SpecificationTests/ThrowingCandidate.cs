// -----------------------------------------------------------------------
// <copyright file="ThrowingCandidate.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

namespace TedToolkit.Specifications.Tests.SpecificationTests;

internal sealed class ThrowingCandidate
{
    public int Number
    {
        get
        {
            Reads++;
            throw new InvalidOperationException("The getter was evaluated.");
        }
    }

    public int Reads { get; private set; }
}