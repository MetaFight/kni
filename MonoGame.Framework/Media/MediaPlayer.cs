// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2022 Nick Kastellanos

using System;
using Microsoft.Xna.Platform.Media;


namespace Microsoft.Xna.Framework.Media
{
    public sealed class MediaPlayer : IMediaPlayer
    {
        private static MediaPlayer _current;

        /// <summary>
        /// Returns the current FrameworkDispatcher instance.
        /// </summary> 
        internal static IMediaPlayer Current
        {
            get
            {
                lock (typeof(MediaPlayer))
                {
                    if (_current == null)
                    {
                        _current = new MediaPlayer();
                    }

                    return _current;
                }
            }
        }


        public static event EventHandler<EventArgs> ActiveSongChanged;
        public static event EventHandler<EventArgs> MediaStateChanged;


        #region Properties

        public static MediaQueue Queue
        {
            get { return Current.Queue; }
        }
        
        public static bool IsMuted
        {
            get { return Current.IsMuted; }
            set { Current.IsMuted = value; }
        }

        public static bool IsRepeating 
        {
            get { return Current.IsRepeating; }
            set { Current.IsRepeating = value; }
        }

        public static bool IsShuffled
        {
            get { return Current.IsShuffled; }
            set { Current.IsShuffled = value; }
        }

        public static bool IsVisualizationEnabled
        {
            get { return Current.IsVisualizationEnabled; }
            set { Current.IsVisualizationEnabled = value; }
        }

        public static TimeSpan PlayPosition
        {
            get { return Current.PlayPosition; }
        }

        public static MediaState State
        {
            get { return Current.State; }
        }
        
        public static bool GameHasControl
        {
            get { return Current.GameHasControl; }
        }
        
        public static float Volume
        {
            get { return Current.Volume; }
            set { Current.Volume = value; }
        }

        #endregion

        /// <summary>
        /// Play clears the current playback queue, and then queues up the specified song for playback. 
        /// Playback starts immediately at the beginning of the song.
        /// </summary>
        public static void Play(Song song)
        {
            MediaPlayer.Current.Play(song);
        }

        public static void Play(SongCollection collection)
        {
            MediaPlayer.Current.Play(collection);
        }

        public static void Play(SongCollection collection, int index)
        {
            MediaPlayer.Current.Play(collection, index);
        }

        public static void Pause()
        {
            MediaPlayer.Current.Pause();
        }

        public static void Resume()
        {
            MediaPlayer.Current.Resume();
        }

        public static void Stop()
        {
            MediaPlayer.Current.Stop();
        }

        public static void MoveNext()
        {
            MediaPlayer.Current.MoveNext();
        }

        public static void MovePrevious()
        {
            MediaPlayer.Current.MovePrevious();
        }

        private void OnActiveSongChanged(EventArgs args)
        {
            var handler = _activeSongChanged;
            if (handler != null)
                handler(this, args);

            var staticHandler = MediaPlayer.ActiveSongChanged;
            if (staticHandler != null)
                staticHandler(null, args);
        }

        private void OnMediaStateChanged(EventArgs args)
        {
            var handler = _mediaStateChanged;
            if (handler != null)
                handler(this, args);

            var staticHandler = MediaPlayer.MediaStateChanged;
            if (staticHandler != null)
                staticHandler(null, args);
        }


        private MediaPlayerStrategy _strategy;

        private event EventHandler<EventArgs> _activeSongChanged;
        private event EventHandler<EventArgs> _mediaStateChanged;


        internal MediaPlayerStrategy Strategy { get { return _strategy; } }


        event EventHandler<EventArgs> IMediaPlayer.ActiveSongChanged
        {
            add { _activeSongChanged += value; }
            remove { ActiveSongChanged -= value; }
        }

        event EventHandler<EventArgs> IMediaPlayer.MediaStateChanged
        {
            add { _mediaStateChanged += value; }
            remove { _mediaStateChanged -= value; }
        }


        MediaQueue IMediaPlayer.Queue
        {
            get { return Strategy.Queue; }
        }

        bool IMediaPlayer.IsMuted
        {
            get { return Strategy.PlatformGetIsMuted(); }
            set { Strategy.PlatformSetIsMuted(value); }
        }

        bool IMediaPlayer.IsRepeating
        {
            get { return Strategy.PlatformGetIsRepeating(); }
            set { Strategy.PlatformSetIsRepeating(value); }
        }

        bool IMediaPlayer.IsShuffled
        {
            get { return Strategy.PlatformGetIsShuffled(); }
            set { Strategy.PlatformSetIsShuffled(value); }
        }

        bool IMediaPlayer.IsVisualizationEnabled
        {
            get { return Strategy.PlatformGetIsVisualizationEnabled(); }
            set { Strategy.PlatformSetIsVisualizationEnabled(value); }
        }

        TimeSpan IMediaPlayer.PlayPosition
        {
            get { return Strategy.PlatformGetPlayPosition(); }
        }

        MediaState IMediaPlayer.State
        {
            get { return Strategy.State; }
        }

        bool IMediaPlayer.GameHasControl
        {
            get { return Strategy.PlatformGetGameHasControl(); }
        }

        float IMediaPlayer.Volume
        {
            get { return Strategy.PlatformGetVolume(); }
            set
            {
                var volume = MathHelper.Clamp(value, 0, 1);
                Strategy.PlatformSetVolume(volume);
            }
        }


        private MediaPlayer()
        {
            _strategy = MediaFactory.Current.CreateMediaPlayerStrategy();

            _strategy.PlatformActiveSongChanged += Strategy_PlatformActiveSongChanged;
            _strategy.PlatformMediaStateChanged += Strategy_PlatformMediaStateChanged;
        }
        

        private void Strategy_PlatformActiveSongChanged(object sender, EventArgs e)
        {
            OnActiveSongChanged(e);
        }

        private void Strategy_PlatformMediaStateChanged(object sender, EventArgs e)
        {
            OnMediaStateChanged(e);
        }

        void IMediaPlayer.Play(Song song)
        {
            Strategy.Play(song);
        }

        void IMediaPlayer.Play(SongCollection collection)
        {
            ((IMediaPlayer)this).Play(collection, 0);
        }

        void IMediaPlayer.Play(SongCollection collection, int index)
        {
            Strategy.Play(collection, index);
        }

        void IMediaPlayer.Pause()
        {
            Strategy.Pause();
        }

        void IMediaPlayer.Resume()
        {
            Strategy.Resume();
        }

        void IMediaPlayer.Stop()
        {
            Strategy.Stop();
        }

        void IMediaPlayer.MoveNext()
        {
            Strategy.MoveNext();
        }

        void IMediaPlayer.MovePrevious()
        {
            Strategy.MovePrevious();
        }
                
    }

    internal interface IMediaPlayer
    {
        MediaQueue Queue { get; }
        bool IsMuted { get; set; }
        bool IsRepeating { get; set; }
        bool IsShuffled { get; set; }
        bool IsVisualizationEnabled { get; set; }
        TimeSpan PlayPosition { get; }
        MediaState State { get; }
        bool GameHasControl { get; }
        float Volume { get; set; }

        event EventHandler<EventArgs> ActiveSongChanged;
        event EventHandler<EventArgs> MediaStateChanged;

        void Play(Song song);
        void Play(SongCollection collection);
        void Play(SongCollection collection, int index);
        void Pause();
        void Resume();
        void Stop();
        void MoveNext();
        void MovePrevious();
    }
}
