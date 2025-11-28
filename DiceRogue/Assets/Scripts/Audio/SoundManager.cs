using UnityEngine;

namespace DiceGame.Audio
{
    /// <summary>
    /// Singleton manager for playing sound effects throughout the game
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        private static SoundManager _instance;
        public static SoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SoundManager");
                    _instance = go.AddComponent<SoundManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private AudioSource _audioSource;      // For sound effects
        private AudioSource _musicSource;       // For background music
        
        // Audio clips loaded from Resources
        private AudioClip _diceRollClip;
        private AudioClip _lockClip;
        private AudioClip _unlockClip;
        private AudioClip _submitClip;
        private AudioClip _backgroundMusicClip;

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // Create AudioSource component for sound effects
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.volume = 1.0f;

            // Create separate AudioSource component for background music
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;  // Enable looping for background music
            _musicSource.volume = 0.7f; // Slightly lower volume for background music

            // Load audio clips from Resources/Audio folder
            _diceRollClip = Resources.Load<AudioClip>("Audio/dice_roll");
            _lockClip = Resources.Load<AudioClip>("Audio/lock");
            _unlockClip = Resources.Load<AudioClip>("Audio/unlock");
            _submitClip = Resources.Load<AudioClip>("Audio/submit");
            _backgroundMusicClip = Resources.Load<AudioClip>("Audio/background");

            // Log warnings if clips are missing
            if (_diceRollClip == null) Debug.LogWarning("[SoundManager] dice_roll.wav not found in Resources/Audio");
            if (_lockClip == null) Debug.LogWarning("[SoundManager] lock.wav not found in Resources/Audio");
            if (_unlockClip == null) Debug.LogWarning("[SoundManager] unlock.wav not found in Resources/Audio");
            if (_submitClip == null) Debug.LogWarning("[SoundManager] submit.wav not found in Resources/Audio");
            if (_backgroundMusicClip == null) Debug.LogWarning("[SoundManager] background.wav not found in Resources/Audio");

            // Start playing background music
            PlayBackgroundMusic();
        }

        /// <summary>
        /// Play dice roll sound effect
        /// </summary>
        public void PlayDiceRoll()
        {
            if (_diceRollClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_diceRollClip);
            }
        }

        /// <summary>
        /// Play lock sound effect
        /// </summary>
        public void PlayLock()
        {
            if (_lockClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_lockClip);
            }
        }

        /// <summary>
        /// Play unlock sound effect
        /// </summary>
        public void PlayUnlock()
        {
            if (_unlockClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_unlockClip);
            }
        }

        /// <summary>
        /// Play submit sound effect
        /// </summary>
        public void PlaySubmit()
        {
            if (_submitClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_submitClip);
            }
        }

        /// <summary>
        /// Start playing background music (loops automatically)
        /// </summary>
        public void PlayBackgroundMusic()
        {
            if (_backgroundMusicClip != null && _musicSource != null)
            {
                if (!_musicSource.isPlaying)
                {
                    _musicSource.clip = _backgroundMusicClip;
                    _musicSource.Play();
                    Debug.Log("[SoundManager] Background music started");
                }
            }
        }

        /// <summary>
        /// Stop background music
        /// </summary>
        public void StopBackgroundMusic()
        {
            if (_musicSource != null && _musicSource.isPlaying)
            {
                _musicSource.Stop();
            }
        }

        /// <summary>
        /// Set background music volume (0.0 to 1.0)
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            if (_musicSource != null)
            {
                _musicSource.volume = Mathf.Clamp01(volume);
            }
        }

        /// <summary>
        /// Set sound effects volume (0.0 to 1.0)
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            if (_audioSource != null)
            {
                _audioSource.volume = Mathf.Clamp01(volume);
            }
        }
    }
}

