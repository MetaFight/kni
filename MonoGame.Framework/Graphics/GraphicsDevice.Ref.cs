// Copyright (C)2022 Nick Kastellanos

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class GraphicsDevice
    {

        internal void OnPresentationChanged()
        {
            throw new PlatformNotSupportedException();
        }

    }
}
