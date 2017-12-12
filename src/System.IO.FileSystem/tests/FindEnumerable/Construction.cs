// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Threading;
using Xunit;

namespace System.IO.Tests.FileEnumerable
{
    public class Construction : FileSystemTest
    {
        [Fact]
        public void NullTransformThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("transform",
                () => new FindEnumerable<string, string>(TestDirectory, transform: null, predicate: FindPredicates.IsDirectory));
        }

        [Fact]
        public void NullPredicateThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("predicate",
                () => new FindEnumerable<string, string>(TestDirectory, transform: FindTransforms.AsUserFullPath, predicate: null));
        }

        [Fact]
        public void DoesNotLockWithFlag()
        {
            string directory = Directory.CreateDirectory(GetTestFilePath()).FullName;

            object lockField = null;
            var enumerable = new FindEnumerable<string, string>(
                directory,
                FindTransforms.AsUserFullPath,
                (ref FindData<string> findData) =>
                {
                    Assert.NotNull(lockField);
                    Assert.False(Monitor.IsEntered(lockField));
                    return false;
                },
                options: FindOptions.AvoidLocking);
            lockField = enumerable.GetType().GetField("_lock", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerable);
        }

        [Fact]
        public void LocksWithoutFlag()
        {
            string directory = Directory.CreateDirectory(GetTestFilePath()).FullName;

            object lockField = null;
            var enumerable = new FindEnumerable<string, string>(
                directory,
                FindTransforms.AsUserFullPath,
                (ref FindData<string> findData) =>
                {
                    Assert.NotNull(lockField);
                    Assert.True(Monitor.IsEntered(lockField));
                    return false;
                });
            lockField = enumerable.GetType().GetField("_lock", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerable);
        }
    }
}
