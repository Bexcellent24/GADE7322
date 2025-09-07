using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Sound Library")]
    [SerializeField] private List<SoundData> sounds;

    private Dictionary<string, SoundData> soundLookup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        soundLookup = new Dictionary<string, SoundData>();
        foreach (var sound in sounds)
            soundLookup[sound.soundName] = sound;

        LoadVolumes();
    }

    // --- MUSIC ---
    
    public void StopMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = null; // optional: clear the clip
        }
    }
    public void PlayMusic(string name)
    {
        if (!soundLookup.TryGetValue(name, out var sound)) return;

        musicSource.clip = sound.clip;
        musicSource.volume = sound.volume;
        musicSource.pitch = sound.pitch;
        musicSource.loop = true;
        musicSource.Play();
    }

    // --- SFX ---
    public void PlaySFX(string name)
    {
        if (!soundLookup.TryGetValue(name, out var sound)) return;

        sfxSource.PlayOneShot(sound.clip, sound.volume);
    }

    // --- Volume Control ---
    public void SetMusicVolume(float value)
    {
        mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp01(value)) * 20);
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        mixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp01(value)) * 20);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    private void LoadVolumes()
    {
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
        SetMusicVolume(music);
        SetSFXVolume(sfx);
    }
}
