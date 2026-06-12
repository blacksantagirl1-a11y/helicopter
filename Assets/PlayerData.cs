using UnityEngine;

[System.Serializable]
public class PlayerData
{
    // Chi so sinh ton cua nguoi choi: mau, calo va do mat nuoc.
    public float[] playerStats;

    // Vi tri va goc xoay cua playerBody de dua nhan vat ve dung cho khi load.
    public float[] playerPositionAndRotation;

    // Trang thai stamina: gia tri hien tai, max slider va max stamina.
    public float[] staminaData;
    //public string[] inventoryItems;

    // Constructor cu giu tuong thich voi save khong co stamina.
    public PlayerData(float[]_playerStats, float[]_playerPosAndRot/*, string[] _inventoryItems*/)
    {
        playerStats = _playerStats;
        playerPositionAndRotation = _playerPosAndRot;
        //inventoryItems = _inventoryItems;
    }

    // Constructor moi luu them stamina de phuc hoi day du trang thai sinh ton.
    public PlayerData(float[]_playerStats, float[]_playerPosAndRot, float[] _staminaData/*, string[] _inventoryItems*/)
    {
        playerStats = _playerStats;
        playerPositionAndRotation = _playerPosAndRot;
        staminaData = _staminaData;
        //inventoryItems = _inventoryItems;
    }
}
