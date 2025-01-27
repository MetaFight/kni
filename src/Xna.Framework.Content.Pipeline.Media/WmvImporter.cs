﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Content.Pipeline
{
    /// <summary>
    /// Provides methods for reading Windows Media Video (.wmv) files for use in the Content Pipeline.
    /// </summary>
    [ContentImporter(".wmv", DisplayName = "Wmv Importer - KNI", DefaultProcessor = "VideoProcessor")]
    public class WmvImporter : ContentImporter<VideoContent>
    {
        /// <summary>
        /// Initializes a new instance of WmvImporter.
        /// </summary>
        public WmvImporter()
        {
        }

        /// <summary>
        /// Called by the XNA Framework when importing a .wmv file to be used as a game asset. This is the method called by the XNA Framework when an asset is to be imported into an object that can be recognized by the Content Pipeline.
        /// </summary>
        /// <param name="filename">Name of a game asset file.</param>
        /// <param name="context">Contains information for importing a game asset, such as a logger interface.</param>
        /// <returns>Resulting game asset.</returns>
        public override VideoContent Import(string filename, ContentImporterContext context)
        {
            var content = new VideoContent(filename);
            return content;
        }
    }
}
