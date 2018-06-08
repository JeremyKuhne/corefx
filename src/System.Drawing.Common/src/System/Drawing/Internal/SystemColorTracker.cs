// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Win32;

namespace System.Drawing.Internal
{
    // Keeps track of objects that need to be notified of system color change events.
    // Mostly this means maintaining a list of weak references.
    internal static class SystemColorTracker
    {
        // when I tried the self host, it went over 500 but never over 1000.
        private const int INITIAL_SIZE = 200;
        // If it gets this big, I seriously miscalculated the performance of this object.
        private const int WARNING_SIZE = 100000;
        private const float EXPAND_THRESHOLD = 0.75f;
        private const int EXPAND_FACTOR = 2;

        private static WeakReference[] s_list = new WeakReference[INITIAL_SIZE];
        private static int s_count = 0;
        private static bool s_addedTracker;
        private static readonly object s_lockObject = new object();

        internal static void Add(ISystemColorTracker obj)
        {
            lock (s_lockObject)
            {
                Debug.Assert(s_list != null, "List is null");
                Debug.Assert(s_list.Length > 0, "INITIAL_SIZE was initialized after list");

                if (s_list.Length == s_count)
                {
                    GarbageCollectList();
                }

                if (!s_addedTracker)
                {
                    s_addedTracker = true;
                    SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(OnUserPreferenceChanged);
                }

                // Strictly speaking, we should grab a lock on this class.  But since the chances
                // of a problem are so low, the consequences so minimal (something will get accidentally dropped
                // from the list), and the performance of locking so lousy, we'll risk it.
                int index = s_count;
                s_count++;

                // COM+ takes forever to Finalize() weak references, so it pays to reuse them.
                if (s_list[index] == null)
                    s_list[index] = new WeakReference(obj);
                else
                {
                    Debug.Assert(s_list[index].Target == null, "Trying to reuse a weak reference that isn't broken yet: list[" + index + "], length =" + s_list.Length);
                    s_list[index].Target = obj;
                }
            }
        }

        private static void CleanOutBrokenLinks()
        {
            // Partition the list -- valid references in the low indices, broken references in the high indices.
            // This is taken straight out of Sedgewick (p. 118 on quicksort).

            // Basic idea is to find a broken reference on the left side of the list, and swap it with
            // a valid reference on the right
            int right = s_list.Length - 1;
            int left = 0;

            int length = s_list.Length;

            // Loop invariant: everything to the left of "left" is a valid reference,
            // and anything to the right of "right" is broken.
            for (;;)
            {
                while (left < length && s_list[left].Target != null)
                    left++;
                while (right >= 0 && s_list[right].Target == null)
                    right--;

                if (left >= right)
                {
                    s_count = left;
                    break;
                }

                WeakReference temp = s_list[left];
                s_list[left] = s_list[right];
                s_list[right] = temp;

                left++;
                right--;
            }

            Debug.Assert(s_count >= 0 && s_count <= s_list.Length, "count not a legal index into list");

#if DEBUG
            // Check loop invariant.

            // We'd like to assert that any index < count contains a valid pointer,
            // but since garbage collection can happen at any time, it may have been broken
            // after we partitioned it.
            //
            // for (int i = 0; i < count; i++) {
            //     Debug.Assert(list[i].Target != null, "Null found on the left side of the list");
            // }

            for (int i = s_count; i < s_list.Length; i++)
            {
                Debug.Assert(s_list[i].Target == null, "Partitioning didn't work");
            }
#endif
        }

        private static void GarbageCollectList()
        {
            CleanOutBrokenLinks();

            if (s_count / (float)s_list.Length > EXPAND_THRESHOLD)
            {
                WeakReference[] newList = new WeakReference[s_list.Length * EXPAND_FACTOR];
                s_list.CopyTo(newList, 0);
                s_list = newList;

                Debug.Assert(s_list.Length < WARNING_SIZE, "SystemColorTracker is using way more memory than expected.");
            }
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {

            // Update pens and brushes
            if (e.Category == UserPreferenceCategory.Color)
            {
                for (int i = 0; i < s_count; i++)
                {
                    Debug.Assert(s_list[i] != null, "null value in active part of list");
                    ISystemColorTracker tracker = (ISystemColorTracker)s_list[i].Target;
                    if (tracker != null)
                    {
                        // If object still around
                        tracker.OnSystemColorChanged();
                    }
                }
            }
        }
    }
}
