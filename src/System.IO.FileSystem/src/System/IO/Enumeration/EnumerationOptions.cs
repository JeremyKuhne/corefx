﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Enumeration
{
    public struct EnumerationOptions
    {
        /// <summary>
        /// Create an enumeration options struct with the default options.
        /// </summary>
        /// <remarks>
        /// The implementation here may get more complicated as we add more options.
        /// </remarks>
        public static EnumerationOptions Default => default;

        /// <summary>
        /// Should we recurse into subdirectories while enumerating?
        /// </summary>
        public bool Recurse { get; set; }

        /// <summary>
        /// Skip files/directories when access is denied (e.g. AccessDeniedException/SecurityException)
        /// </summary>
        public bool IgnoreInaccessible { get; set; }

        /// <summary>
        /// Suggested buffer size.
        /// </summary>
        /// <remarks>
        /// Not all platforms use user allocated buffers, and some require either fixed buffers or a
        /// buffer that has enough space to return a full result. One scenario where this option is
        /// useful is with remote share enumeration on Windows. Having a large buffer may result in
        /// better performance as more results can be batched over the wire.
        /// </remarks>
        public int BufferSize { get; set; }

        /// <summary>
        /// Skip entries with the given attributes.
        /// </summary>
        public FileAttributes AttributesToSkip { get; set; }

        /// <summary>
        /// For APIs that allow specifying a match expression this will allow you to specify how
        /// to interpret the match expression.
        /// </summary>
        /// <remarks>
        /// The default is simple matching where '*' is always 0 or more characters and '?' is a single character.
        /// </remarks>
        public MatchType MatchType { get; set; }
    }
}
