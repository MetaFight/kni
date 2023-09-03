// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2023 Nick Kastellanos

using System;
using System.IO;
using System.Runtime.InteropServices;
using MonoGame.Framework.Utilities;
using Microsoft.Xna.Platform.Graphics;
using DX = SharpDX;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

#if WINDOWS_UAP
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Threading.Tasks;
#endif


namespace Microsoft.Xna.Framework.Graphics
{
    public partial class Texture2D : Texture
    {
        protected DXGI.SampleDescription SampleDescription { get { return _sampleDescription; } }

        private DXGI.SampleDescription _sampleDescription;

        private void PlatformConstructTexture2D(int width, int height, bool mipMap, SurfaceFormat format, SurfaceType type, bool shared)
        {
            ((ConcreteTexture2D)_strategyTexture2D)._shared = shared;
            ((ConcreteTexture2D)_strategyTexture2D)._mipMap = mipMap;
            _sampleDescription = new DXGI.SampleDescription(1, 0);
        }

        private IntPtr PlatformGetSharedHandle()
        {
            using (DXGI.Resource resource = GetTexture().QueryInterface<DXGI.Resource>())
                return resource.SharedHandle;
        }

        private void PlatformSetData<T>(int level, T[] data, int startIndex, int elementCount) where T : struct
        {
            int w, h;
            Texture.GetSizeForLevel(Width, Height, level, out w, out h);

            // For DXT compressed formats the width and height must be
            // a multiple of 4 for the complete mip level to be set.
            if (this.Format.IsCompressedFormat())
            {
                w = (w + 3) & ~3;
                h = (h + 3) & ~3;
            }

            int elementSizeInByte = ReflectionHelpers.SizeOf<T>();
            GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            // Use try..finally to make sure dataHandle is freed in case of an error
            try
            {
                int startBytes = startIndex * elementSizeInByte;
                IntPtr dataPtr = (IntPtr)(dataHandle.AddrOfPinnedObject().ToInt64() + startBytes);
                D3D11.ResourceRegion region = new D3D11.ResourceRegion();
                region.Top = 0;
                region.Front = 0;
                region.Back = 1;
                region.Bottom = h;
                region.Left = 0;
                region.Right = w;

                // TODO: We need to deal with threaded contexts here!
                int subresourceIndex = CalculateSubresourceIndex(0, level);
                lock (GraphicsDevice.Strategy.CurrentContext.Strategy.ToConcrete<ConcreteGraphicsContext>().D3dContext)
                {
                    D3D11.DeviceContext d3dContext = GraphicsDevice.Strategy.CurrentContext.Strategy.ToConcrete<ConcreteGraphicsContext>().D3dContext;

                    d3dContext.UpdateSubresource(GetTexture(), subresourceIndex, region, dataPtr, Texture.GetPitch(this.Format, w), 0);
                }
            }
            finally
            {
                dataHandle.Free();
            }
        }

        private void PlatformSetData<T>(int level, int arraySlice, Rectangle rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            int elementSizeInByte = ReflectionHelpers.SizeOf<T>();
            GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            // Use try..finally to make sure dataHandle is freed in case of an error
            try
            {
                int startBytes = startIndex * elementSizeInByte;
                IntPtr dataPtr = (IntPtr)(dataHandle.AddrOfPinnedObject().ToInt64() + startBytes);
                D3D11.ResourceRegion region = new D3D11.ResourceRegion();
                region.Top = rect.Top;
                region.Front = 0;
                region.Back = 1;
                region.Bottom = rect.Bottom;
                region.Left = rect.Left;
                region.Right = rect.Right;


                // TODO: We need to deal with threaded contexts here!
                int subresourceIndex = CalculateSubresourceIndex(arraySlice, level);
                lock (GraphicsDevice.Strategy.CurrentContext.Strategy.ToConcrete<ConcreteGraphicsContext>().D3dContext)
                {
                    D3D11.DeviceContext d3dContext = GraphicsDevice.Strategy.CurrentContext.Strategy.ToConcrete<ConcreteGraphicsContext>().D3dContext;

                    d3dContext.UpdateSubresource(GetTexture(), subresourceIndex, region, dataPtr, Texture.GetPitch(this.Format, rect.Width), 0);
                }
            }
            finally
            {
                dataHandle.Free();
            }
        }

        private void PlatformGetData<T>(int level, int arraySlice, Rectangle rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            // Create a temp staging resource for copying the data.
            // 
            // TODO: We should probably be pooling these staging resources
            // and not creating a new one each time.
            //
            int min = this.Format.IsCompressedFormat() ? 4 : 1;
            int levelWidth = Math.Max(this.Width >> level, min);
            int levelHeight = Math.Max(this.Height >> level, min);

            D3D11.Texture2DDescription texture2DDesc = new D3D11.Texture2DDescription();
            texture2DDesc.Width = levelWidth;
            texture2DDesc.Height = levelHeight;
            texture2DDesc.MipLevels = 1;
            texture2DDesc.ArraySize = 1;
            texture2DDesc.Format = GraphicsExtensions.ToDXFormat(this.Format);
            texture2DDesc.BindFlags = D3D11.BindFlags.None;
            texture2DDesc.CpuAccessFlags = D3D11.CpuAccessFlags.Read;
            texture2DDesc.SampleDescription = this.SampleDescription;
            texture2DDesc.Usage = D3D11.ResourceUsage.Staging;
            texture2DDesc.OptionFlags = D3D11.ResourceOptionFlags.None;

            D3D11.Texture2D stagingTexture = new D3D11.Texture2D(GraphicsDevice.Strategy.ToConcrete<ConcreteGraphicsDevice>().D3DDevice, texture2DDesc);


            lock (GraphicsDevice.Strategy.CurrentContext.Strategy.ToConcrete<ConcreteGraphicsContext>().D3dContext)
            {
                D3D11.DeviceContext d3dContext = GraphicsDevice.Strategy.CurrentContext.Strategy.ToConcrete<ConcreteGraphicsContext>().D3dContext;

                int subresourceIndex = CalculateSubresourceIndex(arraySlice, level);

                // Copy the data from the GPU to the staging texture.
                int elementsInRow = rect.Width;
                int rows = rect.Height;
                D3D11.ResourceRegion region = new D3D11.ResourceRegion(rect.Left, rect.Top, 0, rect.Right, rect.Bottom, 1);
                d3dContext.CopySubresourceRegion(GetTexture(), subresourceIndex, region, stagingTexture, 0);

                // Copy the data to the array.
                DX.DataStream stream = null;
                try
                {
                    DX.DataBox databox = d3dContext.MapSubresource(stagingTexture, 0, D3D11.MapMode.Read, D3D11.MapFlags.None, out stream);

                    int elementSize = this.Format.GetSize();
                    if (this.Format.IsCompressedFormat())
                    {
                        // for 4x4 block compression formats an element is one block, so elementsInRow
                        // and number of rows are 1/4 of number of pixels in width and height of the rectangle
                        elementsInRow /= 4;
                        rows /= 4;
                    }
                    int rowSize = elementSize * elementsInRow;
                    if (rowSize == databox.RowPitch)
                        stream.ReadRange(data, startIndex, elementCount);
                    else if (level == 0 && arraySlice == 0 &&
                             rect.X == 0 && rect.Y == 0 &&
                             rect.Width == this.Width && rect.Height == this.Height &&
                             startIndex == 0 && elementCount == data.Length)
                    {
                        // TNC: optimized PlatformGetData() that reads multiple elements in a row when texture has rowPitch
                        int elementSize2 = DX.Utilities.SizeOf<T>();
                        if (elementSize2 == 1) // byte[]
                            elementsInRow = elementsInRow * elementSize;

                        int currentIndex = 0;
                        for (int row = 0; row < rows; row++)
                        {
                            stream.ReadRange(data, currentIndex, elementsInRow);
                            stream.Seek((databox.RowPitch - rowSize), SeekOrigin.Current);
                            currentIndex += elementsInRow;
                        }
                    }
                    else
                    {
                        // Some drivers may add pitch to rows.
                        // We need to copy each row separatly and skip trailing zeros.
                        stream.Seek(0, SeekOrigin.Begin);

                        int elementSizeInByte = ReflectionHelpers.SizeOf<T>();
                        for (int row = 0; row < rows; row++)
                        {
                            int i;
                            int maxElements =  (row + 1) * rowSize / elementSizeInByte;
                            for (i = row * rowSize / elementSizeInByte; i < maxElements; i++)
                                data[i + startIndex] = stream.Read<T>();

                            if (i >= elementCount)
                                break;

                            stream.Seek(databox.RowPitch - rowSize, SeekOrigin.Current);
                        }
                    }
                }
                finally
                {
                    DX.Utilities.Dispose( ref stream);

                    d3dContext.UnmapSubresource(stagingTexture, 0);                    
                    DX.Utilities.Dispose(ref stagingTexture);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            base.Dispose(disposing);
        }

        private int CalculateSubresourceIndex(int arraySlice, int level)
        {
            return arraySlice * this.LevelCount + level;
        }

        protected internal virtual D3D11.Texture2DDescription GetTexture2DDescription()
        {
            D3D11.Texture2DDescription texture2DDesc = new D3D11.Texture2DDescription();
            texture2DDesc.Width = this.Width;
            texture2DDesc.Height = this.Height;
            texture2DDesc.MipLevels = this.LevelCount;
            texture2DDesc.ArraySize = this.ArraySize;
            texture2DDesc.Format = GraphicsExtensions.ToDXFormat(this.Format);
            texture2DDesc.BindFlags = D3D11.BindFlags.ShaderResource;
            texture2DDesc.CpuAccessFlags = D3D11.CpuAccessFlags.None;
            texture2DDesc.SampleDescription = SampleDescription;
            texture2DDesc.Usage = D3D11.ResourceUsage.Default;
            texture2DDesc.OptionFlags = D3D11.ResourceOptionFlags.None;

            if (((ConcreteTexture2D)_strategyTexture2D)._shared)
                texture2DDesc.OptionFlags |= D3D11.ResourceOptionFlags.Shared;

            return texture2DDesc;
        }
        internal override D3D11.Resource CreateTexture()
        {
            // TODO: Move this to SetData() if we want to make Immutable textures!
            D3D11.Texture2DDescription texture2DDesc = GetTexture2DDescription();
            return new D3D11.Texture2D(GraphicsDevice.Strategy.ToConcrete<ConcreteGraphicsDevice>().D3DDevice, texture2DDesc);
        }
    }
}

