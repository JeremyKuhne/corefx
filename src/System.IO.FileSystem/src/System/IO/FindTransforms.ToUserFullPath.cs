// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static partial class FindTransforms
    {
        /// <summary>
        /// Returns the full path for find results, based off of the initially provided path.
        /// </summary>
        private class ToUserFullPath : IFindTransform<string>
        {
            private ToUserFullPath() { }
            public static ToUserFullPath Instance = new ToUserFullPath();

            public unsafe string TransformResult(ref RawFindData findData)
                => PathHelpers.CombineNoChecks(findData.UserDirectory, findData.FileName);
        }
    }
}
