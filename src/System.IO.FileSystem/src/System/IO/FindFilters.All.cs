// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static partial class FindFilters
    {
        /// <summary>
        /// Returns all results. (No filtering)
        /// </summary>
        private class All : IFindFilter
        {
            public static All Instance = new All();

            private All() { }

            public bool Match(ref RawFindData findData) => true;
        }
    }
}
