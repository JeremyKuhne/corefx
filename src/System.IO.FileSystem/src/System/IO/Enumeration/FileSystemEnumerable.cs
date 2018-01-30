// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace System.IO.Enumeration
{
    public class FileSystemEnumerable<TResult, TState> : IEnumerable<TResult>
    {
        private DelegateEnumerator _enumerator;
        private readonly FindTransform _transform;
        private readonly FindPredicate _shouldInclude;
        private readonly EnumerationOptions _options;
        private readonly string _directory;

        public FileSystemEnumerable(string directory, FindTransform transform, FindPredicate shouldIncludePredicate = null)
            : this(directory, transform, shouldIncludePredicate, EnumerationOptions.Default)
        {
        }

        public FileSystemEnumerable(string directory, FindTransform transform, FindPredicate shouldIncludePredicate, EnumerationOptions options)
        {
            _transform = transform ?? throw new ArgumentNullException(nameof(transform));
            _shouldInclude = shouldIncludePredicate ?? throw new ArgumentNullException(nameof(shouldIncludePredicate));
            _options = options;
            _directory = directory;

            // We need to create the enumerator up front to ensure that we throw I/O exceptions for
            // the root directory on creation of the enumerable.
            _enumerator = new DelegateEnumerator(this);
        }

        public FindPredicate ShouldRecursePredicate { get; set; }
        public TState State { get; set; }

        public IEnumerator<TResult> GetEnumerator()
        {
            var enumerator = Interlocked.Exchange(ref _enumerator, null);
            if (enumerator == null)
                enumerator = new DelegateEnumerator(this);

            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Delegate for filtering out find results.
        /// </summary>
        public delegate bool FindPredicate(in FileSystemEntry entry, TState state);

        /// <summary>
        /// Delegate for transforming raw find data into a result.
        /// </summary>
        public delegate TResult FindTransform(in FileSystemEntry entry, TState state);

        private class DelegateEnumerator : FileSystemEnumerator<TResult>
        {
            private readonly FileSystemEnumerable<TResult, TState> _enumerable;

            public DelegateEnumerator(FileSystemEnumerable<TResult, TState> enumerable)
                : base(enumerable._directory, enumerable._options)
            {
                _enumerable = enumerable;
            }

            protected override bool ShouldIncludeEntry(in FileSystemEntry entry) => _enumerable._shouldInclude(in entry, _enumerable.State);
            protected override TResult TransformEntry(in FileSystemEntry entry) => _enumerable._transform(in entry, _enumerable.State);
            protected override bool ShouldRecurseIntoEntry(in FileSystemEntry entry)
                => _enumerable.ShouldRecursePredicate?.Invoke(in entry, _enumerable.State) ?? true;
        }
    }
}
