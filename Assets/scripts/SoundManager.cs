using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; set; }

    public AudioSource fishingSource;

    public AudioSource swingAxeSource;

    public AudioSource killBoarSource;

    public AudioSource chopTreeSource;

    public AudioSource treeFallSource;

    public AudioSource pickUpSource;

    public AudioSource cookingSource;

    private void Awake()
    {
        
    }
    public void PlaySound(AudioSource soundToPlay)
    {
        if (!soundToPlay.isPlaying)
        {
            soundToPlay.Play();
        }
    }
}
