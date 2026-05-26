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

    string jsonPathProject;

    string jsonPathPersistant;
    string binaryPath;

    string fileName = "SaveGame";


    public bool isSavingJson;

    private void Start()
    {
        jsonPathProject = Application.dataPath + Path.AltDirectorySeparatorChar;
        jsonPathPersistant = Application.persistentDataPath + Path.AltDirectorySeparatorChar;
        binaryPath = Application.persistentDataPath + Path.AltDirectorySeparatorChar;
    }


    public AllGameData LoadingTypeSwitch(int slotNumber)
    {
        if(isSavingJson)
        {
            AllGameData gameData = LoadGameDataFromJsonFile(slotNumber);
            return gameData;
        }
        else
        {
            AllGameData gameData = LoadGameDataFromBinaryFile(slotNumber);
            return gameData;
        }
    }

    public void LoadGame(int slotNumber)
    {
        SetPlayerData(LoadingTypeSwitch(slotNumber).playerData);

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


    public void StartLoadedGame(int slotNumber)
    {
        SceneManager.LoadScene("InGame");

        StartCoroutine(DelayedLoading(slotNumber));
    }
    private IEnumerator DelayedLoading(int slotNumber)
    {
        yield return new WaitForSeconds(1f);
        LoadGame(slotNumber);

        print("Game Loaded");
    }


    public void SavingTypeSwitch(AllGameData gameData, int slotNumber)
    {
        if(isSavingJson)
        {
            SaveGameDataToJsonFile(gameData, slotNumber);
        }
        else 
        {
            SaveGameDataToBinaryFile(gameData, slotNumber);
        }
    }

    public void SaveGame(int slotNumber)
    {
        AllGameData data = new AllGameData();

        data.playerData = GetPlayerData();
        SavingTypeSwitch(data, slotNumber);
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


#region To Binary Section

    public void SaveGameDataToBinaryFile(AllGameData gameData, int slotNumber)
    {
        BinaryFormatter formatter = new BinaryFormatter();

        FileStream steam = new FileStream(binaryPath + fileName + slotNumber + ".binary", FileMode.Create);

        formatter.Serialize(steam, gameData);
        steam.Close();

        print ("Data saved to" + binaryPath + fileName + slotNumber + ".binary");
       
    }

    public AllGameData LoadGameDataFromBinaryFile(int slotNumber)
    {
        if (File.Exists(binaryPath + fileName + slotNumber + ".binary"))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream steam = new FileStream(binaryPath + fileName + slotNumber + ".binary", FileMode.Open);

            AllGameData gameData = formatter.Deserialize(steam) as AllGameData;
            steam.Close();

            print ("Data loaded from" + binaryPath + fileName + slotNumber + ".binary");

            return gameData;
        }
        else
        {
            return null;
        }
    }

#endregion


    public void SaveGameDataToJsonFile(AllGameData gameData , int slotNumber)
    {
       String json = JsonUtility.ToJson(gameData);

       String encrypted = EncryptionDecryption(json);

       using (StreamWriter writer = new StreamWriter(jsonPathProject + fileName + slotNumber + ".json"))
       {
           writer.Write(encrypted);
           print ("Saved Game to Json file at:" + jsonPathProject + fileName + slotNumber + ".json");
       };

    }

    public AllGameData LoadGameDataFromJsonFile(int slotNumber)
    {
        using (StreamReader reader = new StreamReader(jsonPathProject + fileName + slotNumber + ".json"))
        {
            string json = reader.ReadToEnd();

            string decrypted = EncryptionDecryption(json);

            AllGameData gameData = JsonUtility.FromJson<AllGameData>(decrypted);
            return gameData;
        };


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

#region Encryption

    public string EncryptionDecryption(string jsonString)
    {
        string keyword = "1234567";
        string result = "";
        for (int i = 0; i < jsonString.Length; i++)
        {
            result += (char)(jsonString[i] ^ keyword[i % keyword.Length]);
        }
        return result;
    }

#endregion


#region Utility
public bool DoesFileExists(int slotNumber)
    {
        if (isSavingJson)
        {
            if (System.IO.File.Exists(jsonPathProject + fileName + slotNumber + ".json"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (System.IO.File.Exists(binaryPath + fileName + slotNumber + ".bin"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool IsSlotEmpty(int slotNumber)
    {
        if (DoesFileExists(slotNumber))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void DeselectButton()
    {
        GameObject myEventSystem = GameObject.Find("EventSystem");
        myEventSystem.GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(null);
    }

#endregion



}

