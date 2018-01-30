// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Enumeration
{
    internal static class FileSystemEnumerableFactory
    {
        internal static void NormalizeInputs(ref string directory, ref string expression, EnumerationOptions options)
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

            switch (options.MatchType)
            {
                case MatchType.Dos:
                    // Historically we always treated "." as "*"
                    if (string.IsNullOrEmpty(expression) || expression == "." || expression == "*.*")
                    {
                        expression = "*";
                    }
                    else
                    {
                        expression = FileSystemName.TranslateDosExpression(expression);
                    }
                    break;
                case MatchType.Simple:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(options));
            }
        }

        internal static FileSystemEnumerable<string, string> UserFiles(string directory,
            string expression,
            EnumerationOptions options)
        {
            return new FileSystemEnumerable<string, string>(
                directory,
                (in FileSystemEntry entry, string state) => entry.ToSpecifiedFullPath(),
                (in FileSystemEntry entry, string state) =>
                {
                    return !entry.IsNameDotOrDotDot
                        && !entry.IsDirectory
                        && FileSystemName.MatchesDosExpression(state, entry.FileName, ignoreCase: true);
                },
                options)
            {
                State = expression,
            };
        }

        private static bool MatchesPattern(string expression, ReadOnlySpan<char> name, EnumerationOptions options)
        {
            switch (options.MatchType)
            {
                case MatchType.Simple:
                    return FileSystemName.MatchesSimpleExpression(expression, name, ignoreCase: true);
                case MatchType.Dos:
                    return FileSystemName.MatchesDosExpression(expression, name, ignoreCase: true);
                default:
                    throw new ArgumentOutOfRangeException(nameof(options));
            }
        }

        internal static FileSystemEnumerable<string, (string, EnumerationOptions)> UserDirectories(string directory,
            string expression,
            EnumerationOptions options)
        {
            return new FileSystemEnumerable<string, (string, EnumerationOptions)>(
                directory,
                (in FileSystemEntry entry, (string, EnumerationOptions) state) => entry.ToSpecifiedFullPath(),
                (in FileSystemEntry entry, (string expression, EnumerationOptions options) state) =>
                {
                    return !entry.IsNameDotOrDotDot
                        && entry.IsDirectory
                        && MatchesPattern(expression, entry.FileName, options);
                },
                options)
            {
                State = (expression, options),
            };
        }

        internal static FileSystemEnumerable<string, (string, EnumerationOptions)> UserEntries(string directory,
            string expression,
            EnumerationOptions options)
        {
            return new FileSystemEnumerable<string, (string, EnumerationOptions)>(
                directory,
                (in FileSystemEntry entry, (string, EnumerationOptions) state) => entry.ToSpecifiedFullPath(),
                (in FileSystemEntry entry, (string expression, EnumerationOptions options) state) =>
                {
                    return !entry.IsNameDotOrDotDot
                        && MatchesPattern(expression, entry.FileName, options);
                },
                options)
            {
                State = (expression, options),
            };
        }

        internal static FileSystemEnumerable<FileInfo, string> FileInfos(
            string directory,
            string expression,
            EnumerationOptions options)
        {
             return new FileSystemEnumerable<FileInfo, string>(
                directory,
                (in FileSystemEntry entry, string state) => entry.ToFileInfo(),
                (in FileSystemEntry entry, string state) =>
                {
                    return !entry.IsNameDotOrDotDot
                        && !entry.IsDirectory
                        && FileSystemName.MatchesDosExpression(state, entry.FileName, ignoreCase: true);
                },
                options)
             {
                 State = FileSystemName.TranslateDosExpression(expression),
             };
        }

        internal static FileSystemEnumerable<DirectoryInfo, string> DirectoryInfos(
            string directory,
            string expression,
            EnumerationOptions options)
        {
            return new FileSystemEnumerable<DirectoryInfo, string>(
               directory,
               (in FileSystemEntry entry, string state) => entry.ToDirectoryInfo(),
               (in FileSystemEntry entry, string state) =>
               {
                   return !entry.IsNameDotOrDotDot
                       && entry.IsDirectory
                       && FileSystemName.MatchesDosExpression(state, entry.FileName, ignoreCase: true);
               },
               options)
            {
                State = FileSystemName.TranslateDosExpression(expression),
            };
        }

        internal static FileSystemEnumerable<FileSystemInfo, string> FileSystemInfos(
            string directory,
            string expression,
            EnumerationOptions options)
        {
            return new FileSystemEnumerable<FileSystemInfo, string>(
               directory,
               (in FileSystemEntry entry, string state) => entry.ToFileSystemInfo(),
               (in FileSystemEntry entry, string state) =>
               {
                   return !entry.IsNameDotOrDotDot
                       && FileSystemName.MatchesDosExpression(state, entry.FileName, ignoreCase: true);
               },
               options)
            {
                State = FileSystemName.TranslateDosExpression(expression),
            };
        }
    }
}
