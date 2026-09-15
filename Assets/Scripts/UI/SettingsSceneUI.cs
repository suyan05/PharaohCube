using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SettingsSceneUI : MonoBehaviour
{
    public static string previousSceneName = "MainMenu";

    [Header("Audio Sliders")]
    [SerializeField] private Slider sliderBGM;
    [SerializeField] private Slider sliderSFX;

    [Header("Graphics Dropdowns")]
    [SerializeField] private TMP_Dropdown dropdownResolution;
    [SerializeField] private TMP_Dropdown dropdownQuality;

    [Header("Language Dropdown")]
    [SerializeField] private TMP_Dropdown dropdownLanguage;

    [Header("Buttons")]
    [SerializeField] private Button btnBack;

    [Header("Target Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private const string BGMKey = "Setting_BGM";
    private const string SFXKey = "Setting_SFX";
    private const string QualityKey = "Setting_Quality";
    private const string ResolutionKey = "Setting_Resolution";
    private const string LanguageKey = "Setting_Language";

    private Resolution[] availableResolutions;

    private void Start()
    {
        InitResolutions();
        LoadSettings();
        BindEvents();
    }

    private void BindEvents()
    {
        if (btnBack != null)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(OnBackClicked);
        }

        if (sliderBGM != null) sliderBGM.onValueChanged.AddListener(OnBGMChanged);
        if (sliderSFX != null) sliderSFX.onValueChanged.AddListener(OnSFXChanged);
        if (dropdownQuality != null) dropdownQuality.onValueChanged.AddListener(OnQualityChanged);
        if (dropdownResolution != null) dropdownResolution.onValueChanged.AddListener(OnResolutionChanged);
        if (dropdownLanguage != null) dropdownLanguage.onValueChanged.AddListener(OnLanguageChanged);
    }

    private void InitResolutions()
    {
        if (dropdownResolution == null) return;

        availableResolutions = Screen.resolutions;
        dropdownResolution.ClearOptions();

        List<string> options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            string optionText = $"{availableResolutions[i].width} x {availableResolutions[i].height}";
            options.Add(optionText);

            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentResIndex = i;
            }
        }

        dropdownResolution.AddOptions(options);
        dropdownResolution.value = PlayerPrefs.GetInt(ResolutionKey, currentResIndex);
        dropdownResolution.RefreshShownValue();
    }

    private void LoadSettings()
    {
        if (sliderBGM != null) sliderBGM.value = PlayerPrefs.GetFloat(BGMKey, 0.8f);
        if (sliderSFX != null) sliderSFX.value = PlayerPrefs.GetFloat(SFXKey, 0.8f);
        if (dropdownQuality != null) dropdownQuality.value = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        if (dropdownLanguage != null) dropdownLanguage.value = PlayerPrefs.GetInt(LanguageKey, 0);
    }

    private void OnBGMChanged(float val) => PlayerPrefs.SetFloat(BGMKey, val);
    private void OnSFXChanged(float val) => PlayerPrefs.SetFloat(SFXKey, val);

    private void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index);
        PlayerPrefs.SetInt(QualityKey, index);
    }

    private void OnResolutionChanged(int index)
    {
        if (availableResolutions == null || index >= availableResolutions.Length) return;
        Resolution res = availableResolutions[index];
        Screen.SetResolution(res.width, res.height, FullScreenMode.FullScreenWindow);
        PlayerPrefs.SetInt(ResolutionKey, index);
    }

    private void OnLanguageChanged(int index)
    {
        PlayerPrefs.SetInt(LanguageKey, index);
    }

    private void OnBackClicked()
    {
        PlayerPrefs.Save();
        Time.timeScale = 1f;

        string targetScene = string.IsNullOrEmpty(previousSceneName) || previousSceneName == "Settings"
                             ? mainMenuSceneName
                             : previousSceneName;

        SceneManager.LoadScene(targetScene);
    }
}