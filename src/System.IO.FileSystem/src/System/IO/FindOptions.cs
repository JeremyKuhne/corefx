// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    [Flags]
    public enum FindOptions
    {
        None = 0x0,

        /// <summary>
        /// Enumerate subdirectories
        /// </summary>
        Recurse = 0x0000_0001,

        /// <summary>
        /// Skip files/directories when access is denied (e.g. AccessDeniedException/SecurityException)
        /// </summary>
        IgnoreInaccessable = 0x0000_0002,


        /// <summary>
        /// Hint to use larger buffers for getting data (notably to help address remote enumeration perf)
        /// </summary>
        UseLargeBuffer = 0x0000_0004,

        /// <summary>
        /// Allow .NET to skip locking if you know the enumerator won't be used on multiple threads
        /// </summary>
        /// <remarks>
        /// Enumerating is inherently not thread-safe, but .NET needs to still lock by default to
        /// avoid access violations with native access should someone actually try to use the
        /// the same enumerator on multiple threads.
        /// </remarks>
        AvoidLocking = 0x0000_0008,
    }
}
