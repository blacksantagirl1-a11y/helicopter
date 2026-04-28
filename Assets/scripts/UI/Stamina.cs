using UnityEngine;
using UnityEngine.UI;

// Stamina lo viec giam the luc theo di chuyen va hanh dong.
// Lien quan den chat cay:
// - nghe event AttackPerformed
// - neu camera dang huong vao cay co the chat
// - tru stamina theo chopStaminaCost
public class Stamina : MonoBehaviour
{
    [Tooltip("Slider hiển thị và lưu giá trị stamina hiện tại")]
    public Slider staminaSlider;
    [Tooltip("Tham chiếu script di chuyển của player")]
    public PlayerMovement playerMovement;
    [Tooltip("Rigidbody của player để kiểm tra vận tốc")]
    public Rigidbody playerRigidbody;
    [Tooltip("Script hành động (đánh/chặt) của player")]
    public ActionScript actionScript;
    [Tooltip("Camera dùng để raycast kiểm tra mục tiêu")]
    public Camera sourceCamera;
    [Tooltip("Terrain hiện tại để kiểm tra cây có thể chặt")]
    public Terrain targetTerrain;
    [Tooltip("Mức stamina tối đa")]
    public float maxStamina = 100f;
    [Tooltip("Tốc độ giảm stamina khi đang di chuyển")]
    public float staminaFallRate = 15f;
    [Tooltip("Tốc độ giảm stamina khi đứng yên")]
    public float idleStaminaFallRate = 10f;
    [Tooltip("Ngưỡng vận tốc để coi là đang di chuyển")]
    public float movementThreshold = 0.05f;

    [Header("Tree Chop Stamina")]
    [Tooltip("Lượng stamina tiêu hao cho mỗi lần chặt cây")]
    public float chopStaminaCost = 8f;
    [Tooltip("Tầm tối đa để chặt cây")]
    public float chopRange = 7f;
    [Tooltip("Độ cao điểm ngắm trên cây khi kiểm tra chặt")]
    public float chopTargetHeight = 2f;
    [Tooltip("Góc lệch tối đa giữa camera và mục tiêu cây")]
    public float chopMaxTargetAngle = 12f;
    [Tooltip("Từ khóa tên prototype cây được phép chặt")]
    public string[] cuttablePrototypeKeywords = { "pine", "tree" };

    [Header("Boar Kill Stamina")]
    [Tooltip("Lượng stamina tiêu hao khi hạ một con heo")]
    public float boarKillStaminaCost = 12f;

    [Header("Fishing Catch Stamina")]
    [Tooltip("Luong stamina tieu hao khi cau ca thanh cong")]
    public float fishingCatchStaminaCost = 12f;

    private ActionScript subscribedActionScript;

    private void Awake()
    {
        TryAssignReferences();
        RefreshAttackSubscription();
    }

    private void Reset()
    {
        TryAssignReferences();
        ClampValues();
    }

    private void OnEnable()
    {
        TryAssignReferences();
        RefreshAttackSubscription();
    }

    private void OnDisable()
    {
        if (subscribedActionScript != null)
        {
            subscribedActionScript.AttackPerformed -= HandleAttackPerformed;
            subscribedActionScript = null;
        }

    }

    private void OnValidate()
    {
        TryAssignReferences();
        ClampValues();

        if (Application.isPlaying)
        {
            RefreshAttackSubscription();
        }
    }

    void Start()
    {
        if (staminaSlider == null)
        {
            return;
        }

        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = maxStamina;
    }

    void Update()
    {
        if (staminaSlider == null)
        {
            return;
        }

        bool isMoving = IsPlayerMoving();

        if (isMoving)
        {
            staminaSlider.value -= staminaFallRate * Time.deltaTime;
        }
        else
        {
            staminaSlider.value -= idleStaminaFallRate * Time.deltaTime;
        }

        staminaSlider.value = Mathf.Clamp(staminaSlider.value, 0, maxStamina);
    }

    // Moi cu danh co the tru stamina cho viec chat cay; giet heo tru truc tiep tu Boar.Die().
    private void HandleAttackPerformed()
    {
        if (staminaSlider == null)
        {
            return;
        }

        if (chopStaminaCost > 0f && IsLookingAtCuttableTree())
        {
            staminaSlider.value = Mathf.Clamp(staminaSlider.value - chopStaminaCost, 0f, maxStamina);
        }
    }

    public void ConsumeBoarKillStamina()
    {
        if (boarKillStaminaCost <= 0f)
        {
            return;
        }

        TryAssignReferences();
        if (staminaSlider == null)
        {
            return;
        }

        staminaSlider.value = Mathf.Clamp(staminaSlider.value - boarKillStaminaCost, 0f, maxStamina);
    }

    public void ConsumeFishingCatchStamina()
    {
        if (staminaSlider == null || fishingCatchStaminaCost <= 0f)
        {
            return;
        }

        staminaSlider.value = Mathf.Clamp(
            staminaSlider.value - fishingCatchStaminaCost,
            0f,
            maxStamina);
    }

    public void RestoreStamina(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        TryAssignReferences();
        if (staminaSlider == null)
        {
            return;
        }

        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = Mathf.Clamp(staminaSlider.value + amount, 0f, maxStamina);
    }

    // Kiem tra camera co dang nham vao mot cay hop le de chat hay khong.
    private bool IsLookingAtCuttableTree()
    {
        Terrain terrain = ResolveTerrain();
        if (terrain == null || sourceCamera == null || terrain.terrainData == null)
        {
            return false;
        }

        TreeInstance[] treeInstances = terrain.terrainData.treeInstances;
        TreePrototype[] prototypes = terrain.terrainData.treePrototypes;
        if (treeInstances == null || treeInstances.Length == 0 || prototypes == null || prototypes.Length == 0)
        {
            return false;
        }

        Vector3 rayOrigin = sourceCamera.transform.position;
        Vector3 rayDirection = sourceCamera.transform.forward;

        for (int i = 0; i < treeInstances.Length; i++)
        {
            TreeInstance currentTree = treeInstances[i];
            if (!IsTreePrototypeCuttable(prototypes, currentTree.prototypeIndex))
            {
                continue;
            }

            Vector3 worldPosition = GetTreeWorldPosition(terrain, currentTree);
            Vector3 targetPoint = worldPosition + Vector3.up * Mathf.Max(1f, chopTargetHeight * currentTree.heightScale);
            Vector3 toTarget = targetPoint - rayOrigin;
            float distance = toTarget.magnitude;

            if (distance > chopRange || distance <= Mathf.Epsilon)
            {
                continue;
            }

            float targetAngle = Vector3.Angle(rayDirection, toTarget);
            if (targetAngle > chopMaxTargetAngle)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsTreePrototypeCuttable(TreePrototype[] prototypes, int prototypeIndex)
    {
        if (prototypes == null || prototypeIndex < 0 || prototypeIndex >= prototypes.Length)
        {
            return false;
        }

        if (cuttablePrototypeKeywords == null || cuttablePrototypeKeywords.Length == 0)
        {
            return true;
        }

        GameObject prototypePrefab = prototypes[prototypeIndex].prefab;
        string prototypeName = prototypePrefab != null
            ? prototypePrefab.name
            : string.Empty;

        if (string.IsNullOrWhiteSpace(prototypeName))
        {
            return false;
        }

        string normalizedPrototypeName = prototypeName.ToLowerInvariant();
        for (int i = 0; i < cuttablePrototypeKeywords.Length; i++)
        {
            string keyword = cuttablePrototypeKeywords[i];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (normalizedPrototypeName.Contains(keyword.ToLowerInvariant()))
            {
                return true;
            }
        }

        return false;
    }

    private Terrain ResolveTerrain()
    {
        if (targetTerrain != null)
        {
            return targetTerrain;
        }

        targetTerrain = Terrain.activeTerrain;
        if (targetTerrain == null)
        {
            targetTerrain = Object.FindFirstObjectByType<Terrain>();
        }

        return targetTerrain;
    }

    private static Vector3 GetTreeWorldPosition(Terrain terrain, TreeInstance treeInstance)
    {
        Vector3 terrainPosition = terrain.GetPosition();
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 worldPosition = terrainPosition + Vector3.Scale(treeInstance.position, terrainSize);
        worldPosition.y = terrain.SampleHeight(worldPosition) + terrainPosition.y;
        return worldPosition;
    }

    private void RefreshAttackSubscription()
    {
        if (subscribedActionScript == actionScript)
        {
            return;
        }

        if (subscribedActionScript != null)
        {
            subscribedActionScript.AttackPerformed -= HandleAttackPerformed;
        }

        subscribedActionScript = actionScript;

        if (subscribedActionScript != null)
        {
            subscribedActionScript.AttackPerformed += HandleAttackPerformed;
        }
    }

    private void ClampValues()
    {
        maxStamina = Mathf.Max(1f, maxStamina);
        staminaFallRate = Mathf.Max(0f, staminaFallRate);
        idleStaminaFallRate = Mathf.Max(0f, idleStaminaFallRate);
        movementThreshold = Mathf.Max(0f, movementThreshold);
        chopStaminaCost = Mathf.Max(0f, chopStaminaCost);
        chopRange = Mathf.Max(0.5f, chopRange);
        chopTargetHeight = Mathf.Max(0.5f, chopTargetHeight);
        chopMaxTargetAngle = Mathf.Clamp(chopMaxTargetAngle, 1f, 45f);
        boarKillStaminaCost = Mathf.Max(0f, boarKillStaminaCost);
        fishingCatchStaminaCost = Mathf.Max(0f, fishingCatchStaminaCost);
    }

    private bool IsPlayerMoving()
    {
        if (playerMovement != null && playerMovement.IsCutscenePlaying)
        {
            return false;
        }

        if (playerRigidbody != null)
        {
            Vector3 horizontalVelocity = playerRigidbody.linearVelocity;
            horizontalVelocity.y = 0f;
            return horizontalVelocity.sqrMagnitude > movementThreshold * movementThreshold;
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        return moveX != 0f || moveZ != 0f;
    }

    private void TryAssignReferences()
    {
        if (staminaSlider == null)
        {
            staminaSlider = GetComponent<Slider>();
        }

        if (playerMovement == null)
        {
            playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        }

        if (playerRigidbody == null && playerMovement != null)
        {
            playerRigidbody = playerMovement.GetComponent<Rigidbody>();
        }

        if (actionScript == null)
        {
            actionScript = Object.FindFirstObjectByType<ActionScript>();
        }

        if (sourceCamera == null)
        {
            sourceCamera = Camera.main;
        }

        if (targetTerrain == null)
        {
            targetTerrain = Terrain.activeTerrain;
        }
    }
}
