using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public class SaveSlot : MonoBehaviour
{
    // Nut save va text hien thi mo ta slot tren UI.
    private Button button;
    private TextMeshProUGUI buttonText;

    // So thu tu slot save ma nut nay dai dien.
    public int slotNumber;

    // Popup xac nhan khi nguoi choi sap ghi de slot da co du lieu.
    public GameObject alertUI;
    Button yesBTN;
    Button noBTN;


    private void Awake()
    {
        // Lay component cua slot va hai nut trong popup canh bao.
        button = GetComponent<Button>();
        buttonText = transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();

        yesBTN = alertUI.transform.Find("YesButton").GetComponent<Button>();
        noBTN = alertUI.transform.Find("NoButton").GetComponent<Button>();
    }

    public void Start()
    {
        // Slot trong thi save ngay, slot da co du lieu thi hien canh bao ghi de.
        button.onClick.AddListener(() =>
        {
            if (SaveManager.Instance.IsSlotEmpty(slotNumber))
            {
                SaveGameConfirmed();
            }
            else
            {
                DisplayOverrideWarning();
            }
        }

        );
    }

    private void Update()
    {
        // Cap nhat mo ta slot de nguoi choi biet slot trong hay da duoc luu luc nao.
        if (SaveManager.Instance.IsSlotEmpty(slotNumber))
        {
            buttonText.text = "Empty";
        }
        else
        {
            buttonText.text = PlayerPrefs.GetString("Slot"+ slotNumber + "Description");
        }
    }

    public void DisplayOverrideWarning()
    {
        // Mo popup va gan hanh dong Yes/No cho lan ghi de hien tai.
        alertUI.SetActive(true);

        yesBTN.onClick.AddListener(() =>
        {
            SaveGameConfirmed();
            alertUI.SetActive(false);
        });

        noBTN.onClick.AddListener(() =>
        {
            alertUI.SetActive(false);
        });
    }

    private void SaveGameConfirmed()
    {
        // Luu game vao slot, tao mo ta theo thoi gian va bo focus nut UI.
        SaveManager.Instance.SaveGame(slotNumber);

        DateTime dt = DateTime.Now;
        string time = dt.ToString("yyyy-MM-dd HH:mm:ss");
        
        string description = "Saved Game" + slotNumber + " | " + time;

        buttonText.text = description;

        PlayerPrefs.SetString("Slot"+ slotNumber + "Description", description);

        SaveManager.Instance.DeselectButton();
    }
}
