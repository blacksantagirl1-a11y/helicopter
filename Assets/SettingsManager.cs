using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SaveManager;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; set; }

    public Button backBTN;

    public Slider masterSlider;
    public GameObject masterValue;

    public Slider musicSlider;
    public GameObject musicValue;

    public Slider soundSlider;
    public GameObject soundValue; 

    private void Start()
    {
        backBTN.onClick.AddListener(() =>
        {
            SaveManager.Instance.SaveVolumeSettings(musicSlider.value, soundSlider.value, masterSlider.value);
        }); 

        StartCoroutine(LoadAndApplySettings());

    }

    private IEnumerator LoadAndApplySettings()
    {
        LoadAndSetVolume();
        yield return new WaitForSeconds(0.1f);
    }

    private void LoadAndSetVolume()
    {
        VolumeSettings volumeSettings = SaveManager.Instance.LoadVolumeSettings();

        masterSlider.value = volumeSettings.master;
        musicSlider.value = volumeSettings.music;
        soundSlider.value = volumeSettings.sound;

        print("Volume Settings are Loaded");
    }

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
    }

    private void Update()
    {
        masterValue.GetComponent<TextMeshProUGUI>().text = "" + (masterSlider.value) + "";
        musicValue.GetComponent<TextMeshProUGUI>().text = "" + (musicSlider.value) + "";
        soundValue.GetComponent<TextMeshProUGUI>().text = "" + (soundSlider.value) + "";
    }
}
