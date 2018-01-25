// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    public struct FindOptions
    {
        /// <summary>
        /// Enumerate subdirectories
        /// </summary>
        public bool Recurse { get; set; }

        /// <summary>
        /// Skip files/directories when access is denied (e.g. AccessDeniedException/SecurityException)
        /// </summary>
        public bool IgnoreInaccessible { get; set; }

        /// <summary>
        /// Use a buffer of at least the specified size.
        /// </summary>
        public int MinimumBufferSize { get; set; }

        /// <summary>
        /// Skip entries with the given attributes.
        /// </summary>
        public FileAttributes SkipAttributes { get; set; }
    }
}
