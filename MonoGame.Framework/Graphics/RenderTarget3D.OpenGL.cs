// Copyright (C)2022 Nick Kastellanos

using System;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class RenderTarget3D
    {

        private void PlatformConstruct(GraphicsDevice graphicsDevice, int width, int height, bool mipMap,
            DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
        {
            throw new PlatformNotSupportedException();

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {                
            }

            base.Dispose(disposing);
        }

    }
}