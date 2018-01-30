﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Enumeration
{
    public enum MatchType
    {
        /// <summary>
        /// Match using '*' and '?' wildcards.
        /// </summary>
        Simple,

        /// <summary>
        /// Match using DOS style matching semantics. '*', '?', '&lt;', '&gt;', and '"'
        /// are all considered wildcards.
        /// </summary>
        Dos
    }
}
