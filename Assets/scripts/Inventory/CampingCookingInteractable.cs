using UnityEngine;

[DisallowMultipleComponent]
public sealed class CampingCookingInteractable : Interactable
{
    [Header("Cooking Interaction")]
    [SerializeField] private string cookingPrompt = "Nhan E de nau an";
    [SerializeField] private string eatPrompt = "Nhan E de an";
    [SerializeField] private float staminaRestoreAmount = 35f;

    private int cookedFoodCount;

    public override bool HasPromptText => true;
    public override string PromptText => cookedFoodCount > 0 ? eatPrompt : cookingPrompt;

    public void AddCookedFood(int amount = 1)
    {
        cookedFoodCount += Mathf.Max(1, amount);
    }

    protected override void Interact()
    {
        if (cookedFoodCount > 0)
        {
            EatCookedFood();
            return;
        }

        CampingCookingModeController controller = FindFirstObjectByType<CampingCookingModeController>();
        if (controller == null)
        {
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory != null)
            {
                controller = inventory.GetComponent<CampingCookingModeController>();
                if (controller == null)
                {
                    controller = inventory.gameObject.AddComponent<CampingCookingModeController>();
                }
            }
        }

        if (controller != null)
        {
            controller.EnterCookingMode(transform);
        }
    }

    private void EatCookedFood()
    {
        Stamina stamina = FindFirstObjectByType<Stamina>();
        if (stamina == null)
        {
            return;
        }

        stamina.RestoreStamina(staminaRestoreAmount);
        cookedFoodCount = Mathf.Max(0, cookedFoodCount - 1);
        PlayEatSound();
    }

    private static void PlayEatSound()
    {
        SoundManager soundManager = ResolveSoundManager();
        PlayOneShot(soundManager != null ? soundManager.eatSource : null);
    }

    private static SoundManager ResolveSoundManager()
    {
        return SoundManager.Instance != null
            ? SoundManager.Instance
            : FindFirstObjectByType<SoundManager>();
    }

    private static void PlayOneShot(AudioSource audioSource)
    {
        if (audioSource == null)
        {
            return;
        }

        if (audioSource.clip != null)
        {
            audioSource.PlayOneShot(audioSource.clip);
            return;
        }

        audioSource.Play();
    }
}
