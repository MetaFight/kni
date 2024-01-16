// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
using System.Threading.Tasks;

#if UAP
using Windows.ApplicationModel.Resources.Core;
#endif

#if WINUI
using Microsoft.Windows.ApplicationModel.Resources;
#endif

namespace Microsoft.Xna.Framework
{
    partial class TitleContainer
    {
        static internal ResourceContext ResourceContext;
        static internal ResourceMap FileResourceMap;

        static partial void PlatformInit()
        {
            Location = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;

            ResourceContext = new ResourceContext();
            FileResourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("Files");
        }

        private static async Task<Stream> OpenStreamAsync(string name)
        {
            try
            {
                Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///" + name));
                var randomAccessStream = await file.OpenReadAsync();
                return randomAccessStream.AsStreamForRead();
            }
            catch (IOException)
            {
                // The file must not exist... return a null stream.
                return null;
            }
        }

        private static Stream PlatformOpenStream(string safeName)
        {
            return Task.Run(() => OpenStreamAsync(safeName).Result).Result;
        }
    }
}

