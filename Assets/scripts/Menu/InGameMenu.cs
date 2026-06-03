using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenu : MonoBehaviour
{
    public void BackToMainMenu()
    {
        if (!LoadingManager.LoadScene("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
