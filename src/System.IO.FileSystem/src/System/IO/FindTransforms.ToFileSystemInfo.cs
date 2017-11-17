// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static partial class FindTransforms
    {
        /// <summary>
        /// 
        /// </summary>
        private class ToFileSystemInfo : IFindTransform<FileSystemInfo>
        {
            private ToFileSystemInfo() { }
            public static ToFileSystemInfo Instance = new ToFileSystemInfo();

            public unsafe FileSystemInfo TransformResult(ref RawFindData findData)
            {
                // TODO:
                return null;
            }
        }
    }
}
