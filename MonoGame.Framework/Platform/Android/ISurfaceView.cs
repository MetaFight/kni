﻿// Copyright (C)2023 Nick Kastellanos

using System;
using System.Collections.Generic;
using Microsoft.Xna.Platform.Graphics.OpenGL;
using Javax.Microedition.Khronos.Egl;


namespace Microsoft.Xna.Platform
{
    interface ISurfaceView
    {
        IEGL10 Egl { get; }
        GLESVersion GLesVersion { get; }
        EGLConfig EglConfig { get; }
        EGLDisplay EglDisplay { get; }
        EGLContext EglContext { get; }
    }
}
