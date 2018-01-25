// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.IO
{
    public unsafe abstract partial class FileSystemEnumerableBase<TResult, TState> : CriticalFinalizerObject, IEnumerable<TResult>, IEnumerator<TResult>
    {
        private const int StandardBufferSize = 4096;

        // We need to have enough room for at least a single entry. The filename alone can be 512 bytes, we'll ensure we have
        // a reasonable buffer for all of the other metadata as well.
        private const int MinimumBufferSize = 1024;

        private readonly string _originalFullPath;
        protected readonly string OriginalPath;

        private object _lock = new object();
        private int _enumeratorCreated;

        private Interop.NtDll.FILE_FULL_DIR_INFORMATION* _info;
        private TResult _current;

        private byte[] _buffer;
        private IntPtr _directoryHandle;
        private string _currentPath;
        private bool _lastEntryFound;
        private Queue<(IntPtr Handle, string Path)> _pending;
        private GCHandle _pinnedBuffer;

        /// <summary>
        /// Encapsulates a find operation.
        /// </summary>
        /// <param name="directory">The directory to search in.</param>
        public FileSystemEnumerableBase(string directory)
        {
            OriginalPath = directory ?? throw new ArgumentNullException(nameof(directory));
            _originalFullPath = Path.GetFullPath(directory);

            // We'll only suppress the media insertion prompt on the topmost directory
            using (new DisableMediaInsertionPrompt())
            {
                // We need to initialize the directory handle up front to ensure
                // we immediately throw IO exceptions for missing directory/etc.
                _directoryHandle = CreateDirectoryHandle(_originalFullPath);
            }
        }

        public FindOptions Options { get; set; }
        public TState State { get; set; }

        public virtual bool AcceptEntry(ref FileSystemEntry entry) => true;
        public virtual bool RecurseEntry(ref FileSystemEntry entry) => true;
        public virtual TResult TransformEntry(ref FileSystemEntry entry) => default;

        /// <summary>
        /// Simple wrapper to allow creating a file handle for an existing directory.
        /// </summary>
        private IntPtr CreateDirectoryHandle(string path)
        {
            IntPtr handle = Interop.Kernel32.CreateFile_IntPtr(
                path,
                Interop.Kernel32.FileOperations.FILE_LIST_DIRECTORY,
                FileShare.ReadWrite | FileShare.Delete,
                FileMode.Open,
                Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS);

            if (handle == IntPtr.Zero || handle == (IntPtr)(-1))
            {
                // Historically we throw directory not found rather than file not found
                int error = Marshal.GetLastWin32Error();
                switch (error)
                {
                    case Interop.Errors.ERROR_ACCESS_DENIED:
                        if (Options.IgnoreInaccessible)
                        {
                            return IntPtr.Zero;
                        }
                        break;
                    case Interop.Errors.ERROR_FILE_NOT_FOUND:
                        error = Interop.Errors.ERROR_PATH_NOT_FOUND;
                        break;
                }

                throw Win32Marshal.GetExceptionForWin32Error(error, path);
            }

            return handle;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            if (_directoryHandle == IntPtr.Zero)
            {
                // We didn't have rights to access the root directory and the flag was set appropriately
                return Enumerable.Empty<TResult>().GetEnumerator();
            }

            FileSystemEnumerableBase<TResult, TState> enumerator = Interlocked.Exchange(ref _enumeratorCreated, 1) == 0 ? this : Clone();
            enumerator.Initialize();
            return enumerator;
        }

        protected abstract FileSystemEnumerableBase<TResult, TState> Clone();

        private void Initialize()
        {
            _currentPath = _originalFullPath;

            int requestedBufferSize = Options.MinimumBufferSize;
            int bufferSize = requestedBufferSize <= 0 ? StandardBufferSize
                : Math.Max(MinimumBufferSize, requestedBufferSize);

            _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            _pinnedBuffer = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TResult Current => _current;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_lastEntryFound)
                return false;

            bool acquiredLock = false;
            Monitor.Enter(_lock, ref acquiredLock);

            try
            {
                if (_lastEntryFound)
                    return false;

                FileSystemEntry findData = default;
                do
                {
                    FindNextFile();
                    if (!_lastEntryFound && _info != null)
                    {
                        // If needed, stash any subdirectories to process later
                        if (Options.Recurse && (_info->FileAttributes & FileAttributes.Directory) != 0
                            && !PathHelpers.IsDotOrDotDot(_info->FileName)
                            && RecurseEntry(ref findData))
                        {
                            string subDirectory = PathHelpers.CombineNoChecks(_currentPath, _info->FileName);
                            IntPtr subDirectoryHandle = CreateDirectoryHandle(subDirectory);
                            if (subDirectoryHandle != IntPtr.Zero)
                            {
                                try
                                {
                                    if (_pending == null)
                                        _pending = new Queue<(IntPtr, string)>();
                                    _pending.Enqueue((subDirectoryHandle, subDirectory));
                                }
                                catch
                                {
                                    Interop.Kernel32.CloseHandle(subDirectoryHandle);
                                    throw;
                                }
                            }
                        }

                        findData = new FileSystemEntry(_info, _currentPath, _originalFullPath, OriginalPath);
                    }
                } while (!_lastEntryFound && !AcceptEntry(ref findData));

                if (!_lastEntryFound)
                    _current = TransformEntry(ref findData);

                return !_lastEntryFound;
            }
            finally
            {
                if (acquiredLock)
                    Monitor.Exit(_lock);
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

            // We need more data
            if (GetData())
                _info = (Interop.NtDll.FILE_FULL_DIR_INFORMATION*)_pinnedBuffer.AddrOfPinnedObject();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DirectoryFinished()
        {
            _info = null;
            if (_pending == null || _pending.Count == 0)
            {
                _lastEntryFound = true;
            }
            else
            {
                // Grab the next directory to parse
                Interop.Kernel32.CloseHandle(_directoryHandle);
                (_directoryHandle, _currentPath) = _pending.Dequeue();
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
            // It is possible to fail to allocate the lock, but the finalizer will still run
            if (_lock != null)
            {
                bool acquiredLock = false;
                Monitor.Enter(_lock, ref acquiredLock);

                try
                {
                    _lastEntryFound = true;

                    // Don't ever close a valid handle twice as they can be reused- set to zero to ensure this
                    Interop.Kernel32.CloseHandle(_directoryHandle);
                    _directoryHandle = IntPtr.Zero;

                    if (Options.Recurse && _pending != null)
                    {
                        while (_pending.Count > 0)
                            Interop.Kernel32.CloseHandle(_pending.Dequeue().Handle);
                        _pending = null;
                    }

                    if (_pinnedBuffer.IsAllocated)
                        _pinnedBuffer.Free();

                    if (_buffer != null)
                        ArrayPool<byte>.Shared.Return(_buffer);

                    _buffer = null;
                }
                finally
                {
                    if (acquiredLock)
                        Monitor.Exit(_lock);
                }
            }
        }

        ~FileSystemEnumerableBase()
        {
            Dispose(disposing: false);
        }
    }
}
