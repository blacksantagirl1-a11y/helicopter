using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public float[] playerStats;
    public float[] playerPositionAndRotation;
    //public string[] inventoryItems;

    public PlayerData(float[]_playerStats, float[]_playerPosAndRot/*, string[] _inventoryItems*/)
    {
        playerStats = _playerStats;
        playerPositionAndRotation = _playerPosAndRot;
        //inventoryItems = _inventoryItems;
    }
}
