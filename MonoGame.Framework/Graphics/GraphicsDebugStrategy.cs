﻿// Copyright (C)2023 Nick Kastellanos

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Microsoft.Xna.Platform.Graphics
{
    public abstract class GraphicsDebugStrategy
    {
        internal readonly GraphicsDevice _device;

        internal GraphicsDebugStrategy(GraphicsDevice device)
        {
            _device = device;

        }

        public abstract bool TryDequeueMessage(out GraphicsDebugMessage message);
    }
}