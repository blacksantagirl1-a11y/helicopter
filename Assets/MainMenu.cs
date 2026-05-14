using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{

    public Button LoadGameBTN;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        LoadGameBTN.onClick.AddListener(() =>
        {
            SaveManager.Instance.StartLoadedGame();
        });
    }

    public void NewGame()
    {
        SceneManager.LoadScene("Suml");
    }

    public void ExitGame()
    {
        Debug.Log("Quitting Game");
        Application.Quit();
    }
}
