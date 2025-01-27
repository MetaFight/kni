﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2023 Nick Kastellanos

using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Platform.Graphics.Utilities;
using SharpDX.Mathematics.Interop;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;


namespace Microsoft.Xna.Platform.Graphics
{
    static partial class SharpDXExtensions
    {
        static public int ToDXSwapInterval(this PresentInterval interval)
        {
            switch (interval)
            {
                case PresentInterval.Immediate:
                    return 0;
                case PresentInterval.Two:
                    return 2;

                default:
                    return 1;
            }
        }

        static public DXGI.SwapEffect ToDXSwapEffect(this PresentInterval presentInterval)
        {
            DXGI.SwapEffect effect;

            switch (presentInterval)
            {
                case PresentInterval.One:
                case PresentInterval.Two:
#if UAP || WINUI
                    effect = DXGI.SwapEffect.FlipSequential;
#else
                    effect = DXGI.SwapEffect.Discard;
#endif
                    break;

                case PresentInterval.Immediate:
#if UAP || WINUI
                    effect = DXGI.SwapEffect.FlipSequential;
#else
                    effect = DXGI.SwapEffect.Sequential;
#endif
                    break;


                default:
#if UAP || WINUI
                    effect = DXGI.SwapEffect.FlipSequential;
#else
                    effect = DXGI.SwapEffect.Discard;
#endif
                    break;

            }

            //if (present.RenderTargetUsage != RenderTargetUsage.PreserveContents && present.MultiSampleCount == 0)
            //effect = DXGI.SwapEffect.Discard;

            return effect;
        }

        static public DXGI.Format ToDXFormat(this DepthFormat format)
        {
            switch (format)
            {
                case DepthFormat.None:
                    return DXGI.Format.Unknown;
                case DepthFormat.Depth16:
                    return DXGI.Format.D16_UNorm;
                case DepthFormat.Depth24:
                case DepthFormat.Depth24Stencil8:
                    return DXGI.Format.D24_UNorm_S8_UInt;
                    
                default:
                    return DXGI.Format.Unknown;
            }
        }

        static public DXGI.Format ToDXFormat(this SurfaceFormat format)
        {
            switch (format)
            {
                case SurfaceFormat.Color:
                    return DXGI.Format.R8G8B8A8_UNorm;
                case SurfaceFormat.Bgr565:
                    return DXGI.Format.B5G6R5_UNorm;
                case SurfaceFormat.Bgra5551:
                    return DXGI.Format.B5G5R5A1_UNorm;
                case SurfaceFormat.Bgra4444:
#if UAP || WINUI
                    return DXGI.Format.B4G4R4A4_UNorm;
#else
                    return (DXGI.Format)115;
#endif
                case SurfaceFormat.Dxt1:
                    return DXGI.Format.BC1_UNorm;
                case SurfaceFormat.Dxt3:
                    return DXGI.Format.BC2_UNorm;
                case SurfaceFormat.Dxt5:
                    return DXGI.Format.BC3_UNorm;
                case SurfaceFormat.NormalizedByte2:
                    return DXGI.Format.R8G8_SNorm;
                case SurfaceFormat.NormalizedByte4:
                    return DXGI.Format.R8G8B8A8_SNorm;
                case SurfaceFormat.Rgba1010102:
                    return DXGI.Format.R10G10B10A2_UNorm;
                case SurfaceFormat.Rg32:
                    return DXGI.Format.R16G16_UNorm;
                case SurfaceFormat.Rgba64:
                    return DXGI.Format.R16G16B16A16_UNorm;
                case SurfaceFormat.Alpha8:
                    return DXGI.Format.A8_UNorm;
                case SurfaceFormat.Single:
                    return DXGI.Format.R32_Float;
                case SurfaceFormat.HalfSingle:
                    return DXGI.Format.R16_Float;
                case SurfaceFormat.HalfVector2:
                    return DXGI.Format.R16G16_Float;
                case SurfaceFormat.Vector2:
                    return DXGI.Format.R32G32_Float;
                case SurfaceFormat.Vector4:
                    return DXGI.Format.R32G32B32A32_Float;
                case SurfaceFormat.HalfVector4:
                    return DXGI.Format.R16G16B16A16_Float;
                case SurfaceFormat.HdrBlendable:
                    // TODO: This needs to check the graphics device and 
                    // return the best hdr blendable format for the device.
                    return DXGI.Format.R16G16B16A16_Float;
                case SurfaceFormat.Bgr32:
                    return DXGI.Format.B8G8R8X8_UNorm;
                case SurfaceFormat.Bgra32:
                    return DXGI.Format.B8G8R8A8_UNorm;
                case SurfaceFormat.ColorSRgb:
                    return DXGI.Format.R8G8B8A8_UNorm_SRgb;
                case SurfaceFormat.Bgr32SRgb:
                    return DXGI.Format.B8G8R8X8_UNorm_SRgb;
                case SurfaceFormat.Bgra32SRgb:
                    return DXGI.Format.B8G8R8A8_UNorm_SRgb;
                case SurfaceFormat.Dxt1SRgb:
                    return DXGI.Format.BC1_UNorm_SRgb;
                case SurfaceFormat.Dxt3SRgb:
                    return DXGI.Format.BC2_UNorm_SRgb;
                case SurfaceFormat.Dxt5SRgb:
                    return DXGI.Format.BC3_UNorm_SRgb;
                    
                default:
                    return DXGI.Format.R8G8B8A8_UNorm;
            }
        }

        static public int GetPitch(this SurfaceFormat format, int width)
        {
            Debug.Assert(width > 0, "The width is negative!");

            switch (format)
            {
                case SurfaceFormat.Dxt1:
                case SurfaceFormat.Dxt1SRgb:
                case SurfaceFormat.Dxt1a:
                case SurfaceFormat.RgbPvrtc2Bpp:
                case SurfaceFormat.RgbaPvrtc2Bpp:
                case SurfaceFormat.RgbEtc1:
                case SurfaceFormat.Rgb8Etc2:
                case SurfaceFormat.Srgb8Etc2:
                case SurfaceFormat.Rgb8A1Etc2:
                case SurfaceFormat.Srgb8A1Etc2:
                case SurfaceFormat.Dxt3:
                case SurfaceFormat.Dxt3SRgb:
                case SurfaceFormat.Dxt5:
                case SurfaceFormat.Dxt5SRgb:
                case SurfaceFormat.RgbPvrtc4Bpp:
                case SurfaceFormat.RgbaPvrtc4Bpp:
                    return ((width + 3) / 4) * format.GetSize();

                default:
                    return width * format.GetSize();
            };
        }

        static public D3D11.Comparison ToDXComparisonFunction(this CompareFunction compare)
        {
            switch (compare)
            {
                case CompareFunction.Always:
                    return D3D11.Comparison.Always;
                case CompareFunction.Equal:
                    return D3D11.Comparison.Equal;
                case CompareFunction.Greater:
                    return D3D11.Comparison.Greater;
                case CompareFunction.GreaterEqual:
                    return D3D11.Comparison.GreaterEqual;
                case CompareFunction.Less:
                    return D3D11.Comparison.Less;
                case CompareFunction.LessEqual:
                    return D3D11.Comparison.LessEqual;
                case CompareFunction.Never:
                    return D3D11.Comparison.Never;
                case CompareFunction.NotEqual:
                    return D3D11.Comparison.NotEqual;

                default:
                    throw new ArgumentException("Invalid comparison!");
            }
        }

        static public RawVector2 ToRawVector2(this Vector2 vec)
        {
            return new RawVector2(vec.X, vec.Y);
        }

        static public RawVector3 ToRawVector3(this Vector3 vec)
        {
            return new RawVector3(vec.X, vec.Y, vec.Z);
        }

        static public RawVector4 ToRawVector4(this Vector4 vec)
        {
            return new RawVector4(vec.X, vec.Y, vec.Z, vec.W);
        }

        static public RawColor4 ToRawColor4(this Vector4 color)
        {
            return new RawColor4(color.X, color.Y, color.Z, color.W);
        }

        static public RawColor4 ToRawColor4(this Color color)
        {
            return new RawColor4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
        }

        static public RawViewportF ToRawViewportF(this Viewport viewport)
        {
            return new RawViewportF
            {
                X = viewport.X,
                Y = viewport.Y,
                Width = viewport.Width,
                Height = viewport.Height,
                MinDepth = viewport.MinDepth,
                MaxDepth = viewport.MaxDepth
            };
        }

        

    }
}
