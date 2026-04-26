using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dieu khien che do "Clue Vision": nhan phim de bat/tat hieu ung tim manh moi.
/// Tim moi object co tag "Clue" va bat/tat trang thai highlight cho chung.
/// </summary>
public class EchoVision : MonoBehaviour
{
    [Header("Cai dat phim & thoi gian")]
    [Tooltip("Phim bat/tat Clue Vision")]
    public KeyCode activateKey = KeyCode.Q;
    [Tooltip("Tu tat sau bao nhieu giay (0 = khong gioi han)")]
    public float visionDuration = 5f;

    private readonly List<EchoObject> clues = new List<EchoObject>();
    private CameraEffect cameraEffect;
    private bool visionActive;
    private float timer;

    void Awake()
    {
        CacheCameraEffect();
        RefreshClues();
        ApplyVisionState(false, true);
    }

    void Update()
    {
        if (Input.GetKeyDown(activateKey))
        {
            if (!visionActive)
            {
                RefreshClues();
            }

            ApplyVisionState(!visionActive);
        }

        if (!visionActive)
        {
            return;
        }

        timer += Time.deltaTime;
        if (visionDuration > 0f && timer >= visionDuration)
        {
            ApplyVisionState(false);
        }
    }

    void CacheCameraEffect()
    {
        if (cameraEffect == null && Camera.main != null)
        {
            cameraEffect = Camera.main.GetComponent<CameraEffect>();
        }
    }

    void RefreshClues()
    {
        clues.Clear();

        GameObject[] clueObjects = GameObject.FindGameObjectsWithTag("Clue");
        foreach (GameObject clueObject in clueObjects)
        {
            if (clueObject == null)
            {
                continue;
            }

            EchoObject echoObject = clueObject.GetComponent<EchoObject>();
            if (echoObject != null)
            {
                clues.Add(echoObject);
            }
        }
    }

    void ApplyVisionState(bool state, bool force = false)
    {
        if (!force && visionActive == state)
        {
            return;
        }

        visionActive = state;
        timer = 0f;

        CacheCameraEffect();
        cameraEffect?.SetEchoMode(state);

        for (int i = clues.Count - 1; i >= 0; i--)
        {
            EchoObject clue = clues[i];
            if (clue == null)
            {
                clues.RemoveAt(i);
                continue;
            }

            clue.isActive = state;
        }
    }
}
