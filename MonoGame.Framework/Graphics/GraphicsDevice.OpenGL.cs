// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2023 Nick Kastellanos

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Platform.Graphics;
using MonoGame.Framework.Utilities;
using MonoGame.OpenGL;


namespace Microsoft.Xna.Framework.Graphics
{
    public partial class GraphicsDevice
    {
#if DESKTOPGL
        internal IntPtr CurrentGlContext
        {
            get { return ((ConcreteGraphicsContext)CurrentContext.Strategy).GlContext; }
        }
        internal ConcreteGraphicsContext CurrentConcreteContext
        {
            get { return ((ConcreteGraphicsContext)CurrentContext.Strategy); }
        }
#endif


        internal ShaderProgramCache _programCache;


        internal bool _supportsInvalidateFramebuffer;
        internal bool _supportsBlitFramebuffer;

        internal int _glMajorVersion = 0;
        internal int _glMinorVersion = 0;
        internal int _glDefaultFramebuffer = 0;


        private void PlatformSetup()
        {
            _programCache = new ShaderProgramCache(this);

#if DESKTOPGL
            System.Diagnostics.Debug.Assert(_mainContext == null);

#if DEBUG
            // create debug context, so we get better error messages (glDebugMessageCallback)
            Sdl.GL.SetAttribute(Sdl.GL.Attribute.ContextFlags, 1); // 1 = SDL_GL_CONTEXT_DEBUG_FLAG
#endif
#endif

            var contextStrategy = new ConcreteGraphicsContext(this);
            _mainContext = new GraphicsContext(this, contextStrategy);

            // try getting the context version
            // GL_MAJOR_VERSION and GL_MINOR_VERSION are GL 3.0+ only, so we need to rely on GL_VERSION string
            try
            {
                string version = GL.GetString(StringName.Version);
                if (string.IsNullOrEmpty(version))
                    throw new NoSuitableGraphicsDeviceException("Unable to retrieve OpenGL version");

                // for OpenGL, the GL_VERSION string always starts with the version number in the "major.minor" format,
                // optionally followed by multiple vendor specific characters
                // for GLES, the GL_VERSION string is formatted as:
                //     OpenGL<space>ES<space><version number><space><vendor-specific information>
#if GLES
                if (version.StartsWith("OpenGL ES "))
                    version =  version.Split(' ')[2];
                else // if it fails, we assume to be on a 1.1 context
                    version = "1.1";
#endif
                _glMajorVersion = Convert.ToInt32(version.Substring(0, 1));
                _glMinorVersion = Convert.ToInt32(version.Substring(2, 1));
            }
            catch (FormatException)
            {
                // if it fails, we assume to be on a 1.1 context
                _glMajorVersion = 1;
                _glMinorVersion = 1;
            }

            Capabilities = new GraphicsCapabilities();
            Capabilities.PlatformInitialize(this, _glMajorVersion, _glMinorVersion);


#if DESKTOPGL
            // Initialize draw buffer attachment array
            ((ConcreteGraphicsContext)_mainContext.Strategy)._drawBuffers = new DrawBuffersEnum[Capabilities.MaxDrawBuffers];
			for (int i = 0; i < ((ConcreteGraphicsContext)_mainContext.Strategy)._drawBuffers.Length; i++)
                ((ConcreteGraphicsContext)_mainContext.Strategy)._drawBuffers[i] = (DrawBuffersEnum)(DrawBuffersEnum.ColorAttachment0 + i);
#endif

            ((ConcreteGraphicsContext)_mainContext.Strategy)._newEnabledVertexAttributes = new bool[Capabilities.MaxVertexBufferSlots];
        }

        private void PlatformInitialize()
        {
            _mainContext.Strategy._viewport = new Viewport(0, 0, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);

            // Ensure the vertex attributes are reset
            ((ConcreteGraphicsContext)_mainContext.Strategy)._enabledVertexAttributes.Clear();

            // Free all the cached shader programs.
            _programCache.DisposePrograms();
            ((ConcreteGraphicsContext)_mainContext.Strategy)._shaderProgram = null;

            if (Capabilities.SupportsFramebufferObjectARB
            || Capabilities.SupportsFramebufferObjectEXT)
            {
                this._supportsBlitFramebuffer = GL.BlitFramebuffer != null;
                this._supportsInvalidateFramebuffer = GL.InvalidateFramebuffer != null;
            }
            else
            {
                throw new PlatformNotSupportedException(
                    "MonoGame requires either ARB_framebuffer_object or EXT_framebuffer_object." +
                    "Try updating your graphics drivers.");
            }

            // Force resetting states
            this._mainContext.Strategy._actualBlendState.PlatformApplyState((ConcreteGraphicsContextGL)_mainContext.Strategy, true);
            this._mainContext.Strategy._actualDepthStencilState.PlatformApplyState((ConcreteGraphicsContextGL)_mainContext.Strategy, true);
            this._mainContext.Strategy._actualRasterizerState.PlatformApplyState((ConcreteGraphicsContextGL)_mainContext.Strategy, true);

            ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos = new ConcreteGraphicsContext.BufferBindingInfo[Capabilities.MaxVertexBufferSlots];
            for (int i = 0; i < ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos.Length; i++)
                ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos[i] = new ConcreteGraphicsContext.BufferBindingInfo(null, IntPtr.Zero, 0, -1);
        }

        private void PlatformDispose()
        {
            // Free all the cached shader programs.
            _programCache.Dispose();

#if DESKTOPGL
            _mainContext.Dispose();
            _mainContext = null;
#endif
        }

        private void PlatformPresent()
        {
#if DESKTOPGL
            Sdl.GL.SwapWindow(this.PresentationParameters.DeviceWindowHandle);
            GraphicsExtensions.CheckGLError();
#endif
        }

        internal void PlatformCreateRenderTarget(IRenderTarget renderTarget, int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
        {
            int color = 0;
            int depth = 0;
            int stencil = 0;
            
            if (preferredMultiSampleCount > 0 && _supportsBlitFramebuffer)
            {
                color = GL.GenRenderbuffer();
                GraphicsExtensions.CheckGLError();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, color);
                GraphicsExtensions.CheckGLError();
                if (preferredMultiSampleCount > 0 && GL.RenderbufferStorageMultisample != null)
                    GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, preferredMultiSampleCount, RenderbufferStorage.Rgba8, width, height);
                else
                    GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, width, height);
                GraphicsExtensions.CheckGLError();
            }

            if (preferredDepthFormat != DepthFormat.None)
            {
                var depthInternalFormat = RenderbufferStorage.DepthComponent16;
                var stencilInternalFormat = (RenderbufferStorage)0;
                switch (preferredDepthFormat)
                {
                    case DepthFormat.Depth16: 
                        depthInternalFormat = RenderbufferStorage.DepthComponent16;
                        break;
#if GLES
                    case DepthFormat.Depth24:
                        if (Capabilities.SupportsDepth24)
                            depthInternalFormat = RenderbufferStorage.DepthComponent24Oes;
                        else if (Capabilities.SupportsDepthNonLinear)
                            depthInternalFormat = (RenderbufferStorage)0x8E2C;
                        else
                            depthInternalFormat = RenderbufferStorage.DepthComponent16;
                        break;
                    case DepthFormat.Depth24Stencil8:
                        if (Capabilities.SupportsPackedDepthStencil)
                            depthInternalFormat = RenderbufferStorage.Depth24Stencil8Oes;
                        else
                        {
                            if (Capabilities.SupportsDepth24)
                                depthInternalFormat = RenderbufferStorage.DepthComponent24Oes;
                            else if (Capabilities.SupportsDepthNonLinear)
                                depthInternalFormat = (RenderbufferStorage)0x8E2C;
                            else
                                depthInternalFormat = RenderbufferStorage.DepthComponent16;
                            stencilInternalFormat = RenderbufferStorage.StencilIndex8;
                            break;
                        }
                        break;
#else
                    case DepthFormat.Depth24:
                        depthInternalFormat = RenderbufferStorage.DepthComponent24;
                        break;
                    case DepthFormat.Depth24Stencil8:
                        depthInternalFormat = RenderbufferStorage.Depth24Stencil8;
                        break;
#endif
                }

                if (depthInternalFormat != 0)
                {
                    depth = GL.GenRenderbuffer();
                    GraphicsExtensions.CheckGLError();
                    GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depth);
                    GraphicsExtensions.CheckGLError();
                    if (preferredMultiSampleCount > 0 && GL.RenderbufferStorageMultisample != null)
                        GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, preferredMultiSampleCount, depthInternalFormat, width, height);
                    else
                        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, depthInternalFormat, width, height);
                    GraphicsExtensions.CheckGLError();
                    if (preferredDepthFormat == DepthFormat.Depth24Stencil8)
                    {
                        stencil = depth;
                        if (stencilInternalFormat != 0)
                        {
                            stencil = GL.GenRenderbuffer();
                            GraphicsExtensions.CheckGLError();
                            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, stencil);
                            GraphicsExtensions.CheckGLError();
                            if (preferredMultiSampleCount > 0 && GL.RenderbufferStorageMultisample != null)
                                GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, preferredMultiSampleCount, stencilInternalFormat, width, height);
                            else
                                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, stencilInternalFormat, width, height);
                            GraphicsExtensions.CheckGLError();
                        }
                    }
                }
            }

            var renderTargetGL = (IRenderTargetGL)renderTarget;
            if (color != 0)
                renderTargetGL.GLColorBuffer = color;
            else
                renderTargetGL.GLColorBuffer = renderTargetGL.GLTexture;
            renderTargetGL.GLDepthBuffer = depth;
            renderTargetGL.GLStencilBuffer = stencil;
        }

        internal void PlatformDeleteRenderTarget(IRenderTarget renderTarget)
        {
            int color = 0;
            int depth = 0;
            int stencil = 0;

            var renderTargetGL = (IRenderTargetGL)renderTarget;
            color = renderTargetGL.GLColorBuffer;
            depth = renderTargetGL.GLDepthBuffer;
            stencil = renderTargetGL.GLStencilBuffer;
            bool colorIsRenderbuffer = renderTargetGL.GLColorBuffer != renderTargetGL.GLTexture;

            if (color != 0)
            {
                if (colorIsRenderbuffer)
                {
                    GL.DeleteRenderbuffer(color);
                    GraphicsExtensions.CheckGLError();
                }
                if (stencil != 0 && stencil != depth)
                {
                    GL.DeleteRenderbuffer(stencil);
                    GraphicsExtensions.CheckGLError();
                }
                if (depth != 0)
                {
                    GL.DeleteRenderbuffer(depth);
                    GraphicsExtensions.CheckGLError();
                }

                var bindingsToDelete = new List<RenderTargetBinding[]>();
                foreach (var bindings in ((ConcreteGraphicsContext)_mainContext.Strategy)._glFramebuffers.Keys)
                {
                    foreach (var binding in bindings)
                    {
                        if (binding.RenderTarget == renderTarget)
                        {
                            bindingsToDelete.Add(bindings);
                            break;
                        }
                    }
                }

                foreach (var bindings in bindingsToDelete)
                {
                    int fbo = 0;
                    if (((ConcreteGraphicsContext)_mainContext.Strategy)._glFramebuffers.TryGetValue(bindings, out fbo))
                    {
                        GL.DeleteFramebuffer(fbo);
                        GraphicsExtensions.CheckGLError();
                        ((ConcreteGraphicsContext)_mainContext.Strategy)._glFramebuffers.Remove(bindings);
                    }
                    if (((ConcreteGraphicsContext)_mainContext.Strategy)._glResolveFramebuffers.TryGetValue(bindings, out fbo))
                    {
                        GL.DeleteFramebuffer(fbo);
                        GraphicsExtensions.CheckGLError();
                        ((ConcreteGraphicsContext)_mainContext.Strategy)._glResolveFramebuffers.Remove(bindings);
                    }
                }
            }
        }

        private void PlatformGetBackBufferData<T>(Rectangle? rectangle, T[] data, int startIndex, int count) where T : struct
        {
            Rectangle rect = rectangle ?? new Rectangle(0, 0, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);
            int tSize = ReflectionHelpers.SizeOf<T>();
            int flippedY = PresentationParameters.BackBufferHeight - rect.Y - rect.Height;
            GL.ReadPixels(rect.X, flippedY, rect.Width, rect.Height, PixelFormat.Rgba, PixelType.UnsignedByte, data);

            // buffer is returned upside down, so we swap the rows around when copying over
            int rowSize = rect.Width * PresentationParameters.BackBufferFormat.GetSize() / tSize;
            T[] row = new T[rowSize];
            for (int dy = 0; dy < rect.Height/2; dy++)
            {
                int topRow = startIndex + dy*rowSize;
                int bottomRow = startIndex + (rect.Height - dy - 1)*rowSize;
                // copy the bottom row to buffer
                Array.Copy(data, bottomRow, row, 0, rowSize);
                // copy top row to bottom row
                Array.Copy(data, topRow, data, bottomRow, rowSize);
                // copy buffer to top row
                Array.Copy(row, 0, data, topRow, rowSize);
                count -= rowSize;
            }
        }

        private static Rectangle PlatformGetTitleSafeArea(int x, int y, int width, int height)
        {
            return new Rectangle(x, y, width, height);
        }
        
        internal void PlatformSetMultiSamplingToMaximum(PresentationParameters presentationParameters, out int quality)
        {
            presentationParameters.MultiSampleCount = 4;
            quality = 0;
        }

        internal void OnPresentationChanged()
        {
#if DESKTOPGL
            CurrentConcreteContext.MakeCurrent(this.PresentationParameters.DeviceWindowHandle);
            int swapInterval = ConcreteGraphicsContext.ToGLSwapInterval(PresentationParameters.PresentationInterval);
            Sdl.GL.SetSwapInterval(swapInterval);
#endif

            CurrentContext.ApplyRenderTargets(null);
        }

        internal void Android_OnDeviceResetting()
        {
            var handler = DeviceResetting;
            if (handler != null)
                handler(this, EventArgs.Empty);

            lock (_resourcesLock)
            {
                foreach (var resource in _resources)
                {
                    var target = resource.Target as GraphicsResource;
                    if (target != null)
                        target.GraphicsDeviceResetting();
                }

                // Remove references to resources that have been garbage collected.
                _resources.RemoveAll(wr => !wr.IsAlive);
            }
        }

        internal void Android_OnDeviceReset()
        {
            var handler = DeviceReset;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        internal void Android_ReInitializeContext()
        {
            PlatformInitialize();

            // Force set the default render states.
            _mainContext.Strategy._blendStateDirty = true;
            _mainContext.Strategy._blendFactorDirty = true;
            _mainContext.Strategy._depthStencilStateDirty = true;
            _mainContext.Strategy._rasterizerStateDirty = true;
            BlendState = BlendState.Opaque;
            DepthStencilState = DepthStencilState.Default;
            RasterizerState = RasterizerState.CullCounterClockwise;

            // Clear the texture and sampler collections forcing
            // the state to be reapplied.
            _mainContext.Strategy.VertexTextures.Clear();
            _mainContext.Strategy.VertexSamplerStates.Clear();
            _mainContext.Strategy.Textures.Clear();
            _mainContext.Strategy.SamplerStates.Clear();

            // Clear constant buffers
            _mainContext.Strategy._vertexConstantBuffers.Clear();
            _mainContext.Strategy._pixelConstantBuffers.Clear();

            // Force set the buffers and shaders on next ApplyState() call
            _mainContext.Strategy._vertexBuffers = new VertexBufferBindings(Capabilities.MaxVertexBufferSlots);
            _mainContext.Strategy._vertexBuffersDirty = true;
            _mainContext.Strategy._indexBufferDirty = true;
            _mainContext.Strategy._vertexShaderDirty = true;
            _mainContext.Strategy._pixelShaderDirty = true;

            // Set the default scissor rect.
            _mainContext.Strategy._scissorRectangleDirty = true;
            ScissorRectangle = _mainContext.Strategy._viewport.Bounds;

            // Set the default render target.
            CurrentContext.ApplyRenderTargets(null);
        }

#if DESKTOPGL
        private void GetModeSwitchedSize(out int width, out int height)
        {
            var mode = new Sdl.Display.Mode
            {
                Width = PresentationParameters.BackBufferWidth,
                Height = PresentationParameters.BackBufferHeight,
                Format = 0,
                RefreshRate = 0,
                DriverData = IntPtr.Zero
            };
            Sdl.Display.Mode closest;
            Sdl.Display.GetClosestDisplayMode(0, mode, out closest);
            width = closest.Width;
            height = closest.Height;
        }

        private void GetDisplayResolution(out int width, out int height)
        {
            Sdl.Display.Mode mode;
            Sdl.Display.GetCurrentDisplayMode(0, out mode);
            width = mode.Width;
            height = mode.Height;
        }
#endif
    }
}
