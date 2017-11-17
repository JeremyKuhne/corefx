// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static partial class FindFilters
    {
        /// <summary>
        /// Match file names via the traditional DOS matching.
        /// ("*.*" matches everything, "foo*." matches all files without an extension that begin with foo, etc.)
        /// </summary>
        private class DosMatch : IFindFilter
        {
            private string _filter;
            private bool _ignoreCase;

            public DosMatch(string filter, bool ignoreCase)
            {
                _filter = DosMatcher.TranslateExpression(filter);
                _ignoreCase = ignoreCase;
            }

            public unsafe bool Match(ref RawFindData findData)
            {
                if (_filter == null)
                    return true;

                // RtlIsNameInExpression is the native API DosMatcher is replicating.
                return DosMatcher.MatchPattern(_filter, findData.FileName, _ignoreCase);
            }
        }
    }
}
