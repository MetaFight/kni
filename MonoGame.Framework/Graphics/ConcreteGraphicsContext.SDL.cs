﻿// Copyright (C)2022 Nick Kastellanos

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.OpenGL;


namespace Microsoft.Xna.Platform.Graphics
{
    internal sealed class ConcreteGraphicsContext : ConcreteGraphicsContextGL
    {
        private IntPtr _glContext;

        internal DrawBuffersEnum[] _drawBuffers;

        internal IntPtr GlContext { get { return _glContext; } }

        internal ConcreteGraphicsContext(GraphicsDevice device)
            : base(device)
        {
            _glContext = Sdl.GL.CreateGLContext(device.PresentationParameters.DeviceWindowHandle);

            // GL entry points must be loaded after the GL context creation, otherwise some Windows drivers will return only GL 1.3 compatible functions
            try
            {
                GL.LoadEntryPoints();
            }
            catch (EntryPointNotFoundException)
            {
                throw new PlatformNotSupportedException(
                    "MonoGame requires OpenGL 3.0 compatible drivers, or either ARB_framebuffer_object or EXT_framebuffer_object extensions. " +
                    "Try updating your graphics drivers.");
            }

            MakeCurrent(device.PresentationParameters.DeviceWindowHandle);
            int swapInterval = ConcreteGraphicsContext.ToGLSwapInterval(device.PresentationParameters.PresentationInterval);
            Sdl.GL.SetSwapInterval(swapInterval);
        }

        public void MakeCurrent(IntPtr winHandle)
        {
            Sdl.GL.MakeCurrent(winHandle, _glContext);
        }

        /// <summary>
        /// Converts <see cref="PresentInterval"/> to OpenGL swap interval.
        /// </summary>
        /// <returns>A value according to EXT_swap_control</returns>
        /// <param name="interval">The <see cref="PresentInterval"/> to convert.</param>
        internal static int ToGLSwapInterval(PresentInterval interval)
        {
            // See http://www.opengl.org/registry/specs/EXT/swap_control.txt
            // and https://www.opengl.org/registry/specs/EXT/glx_swap_control_tear.txt
            // OpenTK checks for EXT_swap_control_tear:
            // if supported, a swap interval of -1 enables adaptive vsync;
            // otherwise -1 is converted to 1 (vsync enabled.)

            switch (interval)
            {
                case PresentInterval.Immediate:
                    return 0;
                case PresentInterval.One:
                    return 1;
                case PresentInterval.Two:
                    return 2;
                case PresentInterval.Default:

                default:
                    return -1;
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThrowIfDisposed();

            }

            if (_glContext != IntPtr.Zero)
                Sdl.GL.DeleteContext(_glContext);
            _glContext = IntPtr.Zero;

            base.Dispose(disposing);
        }

    }
}
