﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using TOutput = Microsoft.Xna.Framework.BoundingSphere;

namespace Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler
{
    /// <summary>
    /// Writes the BoundingSphere value to the output.
    /// </summary>
    [ContentTypeWriter]
    class BoundingSphereWriter : ContentTypeWriterBase<TOutput>
    {
        /// <summary>
        /// Writes the value to the output.
        /// </summary>
        /// <param name="output">The output writer object.</param>
        /// <param name="value">The value to write to the output.</param>
        protected override void Write(ContentWriter output, TOutput value)
        {
            output.Write(value.Center);
            output.Write(value.Radius);
        }
    }
}
