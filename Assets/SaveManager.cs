using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        DontDestroyOnLoad(gameObject);
    }


    public bool isSavingJson;


    public AllGameData LoadingTypeSwitch()
    {
        if(isSavingJson)
        {
            AllGameData gameData = LoadGameDataFromBinaryFile();
            return gameData;
        }
        else
        {
            AllGameData gameData = LoadGameDataFromBinaryFile();
            return gameData;
        }
    }

    public void LoadGame()
    {
        SetPlayerData(LoadingTypeSwitch().playerData);

    }

    private void SetPlayerData(PlayerData playerData)
    {
        
        PlayerState.Instance.currentHealthy = playerData.playerStats[0];
        PlayerState.Instance.currentCarlories = playerData.playerStats[1];
        PlayerState.Instance.currentHydrationPercent = playerData.playerStats[2];

        
        Vector3 loadedPosition;
        loadedPosition.x = playerData.playerPositionAndRotation[0];
        loadedPosition.y = playerData.playerPositionAndRotation[1];
        loadedPosition.z = playerData.playerPositionAndRotation[2];

        PlayerState.Instance.playerBody.transform.position = loadedPosition;


        Vector3 loadedRotation;
        loadedRotation.x = playerData.playerPositionAndRotation[3];
        loadedRotation.y = playerData.playerPositionAndRotation[4];
        loadedRotation.z = playerData.playerPositionAndRotation[5];

        PlayerState.Instance.playerBody.transform.rotation = Quaternion.Euler(loadedRotation);
    }


    public void StartLoadedGame()
    {
        SceneManager.LoadScene("InGame");

        StartCoroutine(DelayedLoading());
    }
    private IEnumerator DelayedLoading()
    {
        yield return new WaitForSeconds(1f);
        LoadGame();

        print("Game Loaded");
    }


    public void SavingTypeSwitch(AllGameData gameData)
    {
        if(isSavingJson)
        {
            //SaveGameDataToJsonFile(gameData);
        }
        else 
        {
            SaveGameDataToBinaryFile(gameData);
        }
    }

    public void SaveGame()
    {
        AllGameData data = new AllGameData();

        data.playerData = GetPlayerData();
        SavingTypeSwitch(data);
    }

    private PlayerData GetPlayerData()
    {
        float[] playerStats = new float[3];
        playerStats[0] = PlayerState.Instance.currentHealthy;
        playerStats[1] = PlayerState.Instance.currentCarlories;
        playerStats[2] = PlayerState.Instance.currentHydrationPercent;

        float[] playerPosAndRot = new float[6];
        playerPosAndRot[0] = PlayerState.Instance.playerBody.transform.position.x;
        playerPosAndRot[1] = PlayerState.Instance.playerBody.transform.position.y;
        playerPosAndRot[2] = PlayerState.Instance.playerBody.transform.position.z;

        playerPosAndRot[3] = PlayerState.Instance.playerBody.transform.rotation.x;
        playerPosAndRot[4] = PlayerState.Instance.playerBody.transform.rotation.y;
        playerPosAndRot[5] = PlayerState.Instance.playerBody.transform.rotation.z;

        return new PlayerData(playerStats, playerPosAndRot);
    }

    public void SaveGameDataToBinaryFile(AllGameData gameData)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/save_game.bin";
        FileStream steam = new FileStream(path, FileMode.Create);

        formatter.Serialize(steam, gameData);
        steam.Close();

        print ("Data saved to" + Application.persistentDataPath + "/save_game.bin");
       
    }

    public AllGameData LoadGameDataFromBinaryFile()
    {
        string path = Application.persistentDataPath + "/save_game.bin";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream steam = new FileStream(path, FileMode.Open);

            AllGameData gameData = formatter.Deserialize(steam) as AllGameData;
            steam.Close();

            print ("Data loaded from" + Application.persistentDataPath + "/save_game.bin");

            return gameData;
        }
        else
        {
            return null;
        }


    }


    [System.Serializable]
    public class VolumeSettings
    {
        public float music;
        public float sound;
        public float master;
    }

    public void SaveVolumeSettings(float _music, float _sound, float _master)
    {
        VolumeSettings volumeSettings = new VolumeSettings()
        {
            music = _music,
            sound = _sound,
            master = _master
        };    

        PlayerPrefs.SetString("Volume", JsonUtility.ToJson(volumeSettings));
        PlayerPrefs.Save();

        print("Saved to Player Pref");
    }

    public VolumeSettings LoadVolumeSettings()
    {
        return JsonUtility.FromJson<VolumeSettings>(PlayerPrefs.GetString("Volume"));
    }

}


