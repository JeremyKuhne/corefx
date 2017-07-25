// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.Augments;

namespace System
{
    public static partial class Environment
    {
        private static string CurrentDirectoryCore
        {
            get
            {
                StringBuilder sb = StringBuilderCache.Acquire(Interop.Kernel32.MAX_PATH + 1);
                if (Interop.Kernel32.GetCurrentDirectory(sb.Capacity, sb) == 0)
                {
                    StringBuilderCache.Release(sb);
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
                string currentDirectory = sb.ToString();

                // Note that if we have somehow put our command prompt into short
                // file name mode (i.e. by running edlin or a DOS grep, etc), then
                // this will return a short file name.
                if (currentDirectory.IndexOf('~') >= 0)
                {
                    int r = Interop.Kernel32.GetLongPathName(currentDirectory, sb, sb.Capacity);
                    if (r == 0 || r >= Interop.Kernel32.MAX_PATH)
                    {
                        int errorCode = r >= Interop.Kernel32.MAX_PATH ?
                            Interop.Errors.ERROR_FILENAME_EXCED_RANGE :
                            Marshal.GetLastWin32Error();

                        if (errorCode != Interop.Errors.ERROR_FILE_NOT_FOUND &&
                            errorCode != Interop.Errors.ERROR_PATH_NOT_FOUND &&
                            errorCode != Interop.Errors.ERROR_INVALID_FUNCTION &&
                            errorCode != Interop.Errors.ERROR_ACCESS_DENIED)
                        {
                            StringBuilderCache.Release(sb);
                            throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                        }
                    }

                    currentDirectory = sb.ToString();
                }

                StringBuilderCache.Release(sb);
                return currentDirectory;
            }
            set
            {
                if (!Interop.Kernel32.SetCurrentDirectory(value))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw Win32Marshal.GetExceptionForWin32Error(
                        errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND ? Interop.Errors.ERROR_PATH_NOT_FOUND : errorCode,
                        value);
                }
            }
        }

        public static string[] GetLogicalDrives() => DriveInfoInternal.GetLogicalDrives();

        public static string NewLine => "\r\n";

        private static int ProcessorCountFromSystemInfo
        {
            get
            {
                var info = default(Interop.Kernel32.SYSTEM_INFO);
                Interop.Kernel32.GetSystemInfo(out info);
                return info.dwNumberOfProcessors;
            }
        }

        public static int SystemPageSize
        {
            get
            {
                var info = default(Interop.Kernel32.SYSTEM_INFO);
                Interop.Kernel32.GetSystemInfo(out info);
                return info.dwPageSize;
            }
        }

        public static int ExitCode { get { return EnvironmentAugments.ExitCode; } set { EnvironmentAugments.ExitCode = value; } }

        private static string ExpandEnvironmentVariablesCore(string name)
        {
            int currentSize = 100;
            StringBuilder result = StringBuilderCache.Acquire(currentSize); // A somewhat reasonable default size

            result.Length = 0;
            int size = Interop.Kernel32.ExpandEnvironmentStringsW(name, result, currentSize);
            if (size == 0)
            {
                StringBuilderCache.Release(result);
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            while (size > currentSize)
            {
                currentSize = size;
                result.Length = 0;
                result.Capacity = currentSize;

                size = Interop.Kernel32.ExpandEnvironmentStringsW(name, result, currentSize);
                if (size == 0)
                {
                    StringBuilderCache.Release(result);
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }

            return StringBuilderCache.GetStringAndRelease(result);
        }

        private static string GetFolderPathCore(SpecialFolder folder, SpecialFolderOption option)
        {
            // We're using SHGetKnownFolderPath instead of SHGetFolderPath as SHGetFolderPath is
            // capped at MAX_PATH.
            //
            // Because we validate both of the input enums we shouldn't have to care about CSIDL and flag
            // definitions we haven't mapped. If we remove or loosen the checks we'd have to account
            // for mapping here (this includes tweaking as SHGetFolderPath would do).
            //
            // The only SpecialFolderOption defines we have are equivalent to KnownFolderFlags.

            string folderGuid;

            switch (folder)
            {
                case SpecialFolder.ApplicationData:
                    //folderGuid = Interop.Shell32.KnownFolders.RoamingAppData;
                    return Windows.Storage.UserDataPaths.RoamingAppData;
                    // break;
                case SpecialFolder.CommonApplicationData:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramData;
                    // Windows.Storage.AppDataPaths.ProgramData
                    break;
                case SpecialFolder.LocalApplicationData:
                    folderGuid = Interop.Shell32.KnownFolders.LocalAppData;
                    // Windows.Storage.AppDataPaths.LocalAppData
                    break;
                case SpecialFolder.Cookies:
                    folderGuid = Interop.Shell32.KnownFolders.Cookies;
                    // Windows.Storage.AppDataPaths.Cookies
                    break;
                case SpecialFolder.Desktop:
                    folderGuid = Interop.Shell32.KnownFolders.Desktop;
                    // Windows.Storage.AppDataPaths.Desktop
                    break;
                case SpecialFolder.Favorites:
                    folderGuid = Interop.Shell32.KnownFolders.Favorites;
                    // Windows.Storage.AppDataPaths.Favorites
                    break;
                case SpecialFolder.History:
                    folderGuid = Interop.Shell32.KnownFolders.History;
                    // Windows.Storage.AppDataPaths.History
                    break;
                case SpecialFolder.InternetCache:
                    folderGuid = Interop.Shell32.KnownFolders.InternetCache;
                    // Windows.Storage.AppDataPaths.InternetCache
                    break;
                case SpecialFolder.Programs:
                    folderGuid = Interop.Shell32.KnownFolders.Programs;
                    // TODO:NA
                    break;
                case SpecialFolder.MyComputer:
                    folderGuid = Interop.Shell32.KnownFolders.ComputerFolder;
                    // TODO:NA
                    break;
                case SpecialFolder.MyMusic:
                    folderGuid = Interop.Shell32.KnownFolders.Music;
                    // Windows.Storage.UserDataPaths.Music
                    break;
                case SpecialFolder.MyPictures:
                    folderGuid = Interop.Shell32.KnownFolders.Pictures;
                    // Windows.Storage.UserDataPaths.Pictures
                    break;
                case SpecialFolder.MyVideos:
                    folderGuid = Interop.Shell32.KnownFolders.Videos;
                    // Windows.Storage.UserDataPaths.Videos
                    break;
                case SpecialFolder.Recent:
                    folderGuid = Interop.Shell32.KnownFolders.Recent;
                    // Windows.Storage.UserDataPaths.Recent
                    break;
                case SpecialFolder.SendTo:
                    folderGuid = Interop.Shell32.KnownFolders.SendTo;
                    // TODO:NA
                    break;
                case SpecialFolder.StartMenu:
                    folderGuid = Interop.Shell32.KnownFolders.StartMenu;
                    // TODO:NA
                    break;
                case SpecialFolder.Startup:
                    folderGuid = Interop.Shell32.KnownFolders.Startup;
                    // TODO:NA
                    break;
                case SpecialFolder.System:
                    folderGuid = Interop.Shell32.KnownFolders.System;
                    // Windows.Storage.SystemDataPaths.System
                    break;
                case SpecialFolder.Templates:
                    folderGuid = Interop.Shell32.KnownFolders.Templates;
                    // Windows.Storage.UserDataPaths.Templates
                    break;
                case SpecialFolder.DesktopDirectory:
                    folderGuid = Interop.Shell32.KnownFolders.Desktop;
                    // Windows.Storage.UserDataPaths.Desktop
                    break;
                case SpecialFolder.Personal:
                    // Same as Personal
                    // case SpecialFolder.MyDocuments:
                    folderGuid = Interop.Shell32.KnownFolders.Documents;
                    // Windows.Storage.UserDataPaths.Documents
                    break;
                case SpecialFolder.ProgramFiles:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramFiles;
                    // TODO:NA
                    break;
                case SpecialFolder.CommonProgramFiles:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramFilesCommon;
                    // TODO:NA
                    break;
                case SpecialFolder.AdminTools:
                    folderGuid = Interop.Shell32.KnownFolders.AdminTools;
                    // TODO:NA
                    break;
                case SpecialFolder.CDBurning:
                    folderGuid = Interop.Shell32.KnownFolders.CDBurning;
                    // TODO:NA
                    break;
                case SpecialFolder.CommonAdminTools:
                    folderGuid = Interop.Shell32.KnownFolders.CommonAdminTools;
                    // TODO:NA
                    break;
                case SpecialFolder.CommonDocuments:
                    folderGuid = Interop.Shell32.KnownFolders.PublicDocuments;
                    // Windows.Storage.SystemDataPaths.PublicDocuments
                    break;
                case SpecialFolder.CommonMusic:
                    folderGuid = Interop.Shell32.KnownFolders.PublicMusic;
                    // Windows.Storage.SystemDataPaths.PublicMusic
                    break;
                case SpecialFolder.CommonOemLinks:
                    folderGuid = Interop.Shell32.KnownFolders.CommonOEMLinks;
                    // TODO:NA
                    break;
                case SpecialFolder.CommonPictures:
                    folderGuid = Interop.Shell32.KnownFolders.PublicPictures;
                    // Windows.Storage.SystemDataPaths.PublicPictures
                    break;
                case SpecialFolder.CommonStartMenu:
                    folderGuid = Interop.Shell32.KnownFolders.CommonStartMenu;
                    // TODO:NA
                    break;
                case SpecialFolder.CommonPrograms:
                    folderGuid = Interop.Shell32.KnownFolders.CommonPrograms;
                    // TODO:NA
                    break;
                case SpecialFolder.CommonStartup:
                    folderGuid = Interop.Shell32.KnownFolders.CommonStartup;
                    // TODO:NA
                    break;
                case SpecialFolder.CommonDesktopDirectory:
                    folderGuid = Interop.Shell32.KnownFolders.PublicDesktop;
                    // Windows.Storage.SystemDataPaths.PublicDesktop
                    break;
                case SpecialFolder.CommonTemplates:
                    folderGuid = Interop.Shell32.KnownFolders.CommonTemplates;
                    // TODO:NA
                    break;
                case SpecialFolder.CommonVideos:
                    folderGuid = Interop.Shell32.KnownFolders.PublicVideos;
                    // Windows.Storage.SystemDataPaths.PublicVideos
                    break;
                case SpecialFolder.Fonts:
                    folderGuid = Interop.Shell32.KnownFolders.Fonts;
                    // TODO:NA
                    break;
                case SpecialFolder.NetworkShortcuts:
                    folderGuid = Interop.Shell32.KnownFolders.NetHood;
                    // TODO:NA
                    break;
                case SpecialFolder.PrinterShortcuts:
                    folderGuid = Interop.Shell32.KnownFolders.PrintersFolder;
                    // TODO:NA
                    break;
                case SpecialFolder.UserProfile:
                    folderGuid = Interop.Shell32.KnownFolders.Profile;
                    // Windows.Storage.UserDataPaths.Profile
                    break;
                case SpecialFolder.CommonProgramFilesX86:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramFilesCommonX86;
                    // TODO:NA
                    break;
                case SpecialFolder.ProgramFilesX86:
                    folderGuid = Interop.Shell32.KnownFolders.ProgramFilesX86;
                    // TODO:NA
                    break;
                case SpecialFolder.Resources:
                    folderGuid = Interop.Shell32.KnownFolders.ResourceDir;
                    // TODO:NA
                    break;
                case SpecialFolder.LocalizedResources:
                    folderGuid = Interop.Shell32.KnownFolders.LocalizedResourcesDir;
                    // TODO:NA
                    break;
                case SpecialFolder.SystemX86:
                    folderGuid = Interop.Shell32.KnownFolders.SystemX86;
                    // Windows.Storage.SystemDataPaths.SystemX86
                    break;
                case SpecialFolder.Windows:
                    folderGuid = Interop.Shell32.KnownFolders.Windows;
                    // Windows.Storage.SystemDataPaths.Windows
                    break;
                default:
                    return string.Empty;
            }

            return GetKnownFolderPath(folderGuid, option);
        }

        private static string GetKnownFolderPath(string folderGuid, SpecialFolderOption option)
        {
            Guid folderId = new Guid(folderGuid);

            string path;
            int hr = Interop.Shell32.SHGetKnownFolderPath(folderId, (uint)option, IntPtr.Zero, out path);
            if (hr != 0) // Not S_OK
            {
                return string.Empty;
            }

            return path;
        }

        private static bool Is64BitOperatingSystemWhen32BitProcess
        {
            get
            {
                bool isWow64;
                return Interop.Kernel32.IsWow64Process(Interop.Kernel32.GetCurrentProcess(), out isWow64) && isWow64;
            }
        }

        public static string MachineName
        {
            get
            {
                string name = Interop.Kernel32.GetComputerName();
                if (name == null)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ComputerName);
                }
                return name;
            }
        }

        private static unsafe Lazy<OperatingSystem> s_osVersion = new Lazy<OperatingSystem>(() =>
        {
            var version = new Interop.Kernel32.OSVERSIONINFOEX { dwOSVersionInfoSize = sizeof(Interop.Kernel32.OSVERSIONINFOEX) };
            if (!Interop.Kernel32.GetVersionExW(ref version))
            {
                throw new InvalidOperationException(SR.InvalidOperation_GetVersion);
            }

            return new OperatingSystem(
                PlatformID.Win32NT,
                new Version(version.dwMajorVersion, version.dwMinorVersion, version.dwBuildNumber, (version.wServicePackMajor << 16) | version.wServicePackMinor),
                Marshal.PtrToStringUni((IntPtr)version.szCSDVersion));
        });

        public static int ProcessorCount
        {
            get
            {
                // First try GetLogicalProcessorInformationEx, caching the result as desktop/coreclr does.
                // If that fails for some reason, fall back to a non-cached result from GetSystemInfo.
                // (See SystemNative::GetProcessorCount in coreclr for a comparison.)
                int pc = s_processorCountFromGetLogicalProcessorInformationEx.Value;
                return pc != 0 ? pc : ProcessorCountFromSystemInfo;
            }
        }

        private static readonly unsafe Lazy<int> s_processorCountFromGetLogicalProcessorInformationEx = new Lazy<int>(() =>
        {
            // Determine how much size we need for a call to GetLogicalProcessorInformationEx
            uint len = 0;
            if (!Interop.Kernel32.GetLogicalProcessorInformationEx(Interop.Kernel32.LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup, IntPtr.Zero, ref len) &&
                Marshal.GetLastWin32Error() == Interop.Errors.ERROR_INSUFFICIENT_BUFFER)
            {
                // Allocate that much space
                Debug.Assert(len > 0);
                var buffer = new byte[len];
                fixed (byte* bufferPtr = buffer)
                {
                    // Call GetLogicalProcessorInformationEx with the allocated buffer
                    if (Interop.Kernel32.GetLogicalProcessorInformationEx(Interop.Kernel32.LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup, (IntPtr)bufferPtr, ref len))
                    {
                        // Walk each SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX in the buffer, where the Size of each dictates how
                        // much space it's consuming.  For each group relation, count the number of active processors in each of its group infos.
                        int processorCount = 0;
                        byte* ptr = bufferPtr, endPtr = bufferPtr + len;
                        while (ptr < endPtr)
                        {
                            var current = (Interop.Kernel32.SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*)ptr;
                            if (current->Relationship == Interop.Kernel32.LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup)
                            {
                                Interop.Kernel32.PROCESSOR_GROUP_INFO* groupInfo = &current->Group.GroupInfo;
                                int groupCount = current->Group.ActiveGroupCount;
                                for (int i = 0; i < groupCount; i++)
                                {
                                    processorCount += (groupInfo + i)->ActiveProcessorCount;
                                }
                            }
                            ptr += current->Size;
                        }
                        return processorCount;
                    }
                }
            }

            return 0;
        });

        public static string SystemDirectory
        {
            get
            {
                StringBuilder sb = StringBuilderCache.Acquire(PathInternal.MaxShortPath);
                if (Interop.Kernel32.GetSystemDirectoryW(sb, PathInternal.MaxShortPath) == 0)
                {
                    StringBuilderCache.Release(sb);
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
                return StringBuilderCache.GetStringAndRelease(sb);
            }
        }

        public static string UserName
        {
            get
            {
                string username = "Windows User";
                GetUserName(ref username);
                return username;
            }
        }

        static partial void GetUserName(ref string username);

        public static string UserDomainName
        {
            get
            {
                string userDomainName = "Windows Domain";
                GetDomainName(ref userDomainName);
                return userDomainName;
            }
        }

        static partial void GetDomainName(ref string userDomainName);
    }
}
