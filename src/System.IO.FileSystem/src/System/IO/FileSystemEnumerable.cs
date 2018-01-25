// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    public class FileSystemEnumerable<TResult, TState> : FileSystemEnumerableBase<TResult, TState>
    {
        private readonly FindTransform<TResult, TState> _transform;
        private readonly FindPredicate<TState> _predicate;

        public FileSystemEnumerable(string directory, FindTransform<TResult, TState> transform, FindPredicate<TState> predicate)
            : base(directory)
        {
            _transform = transform ?? throw new ArgumentNullException(nameof(transform));
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        protected override FileSystemEnumerableBase<TResult, TState> Clone()
            => new FileSystemEnumerable<TResult, TState>(OriginalPath, _transform, _predicate)
            {
                RecursePredicate = RecursePredicate,
            };

        public FindPredicate<TState> RecursePredicate { get; set; }

        public override bool AcceptEntry(ref FileSystemEntry entry) => _predicate(ref entry, State);
        public override TResult TransformEntry(ref FileSystemEntry entry) => _transform(ref entry, State);
        public override bool RecurseEntry(ref FileSystemEntry entry) => RecursePredicate?.Invoke(ref entry, State) ?? true;
    }
}
