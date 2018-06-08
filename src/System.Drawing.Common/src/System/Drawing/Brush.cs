// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    public abstract class Brush : MarshalByRefObject, ICloneable, IDisposable
    {
        public abstract object Clone();

        protected internal void SetNativeBrush(IntPtr brush) => SetNativeBrushInternal(brush);
        internal void SetNativeBrushInternal(IntPtr brush) => NativeBrush = brush;

        // Handle to native GDI+ brush object to be used on demand.
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        internal IntPtr NativeBrush { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
#if FINALIZATION_WATCH
            if (!disposing && nativeBrush != IntPtr.Zero )
                Debug.WriteLine("**********************\nDisposed through finalization:\n" + allocationSite);
#endif

            if (NativeBrush != IntPtr.Zero)
            {
                try
                {
#if DEBUG
                    int status =
#endif
                    SafeNativeMethods.Gdip.GdipDeleteBrush(new HandleRef(this, NativeBrush));
#if DEBUG
                    Debug.Assert(status == SafeNativeMethods.Gdip.Ok, "GDI+ returned an error status: " + status.ToString(CultureInfo.InvariantCulture));
#endif
                }
                catch (Exception ex) when (!ClientUtils.IsSecurityOrCriticalException(ex))
                {
                    // Catch all non fatal exceptions. This includes exceptions like EntryPointNotFoundException, that is thrown
                    // on Windows Nano.
                }
                finally
                {
                    NativeBrush = IntPtr.Zero;
                }
            }
        }

        ~Brush() => Dispose(false);
    }
}
