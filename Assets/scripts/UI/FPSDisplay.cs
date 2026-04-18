using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    const string DefaultLabel = "-- FPS";
    static readonly Vector2 TextOffset = new(-24f, -24f);
    static readonly Vector2 TextSize = new(180f, 40f);

    [Tooltip("TextMeshPro dùng để hiển thị FPS hiện tại")]
    [SerializeField] public TextMeshProUGUI FpsText;
    [Tooltip("Khoảng thời gian lấy mẫu để cập nhật FPS (giây)")]
    [SerializeField, Min(0.1f)] float pollingTime = 0.5f;

    static FPSDisplay instance;

    float elapsedTime;
    int frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        UpdateText(0);
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        EnsureTextExists();

        elapsedTime += Time.unscaledDeltaTime;
        frameCount++;

        if (elapsedTime < pollingTime)
        {
            return;
        }

        int frameRate = Mathf.RoundToInt(frameCount / elapsedTime);
        UpdateText(frameRate);

        elapsedTime = 0f;
        frameCount = 0;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    void EnsureTextExists()
    {
    }

    void ConfigureText()
    {
    }

    void UpdateText(int frameRate)
    {
        if (FpsText == null)
        {
            return;
        }

        if (frameRate > 0)
        {
            FpsText.SetText("{0} FPS", frameRate);
            return;
        }

        FpsText.text = DefaultLabel;
    }


}
