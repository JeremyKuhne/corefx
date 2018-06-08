// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Drawing.Imaging;

namespace System.Drawing
{
    /// <summary>
    /// Animates one or more images that have time-based frames. This file contains the nested ImageInfo class
    /// - See ImageAnimator.cs for the definition of the outer class.
    /// </summary>
    public sealed partial class ImageAnimator
    {
        /// <summary>
        /// ImageAnimator nested helper class used to store extra image state info.
        /// </summary>
        private class ImageInfo
        {
            private const int PropertyTagFrameDelay = 0x5100;
            private int _frame;
            private readonly int[] _frameDelay;

            public ImageInfo(Image image)
            {
                Image = image;
                Animated = CanAnimate(image);

                if (Animated)
                {
                    FrameCount = image.GetFrameCount(FrameDimension.Time);

                    PropertyItem frameDelayItem = image.GetPropertyItem(PropertyTagFrameDelay);

                    // If the image does not have a frame delay, we just return 0.
                    //
                    if (frameDelayItem != null)
                    {
                        // Convert the frame delay from byte[] to int
                        byte[] values = frameDelayItem.Value;
                        Debug.Assert(values.Length == 4 * FrameCount, "PropertyItem has invalid value byte array");
                        _frameDelay = new int[FrameCount];
                        for (int i = 0; i < FrameCount; ++i)
                        {
                            _frameDelay[i] = values[i * 4] + 256 * values[i * 4 + 1] + 256 * 256 * values[i * 4 + 2] + 256 * 256 * 256 * values[i * 4 + 3];
                        }
                    }
                }
                else
                {
                    FrameCount = 1;
                }
                if (_frameDelay == null)
                {
                    _frameDelay = new int[FrameCount];
                }
            }

            /// <summary>
            /// Whether the image supports animation.
            /// </summary>
            public bool Animated { get; }

            /// <summary>
            /// The current frame.
            /// </summary>
            public int Frame
            {
                get
                {
                    return _frame;
                }
                set
                {
                    if (_frame != value)
                    {
                        if (value < 0 || value >= FrameCount)
                        {
                            throw new ArgumentException(SR.Format(SR.InvalidFrame), nameof(value));
                        }

                        if (Animated)
                        {
                            _frame = value;
                            FrameDirty = true;

                            OnFrameChanged(EventArgs.Empty);
                        }
                    }
                }
            }

            /// <summary>
            /// The current frame has not been updated.
            /// </summary>
            public bool FrameDirty { get; private set; }

            public EventHandler FrameChangedHandler { get; set; }

            /// <summary>
            /// The number of frames in the image.
            /// </summary>
            public int FrameCount { get; }

            /// <summary>
            /// The delay associated with the frame at the specified index.
            /// </summary>
            public int FrameDelay(int frame) => _frameDelay[frame];

            internal int FrameTimer { get; set; }

            /// <summary>
            /// The image this object wraps.
            /// </summary>
            internal Image Image { get; }

            /// <summary>
            /// Selects the current frame as the active frame in the image.
            /// </summary>
            internal void UpdateFrame()
            {
                if (FrameDirty)
                {
                    Image.SelectActiveFrame(FrameDimension.Time, Frame);
                    FrameDirty = false;
                }
            }

            /// <summary>
            /// Raises the FrameChanged event.
            /// </summary>
            protected void OnFrameChanged(EventArgs e) => FrameChangedHandler?.Invoke(Image, e);
        }
    }
}
