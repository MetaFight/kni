// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2023 Nick Kastellanos

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
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


        private ShaderProgramCache _programCache;


        private bool _supportsInvalidateFramebuffer;
        private bool _supportsBlitFramebuffer;

        internal int _glMajorVersion = 0;
        internal int _glMinorVersion = 0;
        internal int _glDefaultFramebuffer = 0;


        // Get a hashed value based on the currently bound shaders
        // throws an exception if no shaders are bound
        private int ShaderProgramHash
        {
            get
            {
                if (_mainContext.Strategy._vertexShader == null && _mainContext.Strategy._pixelShader == null)
                    throw new InvalidOperationException("There is no shader bound!");
                if (_mainContext.Strategy._vertexShader == null)
                    return _mainContext.Strategy._pixelShader.HashKey;
                if (_mainContext.Strategy._pixelShader == null)
                    return _mainContext.Strategy._vertexShader.HashKey;
                return _mainContext.Strategy._vertexShader.HashKey ^ _mainContext.Strategy._pixelShader.HashKey;
            }
        }

        private int ShaderProgramHash2
        {
            get { return _mainContext.Strategy._vertexShader.HashKey ^ _mainContext.Strategy._pixelShader.HashKey; }
        }

        internal void SetVertexAttributeArray(bool[] attrs)
        {
            for (int x = 0; x < attrs.Length; x++)
            {
                if (attrs[x])
                {
                    if (((ConcreteGraphicsContext)_mainContext.Strategy)._enabledVertexAttributes.Add(x))
                    {
                        GL.EnableVertexAttribArray(x);
                        GraphicsExtensions.CheckGLError();
                    }
                }
                else
                {
                    if (((ConcreteGraphicsContext)_mainContext.Strategy)._enabledVertexAttributes.Remove(x))
                    {
                        GL.DisableVertexAttribArray(x);
                        GraphicsExtensions.CheckGLError();
                    }
                }
            }
        }

        private void PlatformApplyVertexBuffersAttribs(Shader shader, int baseVertex)
        {
            int programHash = ShaderProgramHash;
            bool bindingsChanged = false;

            for (int slot = 0; slot < _mainContext.Strategy._vertexBuffers.Count; slot++)
            {
                var vertexBufferBinding = _mainContext.Strategy._vertexBuffers.Get(slot);
                var vertexDeclaration = vertexBufferBinding.VertexBuffer.VertexDeclaration;
                var attrInfo = vertexDeclaration.GetAttributeInfo(shader, programHash);

                int vertexStride = vertexDeclaration.VertexStride;
                IntPtr offset = (IntPtr)(vertexDeclaration.VertexStride * (baseVertex + vertexBufferBinding.VertexOffset));

                if (!((ConcreteGraphicsContext)_mainContext.Strategy)._attribsDirty &&
                    slot < ((ConcreteGraphicsContext)_mainContext.Strategy)._activeBufferBindingInfosCount &&
                    ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos[slot].VertexOffset == offset &&
                    ReferenceEquals(((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos[slot].AttributeInfo, attrInfo) &&
                    ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos[slot].InstanceFrequency == vertexBufferBinding.InstanceFrequency &&
                    ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos[slot].Vbo == vertexBufferBinding.VertexBuffer._vbo)
                    continue;

                bindingsChanged = true;

                GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferBinding.VertexBuffer._vbo);
                GraphicsExtensions.CheckGLError();

                for (int e = 0; e < attrInfo.Elements.Count; e++)
                {
                    var element = attrInfo.Elements[e];
                    GL.VertexAttribPointer(element.AttributeLocation,
                        element.NumberOfElements,
                        element.VertexAttribPointerType,
                        element.Normalized,
                        vertexStride,
                        (IntPtr)(offset.ToInt64() + element.Offset));
                    GraphicsExtensions.CheckGLError();

                    // only set the divisor if instancing is supported
                    if (Capabilities.SupportsInstancing)
                    {
                        GL.VertexAttribDivisor(element.AttributeLocation, vertexBufferBinding.InstanceFrequency);
                        GraphicsExtensions.CheckGLError();
                    }
                    else // If instancing is not supported, but InstanceFrequency of the buffer is not zero, throw an exception
                    {
                        if (vertexBufferBinding.InstanceFrequency > 0)
                            throw new PlatformNotSupportedException("Instanced geometry drawing requires at least OpenGL 3.2 or GLES 3.2. Try upgrading your graphics drivers.");
                    }
                }

                ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos[slot].VertexOffset = offset;
                ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos[slot].AttributeInfo = attrInfo;
                ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos[slot].InstanceFrequency = vertexBufferBinding.InstanceFrequency;
                ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos[slot].Vbo = vertexBufferBinding.VertexBuffer._vbo;
            }

            ((ConcreteGraphicsContext)_mainContext.Strategy)._attribsDirty = false;

            if (bindingsChanged)
            {
                for (int eva = 0; eva < ((ConcreteGraphicsContext)_mainContext.Strategy)._newEnabledVertexAttributes.Length; eva++)
                    ((ConcreteGraphicsContext)_mainContext.Strategy)._newEnabledVertexAttributes[eva] = false;
                for (int slot = 0; slot < _mainContext.Strategy._vertexBuffers.Count; slot++)
                {
                    for (int e = 0; e< ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos[slot].AttributeInfo.Elements.Count; e++)
                    {
                        var element = ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos[slot].AttributeInfo.Elements[e];
                        ((ConcreteGraphicsContext)_mainContext.Strategy)._newEnabledVertexAttributes[element.AttributeLocation] = true;
                    }
                }
                ((ConcreteGraphicsContext)_mainContext.Strategy)._activeBufferBindingInfosCount = _mainContext.Strategy._vertexBuffers.Count;
            }
            SetVertexAttributeArray(((ConcreteGraphicsContext)_mainContext.Strategy)._newEnabledVertexAttributes);
        }

        private void PlatformApplyUserVertexDataAttribs(VertexDeclaration vertexDeclaration, Shader shader, IntPtr baseVertex)
        {
            int programHash = ShaderProgramHash;
            var attrInfo = vertexDeclaration.GetAttributeInfo(shader, programHash);

            // Apply the vertex attribute info
            for (int i = 0; i < attrInfo.Elements.Count; i++)
            {
                var element = attrInfo.Elements[i];
                GL.VertexAttribPointer(element.AttributeLocation,
                    element.NumberOfElements,
                    element.VertexAttribPointerType,
                    element.Normalized,
                    vertexDeclaration.VertexStride,
                    (IntPtr)(baseVertex.ToInt64() + element.Offset));
                GraphicsExtensions.CheckGLError();

#if DESKTOPGL
                if (Capabilities.SupportsInstancing)
                {
                    GL.VertexAttribDivisor(element.AttributeLocation, 0);
                    GraphicsExtensions.CheckGLError();
                }
#endif
            }
            SetVertexAttributeArray(attrInfo.EnabledAttributes);
            ((ConcreteGraphicsContext)_mainContext.Strategy)._attribsDirty = true;
        }


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
            _viewport = new Viewport(0, 0, PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);

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
            this._mainContext.Strategy._actualBlendState.PlatformApplyState(_mainContext.Strategy, this, true);
            this.DepthStencilState.PlatformApplyState(_mainContext.Strategy, this, true);
            this.RasterizerState.PlatformApplyState(_mainContext.Strategy, this, true);

            ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos = new ConcreteGraphicsContext.BufferBindingInfo[Capabilities.MaxVertexBufferSlots];
            for (int i = 0; i < ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos.Length; i++)
                ((ConcreteGraphicsContext)_mainContext.Strategy)._bufferBindingInfos[i] = new ConcreteGraphicsContext.BufferBindingInfo(null, IntPtr.Zero, 0, -1);
        }
        
        private void PlatformClear(ClearOptions options, Vector4 color, float depth, int stencil)
        {
            // TODO: We need to figure out how to detect if we have a
            // depth stencil buffer or not, and clear options relating
            // to them if not attached.

            // Unlike with XNA and DirectX...  GL.Clear() obeys several
            // different render states:
            //
            //  - The color write flags.
            //  - The scissor rectangle.
            //  - The depth/stencil state.
            //
            // So overwrite these states with what is needed to perform
            // the clear correctly and restore it afterwards.
            //
            Rectangle prevScissorRect = ScissorRectangle;
            DepthStencilState prevDepthStencilState = DepthStencilState;
            BlendState prevBlendState = BlendState;
            ScissorRectangle = _viewport.Bounds;
            // DepthStencilState.Default has the Stencil Test disabled; 
            // make sure stencil test is enabled before we clear since
            // some drivers won't clear with stencil test disabled
            DepthStencilState = ((ConcreteGraphicsContext)_mainContext.Strategy)._clearDepthStencilState;
		    BlendState = BlendState.Opaque;
            PlatformApplyState();

            ClearBufferMask bufferMask = 0;
            if ((options & ClearOptions.Target) == ClearOptions.Target)
            {
                if (color != ((ConcreteGraphicsContext)_mainContext.Strategy)._lastClearColor)
                {
                    GL.ClearColor(color.X, color.Y, color.Z, color.W);
                    GraphicsExtensions.CheckGLError();
                    ((ConcreteGraphicsContext)_mainContext.Strategy)._lastClearColor = color;
                }
                bufferMask = bufferMask | ClearBufferMask.ColorBufferBit;
            }
			if ((options & ClearOptions.Stencil) == ClearOptions.Stencil)
            {
                if (stencil != ((ConcreteGraphicsContext)_mainContext.Strategy)._lastClearStencil)
                {
				    GL.ClearStencil(stencil);
                    GraphicsExtensions.CheckGLError();
                    ((ConcreteGraphicsContext)_mainContext.Strategy)._lastClearStencil = stencil;
                }
                bufferMask = bufferMask | ClearBufferMask.StencilBufferBit;
			}

			if ((options & ClearOptions.DepthBuffer) == ClearOptions.DepthBuffer) 
            {
                if (depth != ((ConcreteGraphicsContext)_mainContext.Strategy)._lastClearDepth)
                {
                    GL.ClearDepth(depth);
                    GraphicsExtensions.CheckGLError();
                    ((ConcreteGraphicsContext)_mainContext.Strategy)._lastClearDepth = depth;
                }
				bufferMask = bufferMask | ClearBufferMask.DepthBufferBit;
			}

#if IOS || TVOS
            if (GL.CheckFramebufferStatus(FramebufferTarget.FramebufferExt) == FramebufferErrorCode.FramebufferComplete)
            {
#endif
                GL.Clear(bufferMask);
                GraphicsExtensions.CheckGLError();
#if IOS || TVOS
            }
#endif
           		
            // Restore the previous render state.
		    ScissorRectangle = prevScissorRect;
		    DepthStencilState = prevDepthStencilState;
		    BlendState = prevBlendState;
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

        private void PlatformApplyDefaultRenderTarget()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _glDefaultFramebuffer);
            GraphicsExtensions.CheckGLError();

            // Reset the raster state because we flip vertices
            // when rendering offscreen and hence the cull direction.
            _mainContext.Strategy._rasterizerStateDirty = true;

            // Textures will need to be rebound to render correctly in the new render target.
            _mainContext.Strategy.Textures.Dirty();
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

        internal static readonly FramebufferAttachment[] InvalidateFramebufferAttachements =
        {
            FramebufferAttachment.ColorAttachment0,
            FramebufferAttachment.DepthAttachment,
            FramebufferAttachment.StencilAttachment,
        };

        private void PlatformResolveRenderTargets()
        {
            if (!this._mainContext.Strategy.IsRenderTargetBound)
                return;

            var renderTargetBinding = _mainContext.Strategy._currentRenderTargetBindings[0];
            var renderTarget = renderTargetBinding.RenderTarget as IRenderTarget;
            if (renderTarget.MultiSampleCount > 0 && _supportsBlitFramebuffer)
            {
                int glResolveFramebuffer = 0;
                if (!((ConcreteGraphicsContext)_mainContext.Strategy)._glResolveFramebuffers.TryGetValue(_mainContext.Strategy._currentRenderTargetBindings, out glResolveFramebuffer))
                {
                    glResolveFramebuffer = GL.GenFramebuffer();
                    GraphicsExtensions.CheckGLError();
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, glResolveFramebuffer);
                    GraphicsExtensions.CheckGLError();

                    for (int i = 0; i < _mainContext.Strategy._currentRenderTargetCount; i++)
                    {
                        var renderTargetGL = (IRenderTargetGL)_mainContext.Strategy._currentRenderTargetBindings[i].RenderTarget;
                        TextureTarget target = renderTargetGL.GetFramebufferTarget(renderTargetBinding.ArraySlice);                       
                        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, target, renderTargetGL.GLTexture, 0);
                        GraphicsExtensions.CheckGLError();
                    }
                    ((ConcreteGraphicsContext)_mainContext.Strategy)._glResolveFramebuffers.Add((RenderTargetBinding[])_mainContext.Strategy._currentRenderTargetBindings.Clone(), glResolveFramebuffer);
                }
                else
                {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, glResolveFramebuffer);
                    GraphicsExtensions.CheckGLError();
                }

                // The only fragment operations which affect the resolve are the pixel ownership test, the scissor test, and dithering.
                if (((ConcreteGraphicsContext)_mainContext.Strategy)._lastRasterizerState.ScissorTestEnable)
                {
                    GL.Disable(EnableCap.ScissorTest);
                    GraphicsExtensions.CheckGLError();
                }

                int glFramebuffer = ((ConcreteGraphicsContext)_mainContext.Strategy)._glFramebuffers[_mainContext.Strategy._currentRenderTargetBindings];
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, glFramebuffer);
                GraphicsExtensions.CheckGLError();

                for (int i = 0; i < _mainContext.Strategy._currentRenderTargetCount; i++)
                {
                    renderTargetBinding = _mainContext.Strategy._currentRenderTargetBindings[i];
                    renderTarget = renderTargetBinding.RenderTarget as IRenderTarget;
                    GL.ReadBuffer(ReadBufferMode.ColorAttachment0 + i);
                    GraphicsExtensions.CheckGLError();
                    GL.DrawBuffer(DrawBufferMode.ColorAttachment0 + i);
                    GraphicsExtensions.CheckGLError();
                    GL.BlitFramebuffer(0, 0, renderTarget.Width, renderTarget.Height, 0, 0, renderTarget.Width, renderTarget.Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
                    GraphicsExtensions.CheckGLError();
                }

                if (renderTarget.RenderTargetUsage == RenderTargetUsage.DiscardContents && _supportsInvalidateFramebuffer)
                {
                    Debug.Assert(_supportsInvalidateFramebuffer);
                    GL.InvalidateFramebuffer(FramebufferTarget.Framebuffer, 3, InvalidateFramebufferAttachements);
                    GraphicsExtensions.CheckGLError();
                }

                if (((ConcreteGraphicsContext)_mainContext.Strategy)._lastRasterizerState.ScissorTestEnable)
                {
                    GL.Enable(EnableCap.ScissorTest);
                    GraphicsExtensions.CheckGLError();
                }
            }

            for (int i = 0; i < _mainContext.Strategy._currentRenderTargetCount; i++)
            {
                renderTargetBinding = _mainContext.Strategy._currentRenderTargetBindings[i];
                if (renderTargetBinding.RenderTarget.LevelCount > 1)
                {
                    var renderTargetGL = (IRenderTargetGL)renderTargetBinding.RenderTarget;
                    GL.BindTexture(renderTargetGL.GLTarget, renderTargetGL.GLTexture);
                    GraphicsExtensions.CheckGLError();
                    GL.GenerateMipmap(renderTargetGL.GLTarget);
                    GraphicsExtensions.CheckGLError();
                }
            }
        }

        private IRenderTarget PlatformApplyRenderTargets()
        {
            int glFramebuffer = 0;
            if (!((ConcreteGraphicsContext)_mainContext.Strategy)._glFramebuffers.TryGetValue(_mainContext.Strategy._currentRenderTargetBindings, out glFramebuffer))
            {
                glFramebuffer = GL.GenFramebuffer();
                GraphicsExtensions.CheckGLError();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, glFramebuffer);
                GraphicsExtensions.CheckGLError();
                var renderTargetBinding = _mainContext.Strategy._currentRenderTargetBindings[0];
                var renderTargetGL = (IRenderTargetGL)renderTargetBinding.RenderTarget;
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, renderTargetGL.GLDepthBuffer);
                GraphicsExtensions.CheckGLError();
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, renderTargetGL.GLStencilBuffer);
                GraphicsExtensions.CheckGLError();

                for (int i = 0; i < _mainContext.Strategy._currentRenderTargetCount; i++)
                {
                    renderTargetBinding = _mainContext.Strategy._currentRenderTargetBindings[i];
                    var renderTarget = (IRenderTarget)renderTargetBinding.RenderTarget;
                    renderTargetGL = renderTargetBinding.RenderTarget as IRenderTargetGL;
                    var attachement = (FramebufferAttachment.ColorAttachment0 + i);
                    if (renderTargetGL.GLColorBuffer != renderTargetGL.GLTexture)
                    {
                        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachement, RenderbufferTarget.Renderbuffer, renderTargetGL.GLColorBuffer);
                        GraphicsExtensions.CheckGLError();
                    }
                    else
                    {
                        TextureTarget target = renderTargetGL.GetFramebufferTarget(renderTargetBinding.ArraySlice);
                        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachement, target, renderTargetGL.GLTexture, 0);
                        GraphicsExtensions.CheckGLError();
                    }
                }

                GraphicsExtensions.CheckFramebufferStatus();

                ((ConcreteGraphicsContext)_mainContext.Strategy)._glFramebuffers.Add((RenderTargetBinding[])_mainContext.Strategy._currentRenderTargetBindings.Clone(), glFramebuffer);
            }
            else
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, glFramebuffer);
                GraphicsExtensions.CheckGLError();
            }

#if DESKTOPGL
            GL.DrawBuffers(_mainContext.Strategy._currentRenderTargetCount, ((ConcreteGraphicsContext)_mainContext.Strategy)._drawBuffers);
#endif

            // Reset the raster state because we flip vertices
            // when rendering offscreen and hence the cull direction.
            _mainContext.Strategy._rasterizerStateDirty = true;

            // Textures will need to be rebound to render correctly in the new render target.
            _mainContext.Strategy.Textures.Dirty();

            return _mainContext.Strategy._currentRenderTargetBindings[0].RenderTarget as IRenderTarget;
        }

        /// <summary>
        /// Activates the Current Vertex/Pixel shader pair into a program.         
        /// </summary>
        private unsafe void ActivateShaderProgram()
        {
            // Lookup the shader program.
            ShaderProgram shaderProgram = _programCache.GetProgram(_mainContext.Strategy.VertexShader, _mainContext.Strategy.PixelShader, ShaderProgramHash2);
            if (shaderProgram.Program == -1)
                return;

            // Set the new program if it has changed.
            if (((ConcreteGraphicsContext)_mainContext.Strategy)._shaderProgram != shaderProgram)
            {
                GL.UseProgram(shaderProgram.Program);
                GraphicsExtensions.CheckGLError();
                ((ConcreteGraphicsContext)_mainContext.Strategy)._shaderProgram = shaderProgram;
            }

            int posFixupLoc = shaderProgram.GetUniformLocation("posFixup");
            if (posFixupLoc == -1)
                return;

            // Apply vertex shader fix:
            // The following two lines are appended to the end of vertex shaders
            // to account for rendering differences between OpenGL and DirectX:
            //
            // gl_Position.y = gl_Position.y * posFixup.y;
            // gl_Position.xy += posFixup.zw * gl_Position.ww;
            //
            // (the following paraphrased from wine, wined3d/state.c and wined3d/glsl_shader.c)
            //
            // - We need to flip along the y-axis in case of offscreen rendering.
            // - D3D coordinates refer to pixel centers while GL coordinates refer
            //   to pixel corners.
            // - D3D has a top-left filling convention. We need to maintain this
            //   even after the y-flip mentioned above.
            // In order to handle the last two points, we translate by
            // (63.0 / 128.0) / VPw and (63.0 / 128.0) / VPh. This is equivalent to
            // translating slightly less than half a pixel. We want the difference to
            // be large enough that it doesn't get lost due to rounding inside the
            // driver, but small enough to prevent it from interfering with any
            // anti-aliasing.
            //
            // OpenGL coordinates specify the center of the pixel while d3d coords specify
            // the corner. The offsets are stored in z and w in posFixup. posFixup.y contains
            // 1.0 or -1.0 to turn the rendering upside down for offscreen rendering. PosFixup.x
            // contains 1.0 to allow a mad.

            ((ConcreteGraphicsContext)_mainContext.Strategy)._posFixup.X = 1.0f;
            ((ConcreteGraphicsContext)_mainContext.Strategy)._posFixup.Y = 1.0f;
            if (!UseHalfPixelOffset)
            {
                ((ConcreteGraphicsContext)_mainContext.Strategy)._posFixup.Z = 0f;
                ((ConcreteGraphicsContext)_mainContext.Strategy)._posFixup.W = 0f;
            }
            else
            {
                ((ConcreteGraphicsContext)_mainContext.Strategy)._posFixup.Z =  (63.0f/64.0f)/Viewport.Width;
                ((ConcreteGraphicsContext)_mainContext.Strategy)._posFixup.W = -(63.0f/64.0f)/Viewport.Height;
            }

            //If we have a render target bound (rendering offscreen)
            if (_mainContext.Strategy.IsRenderTargetBound)
            {
                //flip vertically
                ((ConcreteGraphicsContext)_mainContext.Strategy)._posFixup.Y = -((ConcreteGraphicsContext)_mainContext.Strategy)._posFixup.Y;
                ((ConcreteGraphicsContext)_mainContext.Strategy)._posFixup.W = -((ConcreteGraphicsContext)_mainContext.Strategy)._posFixup.W;
            }
            
            GL.Uniform4(posFixupLoc, ((ConcreteGraphicsContext)_mainContext.Strategy)._posFixup);
            GraphicsExtensions.CheckGLError();
        }

        private void PlatformApplyState()
        {
            Threading.EnsureUIThread();

            {
                ((ConcreteGraphicsContext)_mainContext.Strategy).PlatformApplyBlend();
            }

            if (_mainContext.Strategy._depthStencilStateDirty)
            {
                _mainContext.Strategy._actualDepthStencilState.PlatformApplyState(_mainContext.Strategy, this);
                _mainContext.Strategy._depthStencilStateDirty = false;
            }

            if (_mainContext.Strategy._rasterizerStateDirty)
            {
                _mainContext.Strategy._actualRasterizerState.PlatformApplyState(_mainContext.Strategy, this);
                _mainContext.Strategy._rasterizerStateDirty = false;
            }

            if (_mainContext.Strategy._scissorRectangleDirty)
            {
                ((ConcreteGraphicsContext)_mainContext.Strategy).PlatformApplyScissorRectangle();
                _mainContext.Strategy._scissorRectangleDirty = false;
            }
        }


        private void PlatformApplyViewport()
        {
            if (_mainContext.Strategy.IsRenderTargetBound)
                GL.Viewport(_viewport.X, _viewport.Y, _viewport.Width, _viewport.Height);
            else
                GL.Viewport(_viewport.X, PresentationParameters.BackBufferHeight - _viewport.Y - _viewport.Height, _viewport.Width, _viewport.Height);
            GraphicsExtensions.LogGLError("GraphicsDevice.Viewport_set() GL.Viewport");

            GL.DepthRange(_viewport.MinDepth, _viewport.MaxDepth);
            GraphicsExtensions.LogGLError("GraphicsDevice.Viewport_set() GL.DepthRange");

            // In OpenGL we have to re-apply the special "posFixup"
            // vertex shader uniform if the viewport changes.
            _mainContext.Strategy._vertexShaderDirty = true;
        }

        private void PlatformApplyIndexBuffer()
        {
            if (_mainContext.Strategy._indexBufferDirty)
            {
                if (_mainContext.Strategy._indexBuffer != null)
                {
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _mainContext.Strategy._indexBuffer._ibo);
                    GraphicsExtensions.CheckGLError();
                }
                _mainContext.Strategy._indexBufferDirty = false;
            }
        }

        private void PlatformApplyVertexBuffers()
        {
        }

        private void PlatformApplyShaders()
        {
            if (_mainContext.Strategy._vertexShader == null)
                throw new InvalidOperationException("A vertex shader must be set!");
            if (_mainContext.Strategy._pixelShader == null)
                throw new InvalidOperationException("A pixel shader must be set!");

            if (_mainContext.Strategy._vertexShaderDirty || _mainContext.Strategy._pixelShaderDirty)
            {
                ActivateShaderProgram();

                if (_mainContext.Strategy._vertexShaderDirty)
                {
                    unchecked { CurrentContext._graphicsMetrics._vertexShaderCount++; }
                }

                if (_mainContext.Strategy._pixelShaderDirty)
                {
                    unchecked { CurrentContext._graphicsMetrics._pixelShaderCount++; }
                }

                _mainContext.Strategy._vertexShaderDirty = false;
                _mainContext.Strategy._pixelShaderDirty = false;
            }

            _mainContext.Strategy._vertexConstantBuffers.Apply(_mainContext);
            _mainContext.Strategy._pixelConstantBuffers.Apply(_mainContext);

            _mainContext.Strategy.VertexTextures.PlatformApply();
            _mainContext.Strategy.VertexSamplerStates.PlatformApply();
            _mainContext.Strategy.Textures.PlatformApply();
            _mainContext.Strategy.SamplerStates.PlatformApply();
        }

        private void PlatformDrawIndexedPrimitives(PrimitiveType primitiveType, int baseVertex, int startIndex, int primitiveCount)
        {
            PlatformApplyState();
            PlatformApplyIndexBuffer();
            PlatformApplyVertexBuffers();
            PlatformApplyShaders();

            bool shortIndices = _mainContext.Strategy._indexBuffer.IndexElementSize == IndexElementSize.SixteenBits;

			var indexElementType = shortIndices ? DrawElementsType.UnsignedShort : DrawElementsType.UnsignedInt;
            int indexElementSize = shortIndices ? 2 : 4;
            IntPtr indexOffsetInBytes = (IntPtr)(startIndex * indexElementSize);
            int indexElementCount = GraphicsContextStrategy.GetElementCountArray(primitiveType, primitiveCount);
			var target = ConcreteGraphicsContext.PrimitiveTypeGL(primitiveType);

            PlatformApplyVertexBuffersAttribs(_mainContext.Strategy._vertexShader, baseVertex);

            GL.DrawElements(target,
                                     indexElementCount,
                                     indexElementType,
                                     indexOffsetInBytes);
            GraphicsExtensions.CheckGLError();
        }

        private void PlatformDrawUserPrimitives<T>(
            PrimitiveType primitiveType, T[] vertexData, int vertexOffset, VertexDeclaration vertexDeclaration, int vertexCount)
            where T : struct
        {
            PlatformApplyState();
            //PlatformApplyIndexBuffer();
            //PlatformApplyVertexBuffers();
            PlatformApplyShaders();

            // Unbind current VBOs.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            _mainContext.Strategy._vertexBuffersDirty = true;
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            _mainContext.Strategy._vertexBuffersDirty = true;

            // Pin the buffers.
            GCHandle vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
            try
            {
                // Setup the vertex declaration to point at the VB data.
                vertexDeclaration.GraphicsDevice = this;
                PlatformApplyUserVertexDataAttribs(vertexDeclaration, _mainContext.Strategy._vertexShader, vbHandle.AddrOfPinnedObject());

                //Draw
                GL.DrawArrays(ConcreteGraphicsContext.PrimitiveTypeGL(primitiveType),
                              vertexOffset,
                              vertexCount);
                GraphicsExtensions.CheckGLError();
            }
            finally
            {
                // Release the handles.
                vbHandle.Free();
            }
        }

        private void PlatformDrawPrimitives(PrimitiveType primitiveType, int vertexStart, int vertexCount)
        {
            PlatformApplyState();
            PlatformApplyIndexBuffer();
            PlatformApplyVertexBuffers();
            PlatformApplyShaders();

            PlatformApplyVertexBuffersAttribs(_mainContext.Strategy._vertexShader, 0);

            if (vertexStart < 0)
                vertexStart = 0;

			GL.DrawArrays(ConcreteGraphicsContext.PrimitiveTypeGL(primitiveType),
			              vertexStart,
			              vertexCount);
            GraphicsExtensions.CheckGLError();
        }

        private void PlatformDrawUserIndexedPrimitives<T>(
            PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, short[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration)
            where T : struct
        {
            PlatformApplyState();
            //PlatformApplyIndexBuffer();
            //PlatformApplyVertexBuffers();
            PlatformApplyShaders();

            // Unbind current VBOs.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            _mainContext.Strategy._vertexBuffersDirty = true;
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            _mainContext.Strategy._indexBufferDirty = true;

            // Pin the buffers.
            GCHandle vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
            GCHandle ibHandle = GCHandle.Alloc(indexData, GCHandleType.Pinned);
            try
            {
                IntPtr vertexAddr = (IntPtr)(vbHandle.AddrOfPinnedObject().ToInt64() + vertexDeclaration.VertexStride * vertexOffset);

                // Setup the vertex declaration to point at the VB data.
                vertexDeclaration.GraphicsDevice = this;
                PlatformApplyUserVertexDataAttribs(vertexDeclaration, _mainContext.Strategy._vertexShader, vertexAddr);

                //Draw
                GL.DrawElements(
                    ConcreteGraphicsContext.PrimitiveTypeGL(primitiveType),
                    GraphicsContextStrategy.GetElementCountArray(primitiveType, primitiveCount),
                    DrawElementsType.UnsignedShort,
                    (IntPtr)(ibHandle.AddrOfPinnedObject().ToInt64() + (indexOffset * sizeof(short))));
                GraphicsExtensions.CheckGLError();
            }
            finally
            {
                // Release the handles.
                ibHandle.Free();
                vbHandle.Free();
            }
        }

        private void PlatformDrawUserIndexedPrimitives<T>(
            PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, int[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration)
            where T : struct
        {
            PlatformApplyState();
            //PlatformApplyIndexBuffer();
            //PlatformApplyVertexBuffers();
            PlatformApplyShaders();

            // Unbind current VBOs.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            _mainContext.Strategy._vertexBuffersDirty = true;
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GraphicsExtensions.CheckGLError();
            _mainContext.Strategy._indexBufferDirty = true;

            // Pin the buffers.
            GCHandle vbHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
            GCHandle ibHandle = GCHandle.Alloc(indexData, GCHandleType.Pinned);
            try
            {
                IntPtr vertexAddr = (IntPtr)(vbHandle.AddrOfPinnedObject().ToInt64() + vertexDeclaration.VertexStride * vertexOffset);

                // Setup the vertex declaration to point at the VB data.
                vertexDeclaration.GraphicsDevice = this;
                PlatformApplyUserVertexDataAttribs(vertexDeclaration, _mainContext.Strategy._vertexShader, vertexAddr);

                //Draw
                GL.DrawElements(
                    ConcreteGraphicsContext.PrimitiveTypeGL(primitiveType),
                    GraphicsContextStrategy.GetElementCountArray(primitiveType, primitiveCount),
                    DrawElementsType.UnsignedInt,
                    (IntPtr)(ibHandle.AddrOfPinnedObject().ToInt64() + (indexOffset * sizeof(int))));
                GraphicsExtensions.CheckGLError();
            }
            finally
            {
                // Release the handles.
                ibHandle.Free();
                vbHandle.Free();
            }
        }

        private void PlatformDrawInstancedPrimitives(PrimitiveType primitiveType, int baseVertex, int startIndex, int primitiveCount, int baseInstance, int instanceCount)
        {
            if (!Capabilities.SupportsInstancing)
                throw new PlatformNotSupportedException("Instanced geometry drawing requires at least OpenGL 3.2 or GLES 3.2. Try upgrading your graphics card drivers.");

            PlatformApplyState();
            PlatformApplyIndexBuffer();
            PlatformApplyVertexBuffers();
            PlatformApplyShaders();

            bool shortIndices = _mainContext.Strategy._indexBuffer.IndexElementSize == IndexElementSize.SixteenBits;

            var indexElementType = shortIndices ? DrawElementsType.UnsignedShort : DrawElementsType.UnsignedInt;
            int indexElementSize = shortIndices ? 2 : 4;
            IntPtr indexOffsetInBytes = (IntPtr)(startIndex * indexElementSize);
            int indexElementCount = GraphicsContextStrategy.GetElementCountArray(primitiveType, primitiveCount);
            var target = ConcreteGraphicsContext.PrimitiveTypeGL(primitiveType);

            PlatformApplyVertexBuffersAttribs(_mainContext.Strategy._vertexShader, baseVertex);

            if (baseInstance > 0)
            {
                if (!Capabilities.SupportsBaseIndexInstancing)
                    throw new PlatformNotSupportedException("Instanced geometry drawing with base instance requires at least OpenGL 4.2. Try upgrading your graphics card drivers.");

                GL.DrawElementsInstancedBaseInstance(target,
                                          indexElementCount,
                                          indexElementType,
                                          indexOffsetInBytes,
                                          instanceCount,
                                          baseInstance);
            }
            else
                GL.DrawElementsInstanced(target,
                                     indexElementCount,
                                     indexElementType,
                                     indexOffsetInBytes,
                                     instanceCount);

            GraphicsExtensions.CheckGLError();
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

            ApplyRenderTargets(null);
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
            ScissorRectangle = _viewport.Bounds;

            // Set the default render target.
            ApplyRenderTargets(null);
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
