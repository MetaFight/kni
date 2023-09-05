﻿// Copyright (C)2023 Nick Kastellanos

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Microsoft.Xna.Platform.Graphics
{
    internal class ConcreteTexture2D : ConcreteTexture, ITexture2DStrategy
    {
        internal ConcreteTexture2D(GraphicsContextStrategy contextStrategy, int width, int height, bool mipMap, SurfaceFormat format)
            : base(contextStrategy, format, Texture.CalculateMipLevels(mipMap, width, height))
        {

        }


        #region ITexture2DStrategy
        public int Width
        {
            get { throw new PlatformNotSupportedException(); }
        }

        public int Height
        {
            get { throw new PlatformNotSupportedException(); }
        }

        public int ArraySize
        {
            get { throw new PlatformNotSupportedException(); }
        }

        public Rectangle Bounds
        {
            get { throw new PlatformNotSupportedException(); }
        }

        public void SetData<T>(int level, T[] data, int startIndex, int elementCount)
            where T : struct
        {
            throw new PlatformNotSupportedException();
        }

        public void SetData<T>(int level, int arraySlice, Rectangle rect, T[] data, int startIndex, int elementCount)
            where T : struct
        {
            throw new PlatformNotSupportedException();
        }

        public void GetData<T>(int level, int arraySlice, Rectangle rect, T[] data, int startIndex, int elementCount)
            where T : struct
        {
            throw new PlatformNotSupportedException();
        }
        #endregion #region ITexture2DStrategy

    }
}
