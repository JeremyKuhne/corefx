// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Defines a map for converting colors.
    /// </summary>
    public sealed class ColorMap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='ColorMap'/> class.
        /// </summary>
        public ColorMap()
        {
            OldColor = new Color();
            NewColor = new Color();
        }

        /// <summary>
        /// Specifies the existing <see cref='Color'/> to be converted.
        /// </summary>
        public Color OldColor { get; set; }
        /// <summary>
        /// Specifies the new <see cref='Color'/> to which to convert.
        /// </summary>
        public Color NewColor { get; set; }
    }
}
