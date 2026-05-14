using UnityEngine;

[DisallowMultipleComponent]
public class PlayerState : MonoBehaviour
{
    public static PlayerState Instance { get; private set; }

    public float currentHealthy = 100f;
    public float currentCarlories = 100f;
    public float currentHydrationPercent = 100f;
    public GameObject playerBody;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (playerBody == null)
        {
            playerBody = gameObject;
        }
    }

    private void Reset()
    {
        playerBody = gameObject;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
