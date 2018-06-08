// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <summary>
    /// Specifies print preview information for a single page. This class cannot be inherited.
    /// </summary>
    public sealed class PreviewPageInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='PreviewPageInfo'/> class.
        /// </summary>
        public PreviewPageInfo(Image image, Size physicalSize)
        {
            Image = image;
            PhysicalSize = physicalSize;
        }

        /// <summary>
        /// Gets the image of the printed page.
        /// </summary>
        public Image Image { get; }

        /// <summary>
        /// Gets the size of the printed page, in hundredths of an inch.
        /// </summary>
        public Size PhysicalSize { get; } = Size.Empty;
    }
}
