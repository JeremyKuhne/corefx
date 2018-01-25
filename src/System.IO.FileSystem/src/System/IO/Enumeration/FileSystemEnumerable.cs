// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Enumeration
{
    public class FileSystemEnumerable<TResult, TState> : FileSystemEnumerableBase<TResult>
    {
        private readonly FindTransform _transform;
        private readonly FindPredicate _predicate;

        public FileSystemEnumerable(string directory, FindTransform transform, FindPredicate predicate)
            : base(directory)
        {
            _transform = transform ?? throw new ArgumentNullException(nameof(transform));
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        protected override FileSystemEnumerableBase<TResult> Clone()
            => new FileSystemEnumerable<TResult, TState>(OriginalPath, _transform, _predicate)
            {
                RecursePredicate = RecursePredicate,
            };

        public FindPredicate RecursePredicate { get; set; }
        public TState State { get; set; }

        protected override bool ShouldIncludeEntry(ref FileSystemEntry entry) => _predicate(ref entry, State);
        protected override TResult TransformEntry(ref FileSystemEntry entry) => _transform(ref entry, State);
        protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry) => RecursePredicate?.Invoke(ref entry, State) ?? true;

        /// <summary>
        /// Delegate for filtering out find results.
        /// </summary>
        public delegate bool FindPredicate(ref FileSystemEntry entry, TState state);

        /// <summary>
        /// Delegate for transforming raw find data into a result.
        /// </summary>
        public delegate TResult FindTransform(ref FileSystemEntry entry, TState state);
    }
}
