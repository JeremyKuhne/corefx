// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [System.Security.SecurityCritical]  // auto-generated
        internal static SafeFileHandle UnsafeCreateFile(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes)
        {
            return new SafeFileHandle(CreateFile(lpFileName, dwDesiredAccess, dwShareMode, dwCreationDisposition, dwFlagsAndAttributes), ownsHandle: true);
        }
    }
}
