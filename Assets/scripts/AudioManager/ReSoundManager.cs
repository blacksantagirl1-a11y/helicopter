using System.Collections.Generic;
using UnityEngine;

public static class SoundIds
{
    public const string Fishing = "Fishing";
    public const string SwingAxe = "SwingAxe";
    public const string Boar = "Boar";
    public const string KillBoar = "KillBoar";
    public const string ChopTree = "ChopTree";
    public const string TreeFall = "TreeFall";
    public const string PickUp = "PickUp";
    public const string Cooking = "Cooking";
    public const string Walking = "Walking";
    public const string Running = "Running";
    public const string Win = "Win";
    public const string Lose = "Lose";
    public const string Eat = "Eat";
    public const string OpenDoor = "OpenDoor";
    public const string GunShot = "GunShot";
}

public class ReSoundManager : MonoBehaviour
{
    public static ReSoundManager Instance;

    [SerializeField]
    private SoundLibrary sfxLibrary;
    [SerializeField]
    private AudioSource SoundSource;

    readonly Dictionary<string, AudioSource> loopSources = new();
    readonly Dictionary<string, string> activeLoopSoundNames = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static ReSoundManager Resolve()
    {
        return Instance != null
            ? Instance
            : FindFirstObjectByType<ReSoundManager>();
    }

    public void PlaySound2D(string soundName)
    {
        AudioClip clip = ResolveClip(soundName);
        AudioSource source = ResolveSoundSource();
        if (clip == null || source == null)
        {
            return;
        }

        source.loop = false;
        source.spatialBlend = 0f;
        source.PlayOneShot(clip);
    }

    public void PlaySound3D(AudioClip clip, Vector3 pos)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, pos);
        }
    }

    public void PlaySound3D(string soundName, Vector3 pos)
    {
        PlaySound3D(ResolveClip(soundName), pos);
    }

    public void PlayLoop2D(string soundName, string loopKey)
    {
        if (string.IsNullOrWhiteSpace(loopKey))
        {
            loopKey = soundName;
        }

        AudioClip clip = ResolveClip(soundName);
        AudioSource source = ResolveLoopSource(loopKey);
        if (clip == null || source == null)
        {
            return;
        }

        bool isSameLoop = activeLoopSoundNames.TryGetValue(loopKey, out string activeSoundName)
            && activeSoundName == soundName
            && source.clip != null;

        if (!isSameLoop)
        {
            source.Stop();
            source.clip = clip;
            source.loop = true;
            source.spatialBlend = 0f;
            activeLoopSoundNames[loopKey] = soundName;
        }

        if (!source.isPlaying)
        {
            source.Play();
        }
    }

    public void StopLoop2D(string loopKey)
    {
        if (string.IsNullOrWhiteSpace(loopKey))
        {
            return;
        }

        if (!loopSources.TryGetValue(loopKey, out AudioSource source) || source == null)
        {
            activeLoopSoundNames.Remove(loopKey);
            return;
        }

        source.Stop();
        source.clip = null;
        activeLoopSoundNames.Remove(loopKey);
    }

    AudioClip ResolveClip(string soundName)
    {
        return sfxLibrary != null ? sfxLibrary.GetClipFromName(soundName) : null;
    }

    AudioSource ResolveSoundSource()
    {
        if (SoundSource != null)
        {
            return SoundSource;
        }

        SoundSource = GetComponentInChildren<AudioSource>();
        if (SoundSource != null)
        {
            return SoundSource;
        }

        SoundSource = gameObject.AddComponent<AudioSource>();
        SoundSource.playOnAwake = false;
        SoundSource.spatialBlend = 0f;
        return SoundSource;
    }

    AudioSource ResolveLoopSource(string loopKey)
    {
        if (loopSources.TryGetValue(loopKey, out AudioSource source) && source != null)
        {
            return source;
        }

        GameObject loopObject = new($"SoundLoop_{loopKey}");
        loopObject.transform.SetParent(transform, false);
        source = loopObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        loopSources[loopKey] = source;
        return source;
    }
}
