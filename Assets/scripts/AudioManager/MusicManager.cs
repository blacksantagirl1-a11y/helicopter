using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

	[SerializeField]
	private MusicLibrary musicLibrary;
	[SerializeField]
    private AudioSource musicSource;

	Coroutine activeMusicRoutine;

	private void Awake()
	{
		if (Instance != null)
		{
			Destroy(this);
		}
		else
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void PlayMusic(string trackName, float fadeDuration = 0.5f)
	{
		if (musicLibrary == null || musicSource == null)
		{
			return;
		}

		AudioClip nextTrack = musicLibrary.GetClipFromName(trackName);
		if (nextTrack == null)
		{
			return;
		}

		StopActiveMusicRoutine();
		activeMusicRoutine = StartCoroutine(AnimateMusicCrossfade(nextTrack, fadeDuration));
	}

	public void StopMusic(float fadeDuration = 0.25f)
	{
		if (musicSource == null)
		{
			return;
		}

		StopActiveMusicRoutine();
		activeMusicRoutine = StartCoroutine(AnimateMusicStop(fadeDuration));
	}

	void StopActiveMusicRoutine()
	{
		if (activeMusicRoutine == null)
		{
			return;
		}

		StopCoroutine(activeMusicRoutine);
		activeMusicRoutine = null;
	}

	IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float fadeDuration = 0.5f)
	{
		if (fadeDuration <= 0f)
		{
			musicSource.volume = 1f;
			musicSource.clip = nextTrack;
			musicSource.Play();
			activeMusicRoutine = null;
			yield break;
		}

		float percent = 0;
		float startVolume = musicSource.volume;
		while (percent < 1)
		{
			percent += Time.deltaTime * 1 / fadeDuration;
			musicSource.volume = Mathf.Lerp(startVolume, 0, percent);
			yield return null;
		}

		musicSource.clip = nextTrack;
		musicSource.Play();

		percent = 0;
		while (percent < 1)
		{
			percent += Time.deltaTime * 1 / fadeDuration;
			musicSource.volume = Mathf.Lerp(0, 1f, percent);
			yield return null;
		}

		activeMusicRoutine = null;
	}

	IEnumerator AnimateMusicStop(float fadeDuration = 0.25f)
	{
		if (fadeDuration <= 0f)
		{
			musicSource.Stop();
			musicSource.clip = null;
			musicSource.volume = 0f;
			activeMusicRoutine = null;
			yield break;
		}

		float percent = 0;
		float startVolume = musicSource.volume;
		while (percent < 1)
		{
			percent += Time.deltaTime * 1 / fadeDuration;
			musicSource.volume = Mathf.Lerp(startVolume, 0, percent);
			yield return null;
		}

		musicSource.Stop();
		musicSource.clip = null;
		activeMusicRoutine = null;
	}
}
