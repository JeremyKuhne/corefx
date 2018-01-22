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
    }
}
