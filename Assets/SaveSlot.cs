using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public class SaveSlot : MonoBehaviour
{
    private Button button;
    private TextMeshProUGUI buttonText;

    public int slotNumber;

    public GameObject alertUI;
    Button yesBTN;
    Button noBTN;


    private void Awake()
    {
        button = GetComponent<Button>();
        buttonText = transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();

        yesBTN = alertUI.transform.Find("YesButton").GetComponent<Button>();
        noBTN = alertUI.transform.Find("NoButton").GetComponent<Button>();
    }

    public void Start()
    {
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
        SaveManager.Instance.SaveGame(slotNumber);

        DateTime dt = DateTime.Now;
        string time = dt.ToString("yyyy-MM-dd HH:mm:ss");
        
        string description = "Saved Game" + slotNumber + " | " + time;

        buttonText.text = description;

        PlayerPrefs.SetString("Slot"+ slotNumber + "Description", description);

        SaveManager.Instance.DeselectButton();
    }
}
