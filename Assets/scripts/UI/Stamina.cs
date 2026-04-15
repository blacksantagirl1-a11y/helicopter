using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class Stamina : MonoBehaviour
{
    public Slider staminaSlider;
    public PlayerMovement playerMovement;
    public Rigidbody playerRigidbody;
    public ActionScript actionScript;
    public Camera sourceCamera;
    public Terrain targetTerrain;
    public float maxStamina = 100f;
    public float staminaFallRate = 15f;
    public float idleStaminaFallRate = 10f;
    public float movementThreshold = 0.05f;

    [Header("Tree Chop Stamina")]
    public float chopStaminaCost = 8f;
    public float chopRange = 7f;
    public float chopTargetHeight = 2f;
    public float chopMaxTargetAngle = 12f;
    public string[] cuttablePrototypeKeywords = { "pine", "tree" };

    [Header("Boar Kill Stamina")]
    public float boarKillStaminaCost = 12f;

    private static readonly FieldInfo BoarHitsToKillField =
        typeof(Boar).GetField("hitsToKill", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BoarCurrentHealthField =
        typeof(Boar).GetField("currentHealth", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly List<Dictionary<int, int>> pendingBoarHealthSnapshots = new List<Dictionary<int, int>>();

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

        TryScheduleBoarKillStaminaCheck();
    }

    private void TryScheduleBoarKillStaminaCheck()
    {
        if (boarKillStaminaCost <= 0f)
        {
            return;
        }

        Dictionary<int, int> snapshot = CaptureCurrentBoarHealthSnapshot();
        if (snapshot.Count == 0)
        {
            return;
        }

        pendingBoarHealthSnapshots.Add(snapshot);
        StartCoroutine(ApplyBoarKillStaminaCostOnNextFrame(snapshot));
    }

    private System.Collections.IEnumerator ApplyBoarKillStaminaCostOnNextFrame(Dictionary<int, int> snapshot)
    {
        yield return null;

        if (staminaSlider == null)
        {
            pendingBoarHealthSnapshots.Remove(snapshot);
            yield break;
        }

        Dictionary<int, int> currentBoarHealth = CaptureCurrentBoarHealthSnapshot();
        int killedBoars = 0;

        foreach (KeyValuePair<int, int> pair in snapshot)
        {
            int boarId = pair.Key;
            int previousHealth = pair.Value;
            if (previousHealth <= 0)
            {
                continue;
            }

            if (!currentBoarHealth.TryGetValue(boarId, out int healthAfterAttack))
            {
                killedBoars++;
                continue;
            }

            if (healthAfterAttack <= 0)
            {
                killedBoars++;
            }
        }

        if (killedBoars > 0)
        {
            float totalStaminaCost = boarKillStaminaCost * killedBoars;
            staminaSlider.value = Mathf.Clamp(staminaSlider.value - totalStaminaCost, 0f, maxStamina);
        }

        pendingBoarHealthSnapshots.Remove(snapshot);
    }

    private Dictionary<int, int> CaptureCurrentBoarHealthSnapshot()
    {
        Boar[] boars = Object.FindObjectsByType<Boar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Dictionary<int, int> snapshot = new Dictionary<int, int>(boars.Length);

        for (int i = 0; i < boars.Length; i++)
        {
            Boar boar = boars[i];
            if (boar == null)
            {
                continue;
            }

            int boarId = boar.GetInstanceID();
            snapshot[boarId] = GetBoarCurrentHealth(boar);
        }

        return snapshot;
    }

    private static int GetBoarCurrentHealth(Boar boar)
    {
        if (boar == null)
        {
            return 0;
        }

        if (BoarCurrentHealthField != null)
        {
            object rawHealth = BoarCurrentHealthField.GetValue(boar);
            if (rawHealth is int currentHealth)
            {
                return Mathf.Max(0, currentHealth);
            }
        }

        int fallbackHitsToKill = 1;
        if (BoarHitsToKillField != null)
        {
            object rawHitsToKill = BoarHitsToKillField.GetValue(boar);
            if (rawHitsToKill is int hitsToKill)
            {
                fallbackHitsToKill = Mathf.Max(1, hitsToKill);
            }
        }

        return fallbackHitsToKill;
    }

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
