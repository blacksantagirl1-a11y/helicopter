using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadSlot : MonoBehaviour
{
    // Nut load va text hien thi mo ta slot tren UI.
    public Button button;
    public TextMeshProUGUI buttonText;

    // So thu tu slot save ma nut nay dai dien.
    public int slotNumber;

    private void Awake()
    {
        // Lay cac component can dung ngay tren GameObject cua slot.
        button = GetComponent<Button>();
        buttonText = transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        // Moi frame cap nhat text de slot hien dung trang thai Empty/da co save.
        if (SaveManager.Instance.IsSlotEmpty(slotNumber))
        {
            buttonText.text = "";
        }
        else
        {
            buttonText.text = PlayerPrefs.GetString("Slot"+ slotNumber + "Description");
        }
    }

    private void Start()
    {
        // Chi cho load khi slot da co file save.
        button.onClick.AddListener(() =>
        {
            if (SaveManager.Instance.IsSlotEmpty(slotNumber) == false)
            {
                SaveManager.Instance.StartLoadedGame(slotNumber);
                SaveManager.Instance.DeselectButton();
            }
            else
            {
                //
            }
        });
    }
}
