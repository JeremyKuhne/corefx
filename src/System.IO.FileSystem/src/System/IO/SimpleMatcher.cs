// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace System.IO
{
    public static class SimpleMatcher
    {
        /// <summary>
        /// Return true if the given expression matches the given name. '*' and '?' are wildcards, '\' escapes.
        /// </summary>
        public static bool MatchPattern(string expression, ReadOnlySpan<char> name, bool ignoreCase = true)
        {
            return DosMatcher.MatchPattern(expression, name, ignoreCase, useExtendedWildcards: false);
        }
    }
}
