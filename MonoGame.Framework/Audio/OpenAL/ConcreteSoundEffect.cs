// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2021 Nick Kastellanos
﻿
using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Platform.Audio.OpenAL;

#if IOS
using AudioToolbox;
using AudioUnit;
#endif

namespace Microsoft.Xna.Platform.Audio
{
    class ConcreteSoundEffect : SoundEffectStrategy
    {
        private ALSoundBuffer _soundBuffer;

        #region Initialization

        internal override void PlatformLoadAudioStream(Stream stream, out TimeSpan duration)
        {
            byte[] buffer;

            ALFormat format;
            int freq;
            int channels;
            int blockAlignment;
            int bitsPerSample;
            int samplesPerBlock;
            int sampleCount;
            buffer = AudioLoader.Load(stream, out format, out freq, out channels, out blockAlignment, out bitsPerSample, out samplesPerBlock, out sampleCount);

            duration = TimeSpan.FromSeconds((float)sampleCount / (float)freq);

            PlatformInitializeBuffer(buffer, 0, buffer.Length, format, channels, freq, blockAlignment, bitsPerSample, 0, 0);
        }

        internal override void PlatformInitializePcm(byte[] buffer, int index, int count, int sampleBits, int sampleRate, int channels, int loopStart, int loopLength)
        {
            if (sampleBits == 24)
            {
                // Convert 24-bit signed PCM to 16-bit signed PCM
                buffer = AudioLoader.Convert24To16(buffer, index, count);
                index = 0;
                count = buffer.Length;
                sampleBits = 16;
            }

            var format = AudioLoader.GetSoundFormat(AudioLoader.FormatPcm, channels, sampleBits);

            // bind buffer
            _soundBuffer = new ALSoundBuffer(AudioService.Current);
            _soundBuffer.BindDataBuffer(buffer, index, count, format, sampleRate);
        }

        private void InitializeIeeeFloat(byte[] buffer, int offset, int count, int sampleRate, int channels, int loopStart, int loopLength)
        {
            ConcreteAudioService ConcreteAudioService = (ConcreteAudioService)AudioService.Current._strategy;

            if (!ConcreteAudioService.SupportsIeee)
            {
                // If 32-bit IEEE float is not supported, convert to 16-bit signed PCM
                buffer = AudioLoader.ConvertFloatTo16(buffer, offset, count);
                PlatformInitializePcm(buffer, 0, buffer.Length, 16, sampleRate, channels, loopStart, loopLength);
                return;
            }

            var format = AudioLoader.GetSoundFormat(AudioLoader.FormatIeee, channels, 32);

            // bind buffer
            _soundBuffer = new ALSoundBuffer(AudioService.Current);
            _soundBuffer.BindDataBuffer(buffer, offset, count, format, sampleRate);
        }

        private void InitializeAdpcm(byte[] buffer, int index, int count, int sampleRate, int channels, int blockAlignment, int loopStart, int loopLength)
        {
            ConcreteAudioService ConcreteAudioService = (ConcreteAudioService)AudioService.Current._strategy;

            if (!ConcreteAudioService.SupportsAdpcm)
            {
                // If MS-ADPCM is not supported, convert to 16-bit signed PCM
                buffer = AudioLoader.ConvertMsAdpcmToPcm(buffer, index, count, channels, blockAlignment);
                PlatformInitializePcm(buffer, 0, buffer.Length, 16, sampleRate, channels, loopStart, loopLength);
                return;
            }

            var format = AudioLoader.GetSoundFormat(AudioLoader.FormatMsAdpcm, channels, 0);
            int sampleAlignment = AudioLoader.SampleAlignment(format, blockAlignment);

            // Buffer length must be aligned with the block alignment
            int alignedCount = count - (count % blockAlignment);

            // bind buffer
            _soundBuffer = new ALSoundBuffer(AudioService.Current);
            _soundBuffer.BindDataBuffer(buffer, index, alignedCount, format, sampleRate, sampleAlignment);
        }

        private void InitializeIma4(byte[] buffer, int index, int count, int sampleRate, int channels, int blockAlignment, int loopStart, int loopLength)
        {
            ConcreteAudioService ConcreteAudioService = (ConcreteAudioService)AudioService.Current._strategy;

            if (!ConcreteAudioService.SupportsIma4)
            {
                // If IMA/ADPCM is not supported, convert to 16-bit signed PCM
                buffer = AudioLoader.ConvertIma4ToPcm(buffer, index, count, channels, blockAlignment);
                PlatformInitializePcm(buffer, 0, buffer.Length, 16, sampleRate, channels, loopStart, loopLength);
                return;
            }

            var format = AudioLoader.GetSoundFormat(AudioLoader.FormatIma4, (int)channels, 0);
            int sampleAlignment = AudioLoader.SampleAlignment(format, blockAlignment);

            // bind buffer
            _soundBuffer = new ALSoundBuffer(AudioService.Current);
            _soundBuffer.BindDataBuffer(buffer, index, count, format, sampleRate, sampleAlignment);
        }

        internal override void PlatformInitializeFormat(byte[] header, byte[] buffer, int index, int count, int loopStart, int loopLength)
        {
            var wavFormat = BitConverter.ToInt16(header, 0);
            var channels = BitConverter.ToInt16(header, 2);
            var sampleRate = BitConverter.ToInt32(header, 4);
            var blockAlignment = BitConverter.ToInt16(header, 12);
            var bitsPerSample = BitConverter.ToInt16(header, 14);

            var format = AudioLoader.GetSoundFormat(wavFormat, channels, bitsPerSample);
            PlatformInitializeBuffer(buffer, index, count, format, channels, sampleRate, blockAlignment, bitsPerSample, loopStart, loopLength);
        }

        private void PlatformInitializeBuffer(byte[] buffer, int bufferOffset, int bufferSize, ALFormat format, int channels, int sampleRate, int blockAlignment, int bitsPerSample, int loopStart, int loopLength)
        {
            switch (format)
            {
                case ALFormat.Mono8:
                case ALFormat.Mono16:
                case ALFormat.Stereo8:
                case ALFormat.Stereo16:
                    PlatformInitializePcm(buffer, bufferOffset, bufferSize, bitsPerSample, sampleRate, channels, loopStart, loopLength);
                    break;
                case ALFormat.MonoMSAdpcm:
                case ALFormat.StereoMSAdpcm:
                    InitializeAdpcm(buffer, bufferOffset, bufferSize, sampleRate, channels, blockAlignment, loopStart, loopLength);
                    break;
                case ALFormat.MonoFloat32:
                case ALFormat.StereoFloat32:
                    InitializeIeeeFloat(buffer, bufferOffset, bufferSize, sampleRate, channels, loopStart, loopLength);
                    break;
                case ALFormat.MonoIma4:
                case ALFormat.StereoIma4:
                    InitializeIma4(buffer, bufferOffset, bufferSize, sampleRate, channels, blockAlignment, loopStart, loopLength);
                    break;
                default:
                    throw new NotSupportedException("Unsupported wave format!");
            }
        }

        internal override void PlatformInitializeXactAdpcm(byte[] buffer, int index, int count, int channels, int sampleRate, int blockAlignment, int loopStart, int loopLength)
        {
            InitializeAdpcm(buffer, index, count, sampleRate, channels, (blockAlignment + 22) * channels, loopStart, loopLength);
        }

        #endregion

        internal ALSoundBuffer GetALSoundBuffer()
        {
            return _soundBuffer;
        }


#region IDisposable Members

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _soundBuffer.Dispose();
                _soundBuffer = null;
            }

        }

#endregion

    }
}
