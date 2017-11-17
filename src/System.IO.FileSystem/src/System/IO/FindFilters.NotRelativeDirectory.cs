// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static partial class FindFilters
    {
        /// <summary>
        /// Filters out current "." and previous ".." directory entries.
        /// </summary>
        private class NotRelativeDirectory : IFindFilter
        {
            public static NotRelativeDirectory Instance = new NotRelativeDirectory();

            private NotRelativeDirectory() { }

            public unsafe bool Match(ref RawFindData findData) => !IsRelativeDirectory(findData.FileName);

            public static unsafe bool IsRelativeDirectory(ReadOnlySpan<char> fileName)
            {
                return !(fileName.Length > 2
                    || fileName[0] != '.'
                    || (fileName.Length == 2 && fileName[1] != '.'));
            }
        }
    }
}
