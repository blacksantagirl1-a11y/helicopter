namespace IL3DN
{
    using UnityEngine;
    /// <summary>
    /// Override player sound when walking in different environments
    /// Attach this to a trigger
    /// </summary>
    public class IL3DN_ChangeWalkingSound : MonoBehaviour
    {
        [Tooltip("Bộ âm thanh bước chân ghi đè khi vào vùng trigger")]
        public AudioClip[] footStepsOverride;
        [Tooltip("Âm thanh nhảy ghi đè khi vào vùng trigger")]
        public AudioClip jumpSound;
        [Tooltip("Âm thanh tiếp đất ghi đè khi vào vùng trigger")]
        public AudioClip landSound;
    }
}
