// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    public static class FindPredicates
    {
        public static bool NotDotOrDotDot<TState>(ref FindData<TState> findData) => !PathHelpers.IsDotOrDotDot(findData.FileName);

        public static bool IsDirectory<TState>(ref FindData<TState> findData)
        {
            FileAttributes attributes = findData.Attributes;
            return attributes != (FileAttributes)(-1)
                && (attributes & FileAttributes.Directory) != 0;
        }
    }
}
