﻿// Copyright (C)2023 Nick Kastellanos

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;


namespace Microsoft.Xna.Platform.Graphics
{
    internal class ConcreteRenderTargetCube : IRenderTargetCubeStrategy, ITextureCubeStrategy, ITextureStrategy
    {
        internal ConcreteRenderTargetCube(GraphicsContextStrategy contextStrategy, int size, bool mipMap)
        {

        }


        #region ITextureStrategy
        public SurfaceFormat Format
        {
            get { throw new PlatformNotSupportedException(); }
        }

        public int LevelCount
        {
            get { throw new PlatformNotSupportedException(); }
        }
        #endregion #region ITextureStrategy


        #region ITextureCubeStrategy
        public int Size
        {
            get { throw new PlatformNotSupportedException(); }
        }
        #endregion #region ITextureCubeStrategy


        #region IRenderTargetStrategy
        public DepthFormat DepthStencilFormat
        {
            get { throw new PlatformNotSupportedException(); }
        }

        public int MultiSampleCount
        {
            get { throw new PlatformNotSupportedException(); }
        }

        public RenderTargetUsage RenderTargetUsage
        {
            get { throw new PlatformNotSupportedException(); }
        }
        #endregion #region IRenderTarget2DStrategy

    }
}