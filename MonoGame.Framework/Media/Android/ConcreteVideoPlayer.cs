// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2023 Nick Kastellanos

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Android.Views;
using Android.Graphics;
using Microsoft.Xna.Platform.Graphics.OpenGL;


namespace Microsoft.Xna.Platform.Media
{
    internal sealed class ConcreteVideoPlayerStrategy : VideoPlayerStrategy
    {
        private const int GL_TEXTURE_EXTERNAL_OES = (0x00008D65);
        
        private Android.Media.MediaPlayer _player;
        private int _glVideoSurfaceTexture;
        private SurfaceTexture _surfaceTexture;
        private Surface _surface;

        private bool _frameAvailable;
        private byte[] _frameData;
        private Texture2D _lastFrame;

        public override MediaState State
        {
            get { return base.State; }
            protected set { base.State = value; }
        }

        public override bool IsLooped
        {
            get { return base.IsLooped; }
            set
            {
                base.IsLooped = value;

                _player.Looping = true;
            }
        }

        public override bool IsMuted
        {
            get { return base.IsMuted; }
            set
            {
                base.IsMuted = value;
                throw new NotImplementedException();
            }
        }

        public override TimeSpan PlayPosition
        {
            get { return TimeSpan.FromMilliseconds(_player.CurrentPosition); }
        }

        public override float Volume
        {
            get { return base.Volume; }
            set
            {
                base.Volume = value;
                
                PlatformSetVolume();
            }
        }

        internal ConcreteVideoPlayerStrategy()
        {
            _player = new Android.Media.MediaPlayer();

            _glVideoSurfaceTexture = GL.GenTexture();
            GraphicsExtensions.CheckGLError();
            base.Video.GraphicsDevice.CurrentContext.Textures.Strategy.Dirty(0);
            GL.ActiveTexture(TextureUnit.Texture0 + 0);
            GraphicsExtensions.CheckGLError();
            GL.BindTexture((TextureTarget)GL_TEXTURE_EXTERNAL_OES, _glVideoSurfaceTexture);
            GraphicsExtensions.CheckGLError();
            GL.TexParameter((TextureTarget)GL_TEXTURE_EXTERNAL_OES, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GraphicsExtensions.CheckGLError();
            GL.TexParameter((TextureTarget)GL_TEXTURE_EXTERNAL_OES, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GraphicsExtensions.CheckGLError();
            GL.TexParameter((TextureTarget)GL_TEXTURE_EXTERNAL_OES, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GraphicsExtensions.CheckGLError();
            GL.TexParameter((TextureTarget)GL_TEXTURE_EXTERNAL_OES, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GraphicsExtensions.CheckGLError();

            _surfaceTexture = new SurfaceTexture(_glVideoSurfaceTexture);
            _surface = new Surface(_surfaceTexture);

            _player.SetSurface(_surface);
            _surfaceTexture.FrameAvailable += _surfaceTexture_FrameAvailable;
        }

        public override Texture2D PlatformGetTexture()
        {
            if (_lastFrame != null)
            {
                if (_lastFrame.Width != base.Video.Width || _lastFrame.Height != base.Video.Height)
                {
                    _lastFrame.Dispose();
                    _lastFrame = null;
                }
            }
            if (_lastFrame == null)
                _lastFrame = new Texture2D(base.Video.GraphicsDevice, base.Video.Width, base.Video.Height, false, SurfaceFormat.Color);

            if (_frameAvailable)
            {
                _frameAvailable = false;

                // Calculate the buffer size for RGBA format
                int frameBufferSize = base.Video.Width * base.Video.Height * 4;

                // Allocate memory for the frame data if needed
                if (_frameData == null || _frameData.Length != frameBufferSize)
                    _frameData = new byte[frameBufferSize];

                // Update the surface texture
                _surfaceTexture.UpdateTexImage();

                // Create a framebuffer
                int framebuffer = GL.GenFramebuffer();
                GraphicsExtensions.CheckGLError();

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
                GraphicsExtensions.CheckGLError();

                // Attach the texture to the framebuffer
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, (TextureTarget)GL_TEXTURE_EXTERNAL_OES, _glVideoSurfaceTexture, 0);
                GraphicsExtensions.CheckGLError();

                // Read the pixel data from the framebuffer
                GL.ReadPixels(0, 0, Video.Width, Video.Height, MonoGame.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, _frameData);
                GraphicsExtensions.CheckGLError();

                // Dettach framebuffer
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GraphicsExtensions.CheckGLError();

                // cleanup
                GL.DeleteFramebuffer(framebuffer);
                GraphicsExtensions.CheckGLError();

                _lastFrame.SetData(_frameData);
            }

            return _lastFrame;
        }
        
        protected override void PlatformUpdateState(ref MediaState state)
        {
        }


        public override void PlatformPlay(Video video)
        {
            var state = State;
            if (state == MediaState.Playing || state == MediaState.Paused)
            {
                _player.Stop();
                _player.Reset();
            }

            base.Video = video;
            
            var afd = Android.App.Application.Context.Assets.OpenFd(base.Video.FileName);
            if (afd == null)
                return;

            _player.SetDataSource(afd.FileDescriptor, afd.StartOffset, afd.Length);
            afd.Close();
            _player.Prepare();

            _player.Start();

            State = MediaState.Playing;
        }

        private void _surfaceTexture_FrameAvailable(object sender, SurfaceTexture.FrameAvailableEventArgs e)
        {
            _frameAvailable = true;
        }

        public override void PlatformPause()
        {
            _player.Pause();
            State = MediaState.Paused;
        }

        public override void PlatformResume()
        {
            _player.Start();
            State = MediaState.Playing;
        }

        public override void PlatformStop()
        {
            _player.Stop();
            _player.Reset();

            State = MediaState.Stopped;
        }

        private void PlatformSetVolume()
        {
            float logVolume = (float)Math.Pow(Volume, 2);
            _player.SetVolume(logVolume, logVolume);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_player != null)
                    _player.Dispose();
                _player = null;

                if (_surfaceTexture != null)
                    _surfaceTexture.Dispose();
                _surfaceTexture = null;

                if (_surface != null)
                    _surface.Dispose();
                _surface = null;
            }

            GL.DeleteTexture(_glVideoSurfaceTexture);

            base.Dispose(disposing);
        }
    }    
}
