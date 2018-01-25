// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Enumeration;
using Xunit;

namespace System.IO.Tests.FileEnumerable
{
    public class Construction : FileSystemTest
    {
        [Fact]
        public void NullTransformThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("transform",
                () => new FileSystemEnumerable<string, string>(TestDirectory, transform: null, shouldIncludePredicate: (ref FileSystemEntry entry, string state) => true));
        }

        [Fact]
        public void NullPredicateThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("shouldIncludePredicate",
                () => new FileSystemEnumerable<string, string>(TestDirectory, transform: (ref FileSystemEntry entry, string state) => string.Empty, shouldIncludePredicate: null));
        }
    }
}
