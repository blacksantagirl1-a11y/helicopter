using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{

    private const string MainMenuMusicTrackName = "MainMenu";

    public Button LoadGameBTN;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        MusicManager.Instance?.PlayMusic(MainMenuMusicTrackName);

        LoadGameBTN.onClick.AddListener(() =>
        {
            StopMainMenuMusic();
            
        });
    }

    private void OnDestroy()
    {
        StopMainMenuMusic();
    }

    public void NewGame()
    {
        DialogueNewGameResetService.ResetToDay1();
        StopMainMenuMusic();
        SceneManager.LoadScene("InGame");
    }

    private void StopMainMenuMusic()
    {
        MusicManager.Instance?.StopMusic(0f);
    }

    public void ExitGame()
    {
        Debug.Log("Quitting Game");
        Application.Quit();
    }
}
