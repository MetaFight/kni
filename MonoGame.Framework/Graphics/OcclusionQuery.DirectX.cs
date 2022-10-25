// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using SharpDX.Direct3D11;

namespace Microsoft.Xna.Framework.Graphics
{
    partial class OcclusionQuery
    {
        private Query _query;

        private void PlatformConstruct()
        {
            //if (graphicsDevice.D3DDevice.FeatureLevel == SharpDX.Direct3D.FeatureLevel.Level_9_1)
            //    throw new NotSupportedException("The Reach profile does not support occlusion queries.");

            var queryDescription = new QueryDescription
            {
                Flags = QueryFlags.None,
                Type = QueryType.Occlusion
            };
            _query = new Query(GraphicsDevice.D3DDevice, queryDescription);
        }
        
        private void PlatformBegin()
        {
            lock(GraphicsDevice.CurentD3DContext)
                GraphicsDevice.CurentD3DContext.Begin(_query);
        }

        private void PlatformEnd()
        {
            lock (GraphicsDevice.CurentD3DContext)
                GraphicsDevice.CurentD3DContext.End(_query);
        }

        private bool PlatformGetResult(out int pixelCount)
        {
            ulong count;
            bool isComplete;

            lock (GraphicsDevice.CurentD3DContext)
                isComplete = GraphicsDevice.CurentD3DContext.GetData(_query, out count);

            pixelCount = (int)count;
            return isComplete;
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                    _query.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

