// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static partial class FindFilters
    {
        /// <summary>
        /// Allows combining multiple filters. Runs through filters in order until
        /// a filter rejects the result (returns false).
        /// </summary>
        internal class Multiple : IFindFilter
        {
            private IFindFilter[] _filters;

            public Multiple(params IFindFilter[] filters)
            {
                _filters = filters;
            }

            public unsafe bool Match(ref RawFindData findData)
            {
                foreach (IFindFilter filter in _filters)
                {
                    if (!filter.Match(ref findData))
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Allows combining multiple filters. Runs through filters in order until
        /// a filter rejects the result (returns false).
        /// </summary>
        internal class Two : IFindFilter
        {
            private IFindFilter _first;
            private IFindFilter _second;

            public Two(IFindFilter first, IFindFilter second)
            {
                _first = first;
                _second = second;
            }

            public unsafe bool Match(ref RawFindData findData)
            {
                return _first.Match(ref findData) && _second.Match(ref findData);
            }
        }
    }
}
