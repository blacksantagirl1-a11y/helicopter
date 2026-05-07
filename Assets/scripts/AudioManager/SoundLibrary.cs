using UnityEngine;

[System.Serializable]
public struct SoundEffect
{
    public string groupID;
    public AudioClip[] clips;
}

public class SoundLibrary : MonoBehaviour
{
    public SoundEffect[] soundEffects;

	public AudioClip GetClipFromName(string name)
	{
        if (string.IsNullOrWhiteSpace(name) || soundEffects == null)
        {
            return null;
        }

        foreach (var soundEffect in soundEffects)
        {
			if (soundEffect.groupID == name)
            {
                return GetRandomClip(soundEffect.clips);
            }
        }
		return null;
	}

    static AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        int startIndex = Random.Range(0, clips.Length);
        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[(startIndex + i) % clips.Length];
            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }
}
