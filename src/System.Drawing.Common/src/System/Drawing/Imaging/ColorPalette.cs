// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Defines an array of colors that make up a color palette.
    /// </summary>
    public sealed class ColorPalette
    {
        /// <summary>
        /// Specifies how to interpret the color information in the array of colors.
        /// </summary>
        public int Flags { get; private set; }

        /// <summary>
        /// Specifies an array of <see cref='Color'/> objects.
        /// </summary>
        public Color[] Entries { get; private set; }

        internal ColorPalette(int count)
        {
            Entries = new Color[count];
        }

        internal ColorPalette()
        {
            Entries = new Color[1];
        }

        internal void ConvertFromMemory(IntPtr memory)
        {
            // Memory layout is:
            //    UINT Flags
            //    UINT Count
            //    ARGB Entries[size]

            Flags = Marshal.ReadInt32(memory);

            int size;

            size = Marshal.ReadInt32((IntPtr)((long)memory + 4));  // Marshal.SizeOf(size.GetType())

            Entries = new Color[size];

            for (int i = 0; i < size; i++)
            {
                // use Marshal.SizeOf()
                int argb = Marshal.ReadInt32((IntPtr)((long)memory + 8 + i * 4));
                Entries[i] = Color.FromArgb(argb);
            }
        }

        internal IntPtr ConvertToMemory()
        {
            // Memory layout is:
            //    UINT Flags
            //    UINT Count
            //    ARGB Entries[size]

            // use Marshal.SizeOf()
            int length = Entries.Length;
            IntPtr memory = Marshal.AllocHGlobal(checked(4 * (2 + length)));

            Marshal.WriteInt32(memory, 0, Flags);
            // use Marshal.SizeOf()
            Marshal.WriteInt32((IntPtr)checked((long)memory + 4), 0, length);

            for (int i = 0; i < length; i++)
            {
                // use Marshal.SizeOf()
                Marshal.WriteInt32((IntPtr)((long)memory + 4 * (i + 2)), 0, Entries[i].ToArgb());
            }

            return memory;
        }
    }
}
