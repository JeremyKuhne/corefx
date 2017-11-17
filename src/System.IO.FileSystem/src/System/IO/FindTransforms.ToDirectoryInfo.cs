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
        private class ToDirectoryInfo : IFindTransform<DirectoryInfo>
        {
            private ToDirectoryInfo() { }
            internal static ToDirectoryInfo Instance = new ToDirectoryInfo();

            public unsafe DirectoryInfo TransformResult(ref RawFindData findData)
            {
                // TODO:
                return null;
            }
        }
    }
}
