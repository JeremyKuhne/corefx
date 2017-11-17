// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.IO
{
    internal class FindOperation<T> : IEnumerable<T>
    {
        private string _directory;
        private bool _recursive;
        private IFindTransform<T> _transform;
        private IFindFilter _filter;

        /// <summary>
        /// Encapsulates a find operation. Will strip trailing separator as FindFile will not take it.
        /// </summary>
        /// <param name="directory">The directory to search in.</param>
        /// <param name="nameFilter">
        /// The filter. Can contain wildcards, full details can be found at
        /// <a href="https://msdn.microsoft.com/en-us/library/ff469270.aspx">[MS-FSA] 2.1.4.4 Algorithm for Determining if a FileName Is in an Expression</a>.
        /// </param>
        /// <param name="getAlternateName">Returns the alternate (short) file name in the FindResult.AlternateName field if it exists.</param>
        public FindOperation(
            string directory,
            string nameFilter = "*",
            bool recursive = false,
            IFindTransform<T> findTransform = null,
            IFindFilter findFilter = null)
        {
            _directory = directory;
            _recursive = recursive;
            if (findTransform == null)
            {
                if (typeof(T) == typeof(string))
                    findTransform = (IFindTransform<T>)FindTransforms.UserFullPath;
                else
                    throw new ArgumentException(nameof(findTransform), $"No default filter for {typeof(T)}");
            }
            _transform = findTransform;
            _filter = findFilter ?? new FindFilters.Two(FindFilters.NotCurentOrPreviousDirectory, FindFilters.DosFileNameMatch(nameFilter));
        }

        public IEnumerator<T> GetEnumerator() => new FindEnumerator(CreateDirectoryHandle(_directory), this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Simple wrapper to allow creating a file handle for an existing directory.
        /// </summary>
        public static IntPtr CreateDirectoryHandle(string path)
        {
            IntPtr handle = Interop.Kernel32.CreateFile(
                path,
                Interop.Kernel32.FileOperations.FILE_LIST_DIRECTORY,
                FileShare.ReadWrite | FileShare.Delete,
                FileMode.Open,
                Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS);

            if (handle == IntPtr.Zero)
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error(), path);

            return handle;
        }

        private unsafe class FindEnumerator : CriticalFinalizerObject, IEnumerator<T>
        {
            private Interop.NtDll.FILE_FULL_DIR_INFORMATION* _info;
            private byte[] _buffer;
            private IntPtr _directory;
            private string _path;
            private bool _lastEntryFound;
            private Queue<(IntPtr Handle, string Path)> _pending;
            private FindOperation<T> _findOperation;
            private GCHandle _pinnedBuffer;

            public FindEnumerator(IntPtr directory, FindOperation<T> findOperation)
            {
                // Set the handle first to ensure we always dispose of it
                _directory = directory;
                _path = findOperation._directory;
                _buffer = ArrayPool<byte>.Shared.Rent(4096);
                _pinnedBuffer = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
                _findOperation = findOperation;
                if (findOperation._recursive)
                    _pending = new Queue<(IntPtr, string)>();
            }

            public T Current
            {
                get
                {
                    RawFindData findData = new RawFindData(_info, _path);
                    return _findOperation._transform.TransformResult(ref findData);
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_lastEntryFound)
                    return false;

                RawFindData findData = default;
                do
                {
                    FindNextFile();
                    if (!_lastEntryFound && _info != null)
                    {
                        if (_pending != null && (_info->FileAttributes & FileAttributes.Directory) != 0
                            && !FindFilters.IsRelativeDirectory(_info->FileName))
                        {
                            // Stash directory to recurse into
                            string fileName = new string(_info->FileName);
                            string subDirectory = PathHelpers.CombineNoChecks(_path, fileName);
                            _pending.Enqueue(ValueTuple.Create(
                                CreateDirectoryHandle(subDirectory),
                                subDirectory));
                        }

                        findData = new RawFindData(_info, _path);
                    }
                } while (!_lastEntryFound && !_findOperation._filter.Match(ref findData));

                return !_lastEntryFound;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void GetData()
            {
                if (!Interop.Kernel32.GetFileInformationByHandleEx(
                   _directory,
                   Interop.Kernel32.FILE_INFO_BY_HANDLE_CLASS.FileIdBothDirectoryInfo,
                   _buffer,
                   (uint)_buffer.Length))
                {
                    int error = Marshal.GetLastWin32Error();
                    switch (error)
                    {
                        case Interop.Errors.ERROR_NO_MORE_FILES:
                            NoMoreFiles();
                            return;
                        default:
                            throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error(), _path);
                    }
                }
            }

            private unsafe void FindNextFile()
            {
                Interop.NtDll.FILE_FULL_DIR_INFORMATION* info = _info;
                if (info != null && info->NextEntryOffset != 0)
                {
                    // We're already in a buffer and have another entry
                    _info = (Interop.NtDll.FILE_FULL_DIR_INFORMATION*)((byte*)info + info->NextEntryOffset);
                    return;
                }

                GetData();

                _info = (Interop.NtDll.FILE_FULL_DIR_INFORMATION*)_pinnedBuffer.AddrOfPinnedObject();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void NoMoreFiles()
            {
                _info = null;
                if (_pending == null || _pending.Count == 0)
                {
                    _lastEntryFound = true;
                }
                else
                {
                    // Grab the next directory to parse
                    var next = _pending.Dequeue();
                    Interop.Kernel32.CloseHandle(_directory);
                    _directory = next.Handle;
                    _path = next.Path;
                    FindNextFile();
                }
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            protected void Dispose(bool disposing)
            {
                byte[] buffer = Interlocked.Exchange(ref _buffer, null);
                if (buffer != null)
                    ArrayPool<byte>.Shared.Return(buffer);

                var queue = Interlocked.Exchange(ref _pending, null);
                if (queue != null)
                {
                    while (queue.Count > 0)
                        Interop.Kernel32.CloseHandle(queue.Dequeue().Handle);
                }

                Interop.Kernel32.CloseHandle(_directory);
            }

            ~FindEnumerator()
            {
                Dispose(disposing: false);
            }
        }
    }
}
