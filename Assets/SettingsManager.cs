using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SaveManager;

public class SettingsManager : MonoBehaviour
{
    // Singleton cho man hinh settings cu trong menu.
    public static SettingsManager Instance { get; set; }

    // Nut back se kich hoat viec luu volume hien tai.
    public Button backBTN;

    // Slider va text hien thi gia tri cua tung nhom am thanh.
    public Slider masterSlider;
    public GameObject masterValue;

    public Slider musicSlider;
    public GameObject musicValue;

    public Slider soundSlider;
    public GameObject soundValue; 

    private void Start()
    {
        // Khi thoat settings thi luu lai volume vao SaveManager/PlayerPrefs.
        backBTN.onClick.AddListener(() =>
        {
            SaveManager.Instance.SaveVolumeSettings(musicSlider.value, soundSlider.value, masterSlider.value);
        }); 

        StartCoroutine(LoadAndApplySettings());

    }

    private IEnumerator LoadAndApplySettings()
    {
        // Coroutine giu cho viec nap settings co the chen delay neu UI can on dinh truoc.
        LoadAndSetVolume();
        yield return new WaitForSeconds(0.1f);
    }

    private void LoadAndSetVolume()
    {
        // Doc setting da luu va gan lai vao cac slider.
        VolumeSettings volumeSettings = SaveManager.Instance.LoadVolumeSettings();

        masterSlider.value = volumeSettings.master;
        musicSlider.value = volumeSettings.music;
        soundSlider.value = volumeSettings.sound;

        print("Volume Settings are Loaded");
    }

    private void Awake()
    {
        // Dam bao chi co mot SettingsManager trong scene.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        // Cap nhat text so cua slider de nguoi choi thay gia tri hien tai.
        masterValue.GetComponent<TextMeshProUGUI>().text = "" + (masterSlider.value) + "";
        musicValue.GetComponent<TextMeshProUGUI>().text = "" + (musicSlider.value) + "";
        soundValue.GetComponent<TextMeshProUGUI>().text = "" + (soundSlider.value) + "";
    }
}
