﻿// Copyright (C)2023 Nick Kastellanos

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using nkast.Wasm.Canvas.WebGL;


namespace Microsoft.Xna.Platform.Graphics
{
    public abstract class ConcreteShader : ShaderStrategy
    {
        // We keep this around for recompiling on context lost and debugging.
        private byte[] _shaderBytecode;

        // The shader handle.
        private WebGLShader _shaderHandle = null;

        internal byte[] ShaderBytecode { get { return _shaderBytecode; } }
        protected WebGLShader ShaderHandle { get { return _shaderHandle; } }

        internal ConcreteShader(GraphicsContextStrategy contextStrategy, byte[] shaderBytecode, SamplerInfo[] samplers, int[] cBuffers, VertexAttribute[] attributes, ShaderProfileType profile)
            : base(contextStrategy, shaderBytecode, samplers, cBuffers, attributes, profile)
        {
            if (profile != ShaderProfileType.OpenGL_Mojo)
                throw new Exception("This effect was built for a different platform.");

            _shaderBytecode = shaderBytecode;
            _hashKey = MonoGame.Framework.Utilities.Hash.ComputeHash(_shaderBytecode);
        }

        internal void CreateShader(WebGLShaderType shaderType)
        {
            var GL = GraphicsDevice.Strategy.CurrentContext.Strategy.ToConcrete<ConcreteGraphicsContext>().GL;

            _shaderHandle = GL.CreateShader(shaderType);
            GraphicsExtensions.CheckGLError();
            string glslCode = System.Text.Encoding.ASCII.GetString(_shaderBytecode);
            GL.ShaderSource(_shaderHandle, glslCode);
            GraphicsExtensions.CheckGLError();
            GL.CompileShader(_shaderHandle);
            GraphicsExtensions.CheckGLError();
            bool compiled = false;
            compiled = GL.GetShaderParameter(_shaderHandle, WebGLShaderStatus.COMPILE);
            GraphicsExtensions.CheckGLError();
            if (compiled != true)
            {
                string log = GL.GetShaderInfoLog(_shaderHandle);
                _shaderHandle.Dispose();
                _shaderHandle = null;

                throw new InvalidOperationException("Shader Compilation Failed");
            }
        }

        internal override void PlatformGraphicsDeviceResetting()
        {
            if (_shaderHandle != null)
            {
                _shaderHandle.Dispose();
                _shaderHandle = null;
            }

            base.PlatformGraphicsDeviceResetting();
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_shaderHandle != null)
                {
                    _shaderHandle.Dispose();
                    _shaderHandle = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}