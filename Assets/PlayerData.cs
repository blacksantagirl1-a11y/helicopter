using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public float[] playerStats;
    public float[] playerPositionAndRotation;
    public float[] staminaData;
    //public string[] inventoryItems;

    public PlayerData(float[]_playerStats, float[]_playerPosAndRot/*, string[] _inventoryItems*/)
    {
        playerStats = _playerStats;
        playerPositionAndRotation = _playerPosAndRot;
        //inventoryItems = _inventoryItems;
    }

    public PlayerData(float[]_playerStats, float[]_playerPosAndRot, float[] _staminaData/*, string[] _inventoryItems*/)
    {
        playerStats = _playerStats;
        playerPositionAndRotation = _playerPosAndRot;
        staminaData = _staminaData;
        //inventoryItems = _inventoryItems;
    }
}
