﻿// Copyright (C)2023 Nick Kastellanos

using System;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Platform.Graphics
{
    public interface IPlatformDepthStencilState
    {
        T GetStrategy<T>() where T : IDepthStencilStateStrategy;
    }

    public interface IDepthStencilStateStrategy
    {
        bool DepthBufferEnable { get; set; }
        bool DepthBufferWriteEnable { get; set; }
        CompareFunction DepthBufferFunction { get; set; }
        StencilOperation CounterClockwiseStencilDepthBufferFail { get; set; }
        StencilOperation CounterClockwiseStencilFail { get; set; }
        CompareFunction CounterClockwiseStencilFunction { get; set; }
        StencilOperation CounterClockwiseStencilPass { get; set; }
        int ReferenceStencil { get; set; }
        StencilOperation StencilDepthBufferFail { get; set; }
        bool StencilEnable { get; set; }
        StencilOperation StencilFail { get; set; }
        CompareFunction StencilFunction { get; set; }
        int StencilMask { get; set; }
        StencilOperation StencilPass { get; set; }
        int StencilWriteMask { get; set; }
        bool TwoSidedStencilMode { get; set; }
    }
}
