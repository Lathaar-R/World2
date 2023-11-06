using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages audio sources and plays sound effects and music.
/// </summary>
public class AudioManeger : MonoBehaviour 
{
    public static AudioManeger Instace;
    private int audioSourcesIndex = 0;
    private Coroutine playlistCoroutine;

    
    private AudioSource[] soundsEffectsAudioSources;
    [Tooltip("Audio source for music, it will be used to play music")]
    [SerializeField] private AudioSource musicAudioSource;
    [Tooltip("Number of audio sources for sound effects, the more audio sources the more sound effects can be played at the same time")]
    [SerializeField] private int audioSourcesNumber = 10;
    [Tooltip("Audio source prefab for sound effects, its properties will be copied to the audio sources for sound effects, it will not be used")]
    [SerializeField] private AudioSource audioSourcePrefab;
    [Tooltip("Sound effects audio clips, they will be used to play sound effects")]
    [SerializeField] private AudioClip[] soundEffectsAudioClips;
    [Tooltip("Music audio clips, they will be used to play music, they will play in order when PlayPlaylist is called")]
    [SerializeField] private AudioClip[] musicAudioClips;
    
    
    private void Awake() {

        if(Instace == null)
        {
            Instace = this;

            soundsEffectsAudioSources = new AudioSource[audioSourcesNumber];

            for(int i = 0; i < audioSourcesNumber; i++)
            {
                soundsEffectsAudioSources[i] = gameObject.AddComponent<AudioSource>();

                #region Audio Source Prefab Copy Properties
                soundsEffectsAudioSources[i].outputAudioMixerGroup = audioSourcePrefab.outputAudioMixerGroup;
                soundsEffectsAudioSources[i].playOnAwake = audioSourcePrefab.playOnAwake;
                soundsEffectsAudioSources[i].loop = audioSourcePrefab.loop;
                soundsEffectsAudioSources[i].priority = audioSourcePrefab.priority;
                soundsEffectsAudioSources[i].volume = audioSourcePrefab.volume;
                soundsEffectsAudioSources[i].pitch = audioSourcePrefab.pitch;
                soundsEffectsAudioSources[i].panStereo = audioSourcePrefab.panStereo;
                soundsEffectsAudioSources[i].spatialBlend = audioSourcePrefab.spatialBlend;
                soundsEffectsAudioSources[i].reverbZoneMix = audioSourcePrefab.reverbZoneMix;
                soundsEffectsAudioSources[i].dopplerLevel = audioSourcePrefab.dopplerLevel;
                soundsEffectsAudioSources[i].spread = audioSourcePrefab.spread;
                soundsEffectsAudioSources[i].rolloffMode = audioSourcePrefab.rolloffMode;
                soundsEffectsAudioSources[i].minDistance = audioSourcePrefab.minDistance;
                soundsEffectsAudioSources[i].maxDistance = audioSourcePrefab.maxDistance;
                #endregion
            }

            //lead all the music
            foreach (var M in musicAudioClips)
            {
                M.LoadAudioData();   
            }

            //lead all the sound effects
            foreach (var S in soundEffectsAudioClips)
            {
                S.LoadAudioData();
            }

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySound(String name) 
    {
        AudioClip audioClip = Array.Find(soundEffectsAudioClips, clip => clip.name == name);

        if(audioClip != null)
        {
            soundsEffectsAudioSources[audioSourcesIndex].clip = audioClip;
            soundsEffectsAudioSources[audioSourcesIndex].Play();

            audioSourcesIndex = (audioSourcesIndex + 1) % audioSourcesNumber;
        }
        else
        {
            Debug.LogError("Audio clip not found");
        }
    }

    public void PlayMusic(String name, float fadeTime = 1f, bool fromPlaylist = false) 
    {
        if(!fromPlaylist && playlistCoroutine != null)
        {
            StopPlaylist();
        }

        AudioClip musicAudio = Array.Find(musicAudioClips, clip => clip.name == name);

        if(musicAudio != null)
        {
            if(!musicAudioSource.isPlaying)
            {    
                musicAudioSource.clip = musicAudio;
                musicAudioSource.Play();
            }
            else
            {
                StartCoroutine(ChangeMusic(name, fadeTime, fromPlaylist));
            }
        }
        else
        {
            Debug.LogError("Music not found");
        }
    }   

    private IEnumerator ChangeMusic(String name, float fadeTime, bool fromPlaylist = false)
    {
        float maxVolume = musicAudioSource.volume;
        float time = 0f;

        while(time < fadeTime)
        {
            float t = time / fadeTime;
            musicAudioSource.volume = Mathf.Lerp(maxVolume, 0f, t * t);
            time += Time.deltaTime;
            yield return null;
        }

        musicAudioSource.Stop();

        yield return new WaitForSeconds(2f);

        PlayMusic(name, fadeTime, fromPlaylist);

        time = 0f;

        while(time < fadeTime)
        {
            float t = time / fadeTime;
            musicAudioSource.volume = Mathf.Lerp(0f, maxVolume, t * t);
            time += Time.deltaTime;
            yield return null;
        }

        musicAudioSource.volume = maxVolume;
    }

    public void PlayPlaylist()
    {
        StopPlaylist();
        playlistCoroutine = StartCoroutine(PlayPlaylistCoroutine());
    }

    public void StopPlaylist()
    {
        if(playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
        }
    }

    private IEnumerator PlayPlaylistCoroutine()
    {
        int index = 0;

        while(true)
        {
            PlayMusic(musicAudioClips[index].name, 2f, true);
            yield return new WaitForSeconds(musicAudioClips[index].length - 55f);
            index = (index + 1) % musicAudioClips.Length;
        }
    }

}
