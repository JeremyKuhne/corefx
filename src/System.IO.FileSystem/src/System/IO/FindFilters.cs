// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static partial class FindFilters
    {
        internal static IFindFilter DosFileNameMatch(string expression) => new DosMatch(expression, ignoreCase: true);
        internal static IFindFilter NotCurentOrPreviousDirectory => NotRelativeDirectory.Instance;
        internal static IFindFilter And(IFindFilter first, IFindFilter second) => new Two(first, second);
        internal static IFindFilter And(params IFindFilter[] filters) => new Multiple(filters);
        internal static IFindFilter NoFilter => All.Instance;

        internal static bool IsRelativeDirectory(ReadOnlySpan<char> fileName) => NotRelativeDirectory.IsRelativeDirectory(fileName);
    }
}
