// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    public static class FindTransforms
    {
        public static DirectoryInfo AsDirectoryInfo<TState>(ref FindData<TState> findData)
        {
            string fileName = new string(findData.FileName);
            return DirectoryInfo.Create(PathHelpers.CombineNoChecks(findData.Directory, fileName), fileName, ref findData);
        }

        public static FileInfo AsFileInfo<TState>(ref FindData<TState> findData)
        {
            string fileName = new string(findData.FileName);
            return FileInfo.Create(PathHelpers.CombineNoChecks(findData.Directory, fileName), fileName, ref findData);
        }

        public static FileSystemInfo AsFileSystemInfo<TState>(ref FindData<TState> findData)
        {
            string fileName = new string(findData.FileName);
            string fullPath = PathHelpers.CombineNoChecks(findData.Directory, fileName);

            return (findData.Attributes & FileAttributes.Directory) != 0
                ? (FileSystemInfo)DirectoryInfo.Create(fullPath, fileName, ref findData)
                : (FileSystemInfo)FileInfo.Create(fullPath, fileName, ref findData);
        }

        /// <summary>
        /// Returns the full path for find results, based off of the initially provided path.
        /// </summary>
        public static string AsUserFullPath<TState>(ref FindData<TState> findData)
        {
            ReadOnlySpan<char> subdirectory = findData.Directory.AsReadOnlySpan().Slice(findData.OriginalDirectory.Length);
            return PathHelpers.CombineNoChecks(findData.OriginalUserDirectory, subdirectory, findData.FileName);
        }
    }
}
