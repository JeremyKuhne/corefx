﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    internal static class FileSystemEnumerableFactory
    {
        internal static void NormalizeInputs(ref string directory, ref string expression)
        {
            if (PathHelpers.IsPathRooted(expression))
                throw new ArgumentException(SR.Arg_Path2IsRooted, nameof(expression));

            // We always allowed breaking the passed in directory and filter to be separated
            // any way the user wanted. Looking for "C:\foo\*.cs" could be passed as "C:\" and
            // "foo\*.cs" or "C:\foo" and "*.cs", for example. As such we need to combine and
            // split the inputs if the expression contains a directory separator.
            //
            // We also allowed for expression to be "foo\" which would translate to "foo\*".

            ReadOnlySpan<char> directoryName = PathHelpers.GetDirectoryNameNoChecks(expression.AsReadOnlySpan());

            if (directoryName.Length != 0)
            {
                // Need to fix up the input paths
                directory = PathHelpers.CombineNoChecks(directory, directoryName);
                expression = expression.Substring(directoryName.Length + 1);
            }

            // Historically we always treated "." as "*"
            if (string.IsNullOrEmpty(expression) || expression == "." || expression == "*.*")
                expression = "*";
        }

        internal static FileSystemEnumerable<string, string> UserFiles(string directory,
            string expression = "*",
            FindOptions options = default)
        {
            return new FileSystemEnumerable<string, string>(
                directory,
                (ref FileSystemEntry findData, string state) => findData.AsUserFullPath(),
                (ref FileSystemEntry findData, string state) =>
                {
                    return findData.NotDotOrDotDot
                        && !findData.IsDirectory
                        && NameMatchers.MatchDosPattern(state, findData.FileName, ignoreCase: true);
                })
            {
                State = NameMatchers.TranslateDosExpression(expression),
                Options = options
            };
        }

        internal static FileSystemEnumerable<string, string> UserDirectories(string directory,
            string expression = "*",
            FindOptions options = default)
        {
            return new FileSystemEnumerable<string, string>(
                directory,
                (ref FileSystemEntry findData, string state) => findData.AsUserFullPath(),
                (ref FileSystemEntry findData, string state) =>
                {
                    return findData.NotDotOrDotDot
                        && findData.IsDirectory
                        && NameMatchers.MatchDosPattern(state, findData.FileName, ignoreCase: true);
                })
            {
                State = NameMatchers.TranslateDosExpression(expression),
                Options = options
            };
        }

        internal static FileSystemEnumerable<string, string> UserEntries(string directory,
            string expression = "*",
            FindOptions options = default)
        {
            return new FileSystemEnumerable<string, string>(
                directory,
                (ref FileSystemEntry findData, string state) => findData.AsUserFullPath(),
                (ref FileSystemEntry findData, string state) =>
                {
                    return findData.NotDotOrDotDot
                        && NameMatchers.MatchDosPattern(state, findData.FileName, ignoreCase: true);
                })
            {
                State = NameMatchers.TranslateDosExpression(expression),
                Options = options
            };
        }

        internal static FileSystemEnumerable<FileInfo, string> FileInfos(
            string directory,
            string expression = "*",
            FindOptions options = default)
        {
             return new FileSystemEnumerable<FileInfo, string>(
                directory,
                (ref FileSystemEntry findData, string state) => findData.AsFileInfo(),
                (ref FileSystemEntry findData, string state) =>
                {
                    return findData.NotDotOrDotDot
                        && !findData.IsDirectory
                        && NameMatchers.MatchDosPattern(state, findData.FileName, ignoreCase: true);
                })
             {
                 State = NameMatchers.TranslateDosExpression(expression),
                 Options = options
             };
        }

        internal static FileSystemEnumerable<DirectoryInfo, string> DirectoryInfos(
            string directory,
            string expression = "*",
            FindOptions options = default)
        {
            return new FileSystemEnumerable<DirectoryInfo, string>(
               directory,
               (ref FileSystemEntry findData, string state) => findData.AsDirectoryInfo(),
               (ref FileSystemEntry findData, string state) =>
               {
                   return findData.NotDotOrDotDot
                       && findData.IsDirectory
                       && NameMatchers.MatchDosPattern(state, findData.FileName, ignoreCase: true);
               })
            {
                State = NameMatchers.TranslateDosExpression(expression),
                Options = options
            };
        }

        internal static FileSystemEnumerable<FileSystemInfo, string> FileSystemInfos(
            string directory,
            string expression = "*",
            FindOptions options = default)
        {
            return new FileSystemEnumerable<FileSystemInfo, string>(
               directory,
               (ref FileSystemEntry findData, string state) => findData.AsFileSystemInfo(),
               (ref FileSystemEntry findData, string state) =>
               {
                   return findData.NotDotOrDotDot
                       && NameMatchers.MatchDosPattern(state, findData.FileName, ignoreCase: true);
               })
            {
                State = NameMatchers.TranslateDosExpression(expression),
                Options = options
            };
        }
    }
}
