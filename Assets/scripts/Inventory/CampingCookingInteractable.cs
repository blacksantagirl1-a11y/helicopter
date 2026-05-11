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
    public override string PromptText => ShouldCookBeforeEating() ? cookingPrompt : HasCookedFoodAvailable() ? eatPrompt : cookingPrompt;

    public void AddCookedFood(int amount = 1)
    {
        int safeAmount = Mathf.Max(1, amount);
        cookedFoodCount += safeAmount;
        DailyQuestManager.ReportInteraction(DailyQuestManager.Day5CookedFoodInteractionKey, safeAmount);
    }

    protected override void Interact()
    {
        if (HasCookedFoodAvailable() && !ShouldCookBeforeEating())
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
        if (stamina != null)
        {
            stamina.RestoreStamina(staminaRestoreAmount);
        }

        if (cookedFoodCount > 0)
        {
            cookedFoodCount = Mathf.Max(0, cookedFoodCount - 1);
        }

        PlayEatSound();
        DailyQuestManager.ReportInteraction(DailyQuestManager.Day5AteFoodInteractionKey);
    }

    private static void PlayEatSound()
    {
        ReSoundManager.Resolve()?.PlaySound2D(SoundIds.Eat);
    }

    private static bool ShouldCookBeforeEating()
    {
        return DailyQuestManager.ShouldPrioritizeDay5CookingOverEating();
    }

    private bool HasCookedFoodAvailable()
    {
        return cookedFoodCount > 0 || DailyQuestManager.CanEatDay5CookedFood();
    }
}
