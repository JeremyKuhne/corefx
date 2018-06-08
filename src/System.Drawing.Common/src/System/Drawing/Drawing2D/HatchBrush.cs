// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.Drawing.Drawing2D
{
    public sealed class HatchBrush : Brush
    {
        public HatchBrush(HatchStyle hatchstyle, Color foreColor) : this(hatchstyle, foreColor, Color.FromArgb(unchecked((int)0xff000000)))
        {
        }

        public HatchBrush(HatchStyle hatchstyle, Color foreColor, Color backColor)
        {
            if (hatchstyle < HatchStyle.Min || hatchstyle > HatchStyle.SolidDiamond)
                throw new ArgumentException(SR.Format(SR.InvalidEnumArgument, nameof(hatchstyle), hatchstyle, nameof(HatchStyle)), nameof(hatchstyle));

            int status = SafeNativeMethods.Gdip.GdipCreateHatchBrush(unchecked((int)hatchstyle), foreColor.ToArgb(), backColor.ToArgb(), out IntPtr nativeBrush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeBrushInternal(nativeBrush);
        }

        internal HatchBrush(IntPtr nativeBrush)
        {
            Debug.Assert(nativeBrush != IntPtr.Zero, "Initializing native brush with null.");
            SetNativeBrushInternal(nativeBrush);
        }

        public override object Clone()
        {
            IntPtr clonedBrush = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out clonedBrush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new HatchBrush(clonedBrush);
        }

        public HatchStyle HatchStyle
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetHatchStyle(new HandleRef(this, NativeBrush), out int hatchStyle);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return (HatchStyle)hatchStyle;
            }
        }

        public Color ForegroundColor
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetHatchForegroundColor(new HandleRef(this, NativeBrush), out int foregroundArgb);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return Color.FromArgb(foregroundArgb);
            }
        }

        public Color BackgroundColor
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetHatchBackgroundColor(new HandleRef(this, NativeBrush), out int backgroundArgb);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return Color.FromArgb(backgroundArgb);
            }
        }
    }
}
