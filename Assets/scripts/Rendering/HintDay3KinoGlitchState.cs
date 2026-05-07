using UnityEngine;

public static class HintDay3KinoGlitchState
{
    public static float Amount { get; private set; }

    public static void SetAmount(float amount)
    {
        Amount = Mathf.Clamp01(amount);
    }

    public static void Clear()
    {
        Amount = 0f;
    }
}
