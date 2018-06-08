// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing.Drawing2D;

namespace System.Drawing
{
    /// <summary>
    /// Contains information about the context of a Graphics object.
    /// </summary>
    internal class GraphicsContext : IDisposable
    {
        private GraphicsContext()
        {
        }

        public GraphicsContext(Graphics g)
        {
            Matrix transform = g.Transform;
            if (!transform.IsIdentity)
            {
                float[] elements = transform.Elements;
                TransformOffset = new PointF(elements[4], elements[5]);
            }
            transform.Dispose();

            Region clip = g.Clip;
            if (clip.IsInfinite(g))
            {
                clip.Dispose();
            }
            else
            {
                Clip = clip;
            }
        }

        /// <summary>
        /// Disposes this and all contexts up the stack.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes this and all contexts up the stack.
        /// </summary>
        public void Dispose(bool disposing)
        {
            if (Next != null)
            {
                // Dispose all contexts up the stack since they are relative to this one and its state will be invalid.
                Next.Dispose();
                Next = null;
            }

            if (Clip != null)
            {
                Clip.Dispose();
                Clip = null;
            }
        }

        /// <summary>
        /// The state id representing the GraphicsContext.
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// The translate transform in the GraphicsContext.
        /// </summary>
        public PointF TransformOffset { get; private set; }

        /// <summary>
        /// The clipping region the GraphicsContext.
        /// </summary>
        public Region Clip { get; private set; }

        /// <summary>
        /// The next GraphicsContext object in the stack.
        /// </summary>
        public GraphicsContext Next { get; set; }

        /// <summary>
        /// The previous GraphicsContext object in the stack.
        /// </summary>
        public GraphicsContext Previous { get; set; }

        /// <summary>
        /// Determines whether this context is cumulative or not.  See filed for more info.
        /// </summary>
        public bool IsCumulative { get; set; }
    }
}
