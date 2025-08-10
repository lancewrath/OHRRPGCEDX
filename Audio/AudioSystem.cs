using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace OHRRPGCEDX.Audio
{
    /// <summary>
    /// Audio system for handling music and sound effects
    /// </summary>
    public class AudioSystem : IDisposable
    {
        private XAudio2 xaudio2;
        private MasteringVoice masteringVoice;
        private Dictionary<int, AudioBuffer> soundEffects;
        private Dictionary<int, AudioBuffer> musicTracks;
        private AudioBuffer currentMusic;
        private SourceVoice currentMusicVoice;
        private bool isInitialized;
        private float masterVolume;
        private float musicVolume;
        private float sfxVolume;

        public AudioSystem()
        {
            soundEffects = new Dictionary<int, AudioBuffer>();
            musicTracks = new Dictionary<int, AudioBuffer>();
            masterVolume = 1.0f;
            musicVolume = 0.8f;
            sfxVolume = 1.0f;
        }

        /// <summary>
        /// Initialize the audio system
        /// </summary>
        public bool Initialize()
        {
            try
            {
                xaudio2 = new XAudio2();
                masteringVoice = new MasteringVoice(xaudio2);
                isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize audio system: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load a sound effect from file
        /// </summary>
        public bool LoadSoundEffect(int id, string filePath)
        {
            if (!isInitialized) return false;

            try
            {
                if (!File.Exists(filePath)) return false;

                var audioData = LoadAudioFile(filePath);
                if (audioData != null)
                {
                    soundEffects[id] = audioData;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load sound effect {id}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Load a music track from file
        /// </summary>
        public bool LoadMusicTrack(int id, string filePath)
        {
            if (!isInitialized) return false;

            try
            {
                if (!File.Exists(filePath)) return false;

                var audioData = LoadAudioFile(filePath);
                if (audioData != null)
                {
                    musicTracks[id] = audioData;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load music track {id}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Play a sound effect
        /// </summary>
        public void PlaySoundEffect(int id)
        {
            if (!isInitialized || !soundEffects.ContainsKey(id)) return;

            try
            {
                var sourceVoice = new SourceVoice(xaudio2, soundEffects[id].Format);
                sourceVoice.SubmitSourceBuffer(soundEffects[id], null);
                sourceVoice.SetVolume(sfxVolume * masterVolume);
                sourceVoice.Start(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to play sound effect {id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Play music track
        /// </summary>
        public void PlayMusic(int id, bool loop = true)
        {
            if (!isInitialized || !musicTracks.ContainsKey(id)) return;

            try
            {
                // Stop current music if playing
                StopMusic();

                currentMusic = musicTracks[id];
                currentMusicVoice = new SourceVoice(xaudio2, currentMusic.Format);
                
                if (loop)
                {
                    currentMusic.LoopCount = XAudio2.LoopInfinite;
                }

                currentMusicVoice.SubmitSourceBuffer(currentMusic, null);
                currentMusicVoice.SetVolume(musicVolume * masterVolume);
                currentMusicVoice.Start(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to play music {id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop current music
        /// </summary>
        public void StopMusic()
        {
            if (currentMusicVoice != null)
            {
                currentMusicVoice.Stop();
                currentMusicVoice.Dispose();
                currentMusicVoice = null;
            }
        }

        /// <summary>
        /// Pause music
        /// </summary>
        public void PauseMusic()
        {
            if (currentMusicVoice != null)
            {
                currentMusicVoice.Stop();
            }
        }

        /// <summary>
        /// Resume music
        /// </summary>
        public void ResumeMusic()
        {
            if (currentMusicVoice != null)
            {
                currentMusicVoice.Start(0);
            }
        }

        /// <summary>
        /// Set master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Math.Max(0.0f, Math.Min(1.0f, volume));
            UpdateVolumes();
        }

        /// <summary>
        /// Set music volume
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Math.Max(0.0f, Math.Min(1.0f, volume));
            UpdateVolumes();
        }

        /// <summary>
        /// Set sound effects volume
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Math.Max(0.0f, Math.Min(1.0f, volume));
        }

        /// <summary>
        /// Update all voice volumes
        /// </summary>
        private void UpdateVolumes()
        {
            if (currentMusicVoice != null)
            {
                currentMusicVoice.SetVolume(musicVolume * masterVolume);
            }
        }

        /// <summary>
        /// Load audio file (placeholder - would need proper audio format support)
        /// </summary>
        private AudioBuffer LoadAudioFile(string filePath)
        {
            // This is a placeholder - in a real implementation, you'd need to:
            // 1. Detect audio format (WAV, OGG, MP3, etc.)
            // 2. Decode the audio data
            // 3. Convert to the format XAudio2 expects
            // 4. Create proper AudioBuffer with correct format info
            
            try
            {
                // For now, just create a dummy buffer
                // In reality, you'd parse the actual audio file
                var dummyData = new byte[1024]; // Placeholder
                var dummyFormat = new WaveFormat(44100, 16, 2); // CD quality stereo
                
                return new AudioBuffer(dummyData, dummyFormat);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check if audio system is initialized
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Get current master volume
        /// </summary>
        public float MasterVolume => masterVolume;

        /// <summary>
        /// Get current music volume
        /// </summary>
        public float MusicVolume => musicVolume;

        /// <summary>
        /// Get current SFX volume
        /// </summary>
        public float SFXVolume => sfxVolume;

        /// <summary>
        /// Check if music is currently playing
        /// </summary>
        public bool IsMusicPlaying => currentMusicVoice != null && currentMusicVoice.State.BuffersQueued > 0;

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            StopMusic();
            
            if (masteringVoice != null)
            {
                masteringVoice.Dispose();
                masteringVoice = null;
            }
            
            if (xaudio2 != null)
            {
                xaudio2.Dispose();
                xaudio2 = null;
            }

            foreach (var buffer in soundEffects.Values)
            {
                buffer.Dispose();
            }
            soundEffects.Clear();

            foreach (var buffer in musicTracks.Values)
            {
                buffer.Dispose();
            }
            musicTracks.Clear();

            isInitialized = false;
        }
    }

    /// <summary>
    /// Audio buffer wrapper for XAudio2
    /// </summary>
    public class AudioBuffer : IDisposable
    {
        public byte[] Data { get; private set; }
        public WaveFormat Format { get; private set; }
        public int LoopCount { get; set; }

        public AudioBuffer(byte[] data, WaveFormat format)
        {
            Data = data;
            Format = format;
            LoopCount = 0;
        }

        public void Dispose()
        {
            Data = null;
            Format = null;
        }
    }
}
