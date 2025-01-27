﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2023 Nick Kastellanos

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Platform.Graphics.Utilities;
using Microsoft.Xna.Platform.Graphics.OpenGL;


namespace Microsoft.Xna.Platform.Graphics
{
    internal class ConcreteTexture3D : ConcreteTexture, ITexture3DStrategy
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _depth;


        internal ConcreteTexture3D(GraphicsContextStrategy contextStrategy, int width, int height, int depth, bool mipMap, SurfaceFormat format,
                                   bool isRenderTarget)
            : base(contextStrategy, format, TextureHelpers.CalculateMipLevels(mipMap, width, height, depth))
        {
            this._width = width;
            this._height = height;
            this._depth = depth;

            System.Diagnostics.Debug.Assert(isRenderTarget);
        }

        internal ConcreteTexture3D(GraphicsContextStrategy contextStrategy, int width, int height, int depth, bool mipMap, SurfaceFormat format)
            : base(contextStrategy, format, TextureHelpers.CalculateMipLevels(mipMap, width, height, depth))
        {
            this._width = width;
            this._height = height;
            this._depth = depth;

            this.PlatformConstructTexture3D(contextStrategy, width, height, depth, mipMap, format);
        }


        #region ITexture3DStrategy
        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        public int Depth { get { return _depth; } }

        public void SetData<T>(int level, int left, int top, int right, int bottom, int front, int back,
                               T[] data, int startIndex, int elementCount)
            where T : struct
        {
            int width = right - left;
            int height = bottom - top;
            int depth = back - front;

            Threading.EnsureMainThread();

            {
                var GL = ((IPlatformGraphicsContext)base.GraphicsDeviceStrategy.CurrentContext).Strategy.ToConcrete<ConcreteGraphicsContextGL>().GL;

                int elementSizeInByte = ReflectionHelpers.SizeOf<T>();
                GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    IntPtr dataPtr = (IntPtr)(dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * elementSizeInByte);

                    ((IPlatformTextureCollection)base.GraphicsDeviceStrategy.CurrentContext.Textures).Strategy.Dirty(0);
                    GL.ActiveTexture(TextureUnit.Texture0 + 0);
                    GL.CheckGLError();
                    GL.BindTexture(_glTarget, _glTexture);
                    GL.CheckGLError();

                    GL.TexSubImage3D(_glTarget, level, left, top, front, width, height, depth, _glFormat, _glType, dataPtr);
                    GL.CheckGLError();
                }
                finally
                {
                    dataHandle.Free();
                }
            }
        }

        public void GetData<T>(int level, int left, int top, int right, int bottom, int front, int back,
                               T[] data, int startIndex, int elementCount)
             where T : struct
        {
            throw new NotImplementedException();
        }
        #endregion ITexture3DStrategy


        internal void PlatformConstructTexture3D(GraphicsContextStrategy contextStrategy, int width, int height, int depth, bool mipMap, SurfaceFormat format)
        {
            _glTarget = TextureTarget.Texture3D;

            Threading.EnsureMainThread();
            {
                var GL = contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().GL;

                _glTexture = GL.GenTexture();
                GL.CheckGLError();

                ((IPlatformTextureCollection)base.GraphicsDeviceStrategy.CurrentContext.Textures).Strategy.Dirty(0);
                GL.ActiveTexture(TextureUnit.Texture0 + 0);
                GL.CheckGLError();
                GL.BindTexture(_glTarget, _glTexture);
                GL.CheckGLError();

                ConcreteTexture.ToGLSurfaceFormat(format, contextStrategy,
                    out _glInternalFormat,
                    out _glFormat,
                    out _glType);

                GL.TexImage3D(_glTarget, 0, _glInternalFormat, width, height, depth, 0, _glFormat, _glType, IntPtr.Zero);
                GL.CheckGLError();
            }

            if (mipMap)
                throw new NotImplementedException("Texture3D does not yet support mipmaps.");
        }


    }
}
