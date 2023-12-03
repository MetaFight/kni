// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2023 Nick Kastellanos

using System;
using Microsoft.Xna.Platform.Graphics;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class BlendState : GraphicsResource
    {
        internal IBlendStateStrategy _strategy;

        public static readonly BlendState Additive;
        public static readonly BlendState AlphaBlend;
        public static readonly BlendState NonPremultiplied;
        public static readonly BlendState Opaque;

        static BlendState()
        {
            Additive = new BlendState("BlendState.Additive", Blend.SourceAlpha, Blend.One);
            AlphaBlend = new BlendState("BlendState.AlphaBlend", Blend.One, Blend.InverseSourceAlpha);
            NonPremultiplied = new BlendState("BlendState.NonPremultiplied", Blend.SourceAlpha, Blend.InverseSourceAlpha);
            Opaque = new BlendState("BlendState.Opaque", Blend.One, Blend.Zero);
        }

        /// <summary>
        /// Enables use of the per-target blend states.
        /// </summary>
        public bool IndependentBlendEnable
        {
            get { return _strategy.IndependentBlendEnable; }
            set
            {
                if (_strategy is ReadonlyBlendStateStrategy)
                    throw new InvalidOperationException("You cannot modify a default blend state object.");
                if (GraphicsDevice != null)
                    throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");

                _strategy.IndependentBlendEnable = value;
            }
        }

        /// <summary>
        /// Returns the target specific blend state.
        /// </summary>
        /// <param name="index">The 0 to 3 target blend state index.</param>
        /// <returns>A target blend state.</returns>
        public TargetBlendState this[int index]
        {
            get { return _strategy.Targets[index]; }
        }

        public BlendFunction AlphaBlendFunction
        {
            get { return _strategy.AlphaBlendFunction; }
            set
            {
                 if (GraphicsDevice != null)
                    throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");

                _strategy.AlphaBlendFunction = value;
            }
        }

        public Blend AlphaDestinationBlend
        {
            get { return _strategy.AlphaDestinationBlend; }
            set
            {
                if (GraphicsDevice != null)
                    throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");

                _strategy.AlphaDestinationBlend = value;
            }
        }

        public Blend AlphaSourceBlend
        {
            get { return _strategy.AlphaSourceBlend; }
            set
            {
                if (GraphicsDevice != null)
                    throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");

                _strategy.AlphaSourceBlend = value;
            }
        }

        public BlendFunction ColorBlendFunction
        {
            get { return _strategy.ColorBlendFunction; }
            set
            {
                if (GraphicsDevice != null)
                    throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");

                _strategy.ColorBlendFunction = value;
            }
        }

        public Blend ColorDestinationBlend
        {
            get { return _strategy.ColorDestinationBlend; }
            set
            {
                 if (GraphicsDevice != null)
                    throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");

                _strategy.ColorDestinationBlend = value;
            }
        }

        public Blend ColorSourceBlend
        {
            get { return _strategy.ColorSourceBlend; }
            set
            {
                if (GraphicsDevice != null)
                    throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");

                _strategy.ColorSourceBlend = value;
            }
        }

        public ColorWriteChannels ColorWriteChannels
        {
            get { return _strategy.ColorWriteChannels; }
            set
            {
                 if (GraphicsDevice != null)
                    throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");

                _strategy.ColorWriteChannels = value;
            }
        }

        public ColorWriteChannels ColorWriteChannels1
        {
            get { return _strategy.ColorWriteChannels1; }
            set
            {
                if (GraphicsDevice != null)
                    throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");

                _strategy.ColorWriteChannels1 = value;
            }
        }

        public ColorWriteChannels ColorWriteChannels2
        {
            get { return _strategy.ColorWriteChannels2; }
            set
            {
                 if (GraphicsDevice != null)
                    throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");

                _strategy.ColorWriteChannels2 = value;
            }
        }

        public ColorWriteChannels ColorWriteChannels3
        {
            get { return _strategy.ColorWriteChannels3; }
            set
            {
                if (GraphicsDevice != null)
                    throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");

                _strategy.ColorWriteChannels3 = value;
            }
        }

        /// <summary>
        /// The color used as blend factor when alpha blending.
        /// </summary>
        /// <remarks>
        /// <see cref="P:Microsoft.Xna.Framework.Graphics.GraphicsDevice.BlendFactor"/> is set to this value when this <see cref="BlendState"/>
        /// is bound to a GraphicsDevice.
        /// </remarks>
        public Color BlendFactor
        {
            get { return _strategy.BlendFactor; }
            set
            {
                if (GraphicsDevice != null)
                    throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");

                _strategy.BlendFactor = value;
            }
        }

        public int MultiSampleMask
        {
            get { return _strategy.MultiSampleMask; }
            set
            {
                if (GraphicsDevice != null)
                    throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");

                _strategy.MultiSampleMask = value;
            }
        }

        internal void BindToGraphicsDevice(GraphicsDevice device)
        {
            if (_strategy is ReadonlyBlendStateStrategy)
                throw new InvalidOperationException("You cannot bind a default state object.");

            if (this.GraphicsDevice != device)
            {
                if (this.GraphicsDevice == null)
                {
                    System.Diagnostics.Debug.Assert(device != null);
                    BindGraphicsDevice(device.Strategy);
                }
                else
                    throw new InvalidOperationException("This blend state is already bound to a different graphics device.");
            }
        }

        public BlendState()
            : base()
        {
            _strategy = new BlendStateStrategy(this);
        }

        private BlendState(string name, Blend sourceBlend, Blend destinationBlend)
            : base()
        {
            Name = name;
            _strategy = new ReadonlyBlendStateStrategy(sourceBlend, destinationBlend, this);
        }

        internal BlendState(BlendState source)
            : base()
        {
            Name = source.Name;
            _strategy = new BlendStateStrategy(source._strategy, this);
        }

        partial void PlatformDispose(bool disposing);

        protected override void Dispose(bool disposing)
        {
            System.Diagnostics.Debug.Assert(!IsDisposed);

            if (disposing)
            {
            }

            for (int i = 0; i < _strategy.Targets.Length; i++)
                _strategy.Targets[i] = null;

            PlatformDispose(disposing);
            base.Dispose(disposing);
        }
    }
}

