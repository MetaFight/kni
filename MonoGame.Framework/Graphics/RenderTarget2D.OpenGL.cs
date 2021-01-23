// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using Microsoft.Xna.Platform.Graphics;
using MonoGame.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class RenderTarget2D : IRenderTargetGL
    {
        private static Action<RenderTarget2D> DisposeAction =
            (t) => t.GraphicsDevice.PlatformDeleteRenderTarget(t);

        int IRenderTargetGL.GLTexture { get { return glTexture; } }
        TextureTarget IRenderTargetGL.GLTarget { get { return glTarget; } }
        int IRenderTargetGL.GLColorBuffer { get; set; }
        int IRenderTargetGL.GLDepthBuffer { get; set; }
        int IRenderTargetGL.GLStencilBuffer { get; set; }

        TextureTarget IRenderTargetGL.GetFramebufferTarget(int arraySlice)
        {
            return glTarget;
        }

        private void PlatformConstruct(GraphicsDevice graphicsDevice, int width, int height, bool mipMap,
            DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage, bool shared)
        {
            Threading.EnsureUIThread();
            {
                graphicsDevice.PlatformCreateRenderTarget(
                    this, width, height, mipMap, this.Format, preferredDepthFormat, preferredMultiSampleCount, usage);
            }
        }

        private void PlatformGraphicsDeviceResetting()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (GraphicsDevice != null)
                {
                    Threading.BlockOnUIThread(DisposeAction, this);
                }
            }

            base.Dispose(disposing);
        }
    }
}