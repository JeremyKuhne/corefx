// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO
{
    // Helper methods related to paths.  Some of these are copies of 
    // internal members of System.IO.Path from System.Runtime.Extensions.dll.
    internal static partial class PathHelpers
    {
        // Array of the separator chars
        internal static readonly char[] DirectorySeparatorChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        // string-representation of the directory-separator character, used when appending the character to another
        // string so as to avoid the boxing of the character when calling string.Concat(..., object).
        internal static readonly string DirectorySeparatorCharAsString = Path.DirectorySeparatorChar.ToString();

        // System.IO.Path has both public Combine and internal InternalCombine
        // members.  InternalCombine performs these extra validations on the second 
        // argument.  This provides a convenient helper to maintain this extra
        // validation when porting code from Path.InternalCombine to Path.Combine.
        internal static void ThrowIfEmptyOrRootedPath(string path2)
        {
            if (path2 == null)
                throw new ArgumentNullException(nameof(path2));
            if (path2.Length == 0)
                throw new ArgumentException(SR.Argument_PathEmpty, nameof(path2));
            if (Path.IsPathRooted(path2))
                throw new ArgumentException(SR.Arg_Path2IsRooted, nameof(path2));
        }

        internal static bool IsRoot(string path)
        {
            return path.Length == PathInternal.GetRootLength(path);
        }

        internal static bool EndsInDirectorySeparator(string path)
        {
            return path.Length > 0 && PathInternal.IsDirectorySeparator(path[path.Length - 1]);
        }

        /// <summary>
        /// Combines two paths. Does no validation of paths, only concatenates the paths
        /// and places a directory separator between them if needed.
        /// </summary>
        internal static string CombineNoChecks(string first, ReadOnlySpan<char> second)
        {
            if (string.IsNullOrEmpty(first))
                return second.Length == 0
                    ? string.Empty
                    : new string(second);

            if (second.Length == 0)
                return first;

            return CombineNoChecksInternal(first.AsReadOnlySpan(), second);
        }

        /// <summary>
        /// Combines two paths. Does no validation of paths, only concatenates the paths
        /// and places a directory separator between them if needed.
        /// </summary>
        internal unsafe static string CombineNoChecks(ReadOnlySpan<char> first, string second)
        {
            if (string.IsNullOrEmpty(second))
                return first.Length == 0
                    ? string.Empty
                    : new string(first);

            if (first.Length == 0)
                return second;

            return CombineNoChecksInternal(first, second.AsReadOnlySpan());
        }

        /// <summary>
        /// Combines two paths. Does no validation of paths, only concatenates the paths
        /// and places a directory separator between them if needed.
        /// </summary>
        internal unsafe static string CombineNoChecks(string first, string second)
        {
            if (string.IsNullOrEmpty(first))
                return string.IsNullOrEmpty(second) ? string.Empty : second;

            if (string.IsNullOrEmpty(second))
                return first;

            return CombineNoChecksInternal(first.AsReadOnlySpan(), second.AsReadOnlySpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static string CombineNoChecksInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
        {
            Debug.Assert(first.Length > 0 && second.Length > 0, "should have dealt with empty paths");

            bool hasSeparator = first[first.Length - 1] == Path.DirectorySeparatorChar
                || second[0] == Path.DirectorySeparatorChar;

            int totalLength = first.Length + second.Length + (hasSeparator ? 0 : 1);
            string fullPath = new string('\0', totalLength);
            fixed (char* f = fullPath)
            {
                Span<char> pathSpan = new Span<char>(f, totalLength);
                first.CopyTo(pathSpan);
                if (!hasSeparator)
                    pathSpan[first.Length] = Path.DirectorySeparatorChar;
                second.CopyTo(pathSpan.Slice(first.Length + (hasSeparator ? 0 : 1)));
            }
            return fullPath;
        }
    }
}
