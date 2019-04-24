// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Json.Serialization
{
    internal readonly struct PropertyRef
    {
        // The length of the property name embedded in the key (in bytes).
        private const int PropertyNameKeyLength = 6;

        public PropertyRef(ulong key, JsonPropertyInfo info)
        {
            Key = key;
            Info = info;
        }

        /// <summary>
        /// Generate a hash value for the given property name.
        /// </summary>
        public static ulong GetKey(ReadOnlySpan<byte> propertyName)
        {
            ulong key;
            int length = propertyName.Length;

            // Embed the propertyName in the first 6 bytes of the key.
            if (length > 3)
            {
                key = MemoryMarshal.Read<uint>(propertyName);
                if (length > 4)
                {
                    key |= (ulong)propertyName[4] << 32;
                }
                if (length > 5)
                {
                    key |= (ulong)propertyName[5] << 40;
                }
            }
            else if (length > 1)
            {
                key = MemoryMarshal.Read<ushort>(propertyName);
                if (length > 2)
                {
                    key |= (ulong)propertyName[2] << 16;
                }
            }
            else if (length == 1)
            {
                key = propertyName[0];
            }
            else
            {
                // An empty name is valid.
                key = 0;
            }

            // Embed the propertyName length in the last two bytes.
            key |= (ulong)propertyName.Length << 48;
            return key;
        }

        // The first 6 bytes are the first part of the name and last 2 bytes are the name's length.
        public readonly ulong Key;

        public readonly JsonPropertyInfo Info;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(PropertyRef other)
        {
            if (Key == other.Key)
            {
                if (Info.NameUsedToCompare.Length <= PropertyNameKeyLength ||
                    Info.NameUsedToCompare.SequenceEqual(other.Info.NameUsedToCompare))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ReadOnlySpan<byte> propertyName, ulong key)
        {
            if (key == Key)
            {
                if (propertyName.Length <= PropertyNameKeyLength ||
                    // We compare the whole name, although we could skip the first 6 bytes (but it's likely not any faster)
                    propertyName.SequenceEqual(Info.NameUsedToCompare))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
