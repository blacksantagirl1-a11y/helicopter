using System.Linq;
using UnityEngine;

public class FirstPersonAudio : MonoBehaviour
{
    [Tooltip("Script điều khiển di chuyển nhân vật")]
    public PlayerMovement character;
    [Tooltip("Component kiểm tra chạm đất")]
    public GroundCheck groundCheck;

    [Header("Step")]
    [Tooltip("AudioSource phát tiếng bước chân đi bộ")]
    public AudioSource stepAudio;
    [Tooltip("AudioSource phát tiếng bước chân khi chạy")]
    public AudioSource runningAudio;
    [Tooltip("Minimum velocity for moving audio to play")]
    /// <summary> "Minimum velocity for moving audio to play" </summary>
    public float velocityThreshold = .01f;
    Vector2 lastCharacterPosition;
    Vector2 CurrentCharacterPosition => new Vector2(character.transform.position.x, character.transform.position.z);

    [Header("Landing")]
    [Tooltip("AudioSource phát âm thanh tiếp đất")]
    public AudioSource landingAudio;
    [Tooltip("Danh sách clip âm thanh tiếp đất")]
    public AudioClip[] landingSFX;

    [Header("Jump")]
    [Tooltip("Component Jump để lắng nghe sự kiện nhảy")]
    public Jump jump;
    [Tooltip("AudioSource phát âm thanh nhảy")]
    public AudioSource jumpAudio;
    [Tooltip("Danh sách clip âm thanh nhảy")]
    public AudioClip[] jumpSFX;

    [Header("Crouch")]
    [Tooltip("Component Crouch để lắng nghe trạng thái cúi")]
    public Crouch crouch;
    [Tooltip("AudioSource phát âm thanh bắt đầu cúi, khi đang cúi và khi đứng dậy")]
    public AudioSource crouchStartAudio, crouchedAudio, crouchEndAudio;
    [Tooltip("Danh sách clip âm thanh bắt đầu cúi và kết thúc cúi")]
    public AudioClip[] crouchStartSFX, crouchEndSFX;

    const string MovementLoopKey = "PlayerMovement";

    AudioSource[] MovingAudios => new AudioSource[] { stepAudio, runningAudio, crouchedAudio };


    void Reset()
    {
        // Setup stuff.
        character = GetComponentInParent<PlayerMovement>();
        groundCheck = (transform.parent ?? transform).GetComponentInChildren<GroundCheck>();
        stepAudio = GetOrCreateAudioSource("Step Audio");
        runningAudio = GetOrCreateAudioSource("Running Audio");
        landingAudio = GetOrCreateAudioSource("Landing Audio");

        // Setup jump audio.
        jump = GetComponentInParent<Jump>();
        if (jump)
        {
            jumpAudio = GetOrCreateAudioSource("Jump audio");
        }

        // Setup crouch audio.
        crouch = GetComponentInParent<Crouch>();
        if (crouch)
        {
            crouchStartAudio = GetOrCreateAudioSource("Crouch Start Audio");
            crouchStartAudio = GetOrCreateAudioSource("Crouched Audio");
            crouchStartAudio = GetOrCreateAudioSource("Crouch End Audio");
        }
    }

    void OnEnable() => SubscribeToEvents();

    void OnDisable() => UnsubscribeToEvents();

    void FixedUpdate()
    {
        if (CanPlayMovementAudio())
        {
            if (crouch && crouch.IsCrouched)
            {
                StopSharedMovementAudio();
                SetPlayingMovingAudio(crouchedAudio);
            }
            else if (character.IsRunning)
            {
                SetPlayingMovingAudio(null);
                PlaySharedMovementAudio(SoundIds.Running);
            }
            else
            {
                SetPlayingMovingAudio(null);
                PlaySharedMovementAudio(SoundIds.Walking);
            }
        }
        else
        {
            SetPlayingMovingAudio(null);
            StopSharedMovementAudio();
        }

        // Remember lastCharacterPosition.
        lastCharacterPosition = CurrentCharacterPosition;
    }

    bool CanPlayMovementAudio()
    {
        if (character == null || !character.isActiveAndEnabled || character.IsCutscenePlaying)
        {
            return false;
        }

        if (groundCheck != null && !groundCheck.isGrounded)
        {
            return false;
        }

        return Input.GetKey(KeyCode.W) ||
               Input.GetKey(KeyCode.A) ||
               Input.GetKey(KeyCode.S) ||
               Input.GetKey(KeyCode.D);
    }

    /// <summary>
    /// Pause all MovingAudios and enforce play on audioToPlay.
    /// </summary>
    /// <param name="audioToPlay">Audio that should be playing.</param>
    void SetPlayingMovingAudio(AudioSource audioToPlay)
    {
        // Pause all MovingAudios.
        foreach (var audio in MovingAudios.Where(audio => audio != audioToPlay && audio != null))
        {
            audio.Pause();
        }

        if (!audioToPlay)
        {
            return;
        }

        audioToPlay.loop = true;

        // Play audioToPlay if it was not playing.
        if (!audioToPlay.isPlaying)
        {
            audioToPlay.Play();
        }
    }

    static void PlaySharedMovementAudio(string soundId)
    {
        ReSoundManager.Resolve()?.PlayLoop2D(soundId, MovementLoopKey);
    }

    static void StopSharedMovementAudio()
    {
        ReSoundManager.Resolve()?.StopLoop2D(MovementLoopKey);
    }

    #region Play instant-related audios.
    void PlayLandingAudio() => PlayRandomClip(landingAudio, landingSFX);
    void PlayJumpAudio() => PlayRandomClip(jumpAudio, jumpSFX);
    void PlayCrouchStartAudio() => PlayRandomClip(crouchStartAudio, crouchStartSFX);
    void PlayCrouchEndAudio() => PlayRandomClip(crouchEndAudio, crouchEndSFX);
    #endregion

    #region Subscribe/unsubscribe to events.
    void SubscribeToEvents()
    {
        // PlayLandingAudio when Grounded.
        groundCheck.Grounded += PlayLandingAudio;

        // PlayJumpAudio when Jumped.
        if (jump)
        {
            jump.Jumped += PlayJumpAudio;
        }

        // Play crouch audio on crouch start/end.
        if (crouch)
        {
            crouch.CrouchStart += PlayCrouchStartAudio;
            crouch.CrouchEnd += PlayCrouchEndAudio;
        }
    }

    void UnsubscribeToEvents()
    {
        // Undo PlayLandingAudio when Grounded.
        groundCheck.Grounded -= PlayLandingAudio;

        // Undo PlayJumpAudio when Jumped.
        if (jump)
        {
            jump.Jumped -= PlayJumpAudio;
        }

        // Undo play crouch audio on crouch start/end.
        if (crouch)
        {
            crouch.CrouchStart -= PlayCrouchStartAudio;
            crouch.CrouchEnd -= PlayCrouchEndAudio;
        }
    }
    #endregion

    #region Utility.
    /// <summary>
    /// Get an existing AudioSource from a name or create one if it was not found.
    /// </summary>
    /// <param name="name">Name of the AudioSource to search for.</param>
    /// <returns>The created AudioSource.</returns>
    AudioSource GetOrCreateAudioSource(string name)
    {
        // Try to get the audiosource.
        AudioSource result = System.Array.Find(GetComponentsInChildren<AudioSource>(), a => a.name == name);
        if (result)
            return result;

        // Audiosource does not exist, create it.
        result = new GameObject(name).AddComponent<AudioSource>();
        result.spatialBlend = 1;
        result.playOnAwake = false;
        result.transform.SetParent(transform, false);
        return result;
    }

    static void PlayRandomClip(AudioSource audio, AudioClip[] clips)
    {
        if (!audio || clips.Length <= 0)
            return;

        // Get a random clip. If possible, make sure that it's not the same as the clip that is already on the audiosource.
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clips.Length > 1)
            while (clip == audio.clip)
                clip = clips[Random.Range(0, clips.Length)];

        // Play the clip.
        audio.clip = clip;
        audio.Play();
    }
    #endregion 
}
