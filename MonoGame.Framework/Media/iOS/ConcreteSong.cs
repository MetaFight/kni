// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2022 Nick Kastellanos

using System;
using Microsoft.Xna.Framework.Media;
using AVFoundation;
using Foundation;
using MediaPlayer;


namespace Microsoft.Xna.Platform.Media
{
    internal sealed class ConcreteSongStrategy : SongStrategy
    {
        private Uri _streamSource;

        #if !TVOS
        internal MPMediaItem _mediaItem;
        #endif
        internal NSUrl _assetUrl;

        private AVPlayer _player; // TODO: Move _player to MediaPlayer
        private NSObject _playToEndObserver;
        private AVPlayerItem _sound;

        internal Uri StreamSource { get { return _streamSource; } }

        [CLSCompliant(false)]
        public NSUrl AssetUrl { get { return this._assetUrl; } }
        internal AVPlayer Player { get { return _player; } }

        internal ConcreteSongStrategy()
        {
        }

        internal ConcreteSongStrategy(string name, Uri streamSource)
        {
            this.Name = name;
            this._streamSource = streamSource;

            NSUrl nsUrl = NSUrl.FromFilename(streamSource.OriginalString);
            this.CreatePlayer(nsUrl);
        }

        internal void CreatePlayer(NSUrl url)
        {
            _sound = AVPlayerItem.FromUrl(url);
            _player = AVPlayer.FromPlayerItem(_sound);
            _playToEndObserver = AVPlayerItem.Notifications.ObserveDidPlayToEndTime(OnFinishedPlaying);
        }

        internal delegate void FinishedPlayingHandler(object sender, EventArgs args);
        event FinishedPlayingHandler DonePlaying;

        private void OnFinishedPlaying(object sender, NSNotificationEventArgs args)
		{
            var handler = DonePlaying;
            if (handler != null)
                handler(this, EventArgs.Empty);
		}

		/// <summary>
		/// Set the event handler for "Finished Playing". Done this way to prevent multiple bindings.
		/// </summary>
		internal void SetEventHandler(FinishedPlayingHandler handler)
		{
			if (DonePlaying == null)
			    DonePlaying += handler;
		}

        public override Album Album
        {
            get { return base.Album; }
        }

        public override Artist Artist
        {
            get { return base.Artist; }
        }

        public override Genre Genre
        {
            get { return base.Genre; }
        }

        public override TimeSpan Duration
        {
            get { return base.Duration; }
        }

        public override bool IsProtected
        {
            get { return base.IsProtected; }
        }

        public override bool IsRated
        {
            get { return base.IsRated; }
        }

        internal override string Filename
        {
            get
            {
                if (this.StreamSource == null)
                    return this.Name;

                return StreamSource.OriginalString;
            }
        }

        public override string Name
        {
            get { return base.Name; }
        }

        public override int PlayCount
        {
            get { return base.PlayCount; }
        }

        public override int Rating
        {
            get { return base.Rating; }
        }

        public override int TrackNumber
        {
            get { return base.TrackNumber; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_sound != null)
                {
                    _playToEndObserver.Dispose();
                    _sound.Dispose();
                    _player.Dispose();
                }

                _playToEndObserver = null;
                _sound = null;
                _player = null;
            }

            //base.Dispose(disposing);
        }
    }
}

