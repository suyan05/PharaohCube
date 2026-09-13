using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SettingsSceneUI : MonoBehaviour
{
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
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // 복귀할 메인 화면 이름

    private const string BGMKey = "Setting_BGM";
    private const string SFXKey = "Setting_SFX";
    private const string QualityKey = "Setting_Quality";
    private const string ResolutionKey = "Setting_Resolution";
    private const string LanguageKey = "Setting_Language";

    private Resolution[] availableResolutions;

    private void Start()
    {
        // 씬 시작 시 화면 페이드 인
        if (FadeController.Instance != null)
        {
            FadeController.Instance.FadeIn();
        }

        InitResolutions();
        LoadSettings();
        BindEvents();
    }

    private void BindEvents()
    {
        btnBack.onClick.AddListener(OnBackClicked);

        sliderBGM.onValueChanged.AddListener(OnBGMChanged);
        sliderSFX.onValueChanged.AddListener(OnSFXChanged);
        dropdownQuality.onValueChanged.AddListener(OnQualityChanged);
        dropdownResolution.onValueChanged.AddListener(OnResolutionChanged);
        dropdownLanguage.onValueChanged.AddListener(OnLanguageChanged);
    }

    // 모니터 해상도 목록을 드롭다운에 채움
    private void InitResolutions()
    {
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
        sliderBGM.value = PlayerPrefs.GetFloat(BGMKey, 0.8f);
        sliderSFX.value = PlayerPrefs.GetFloat(SFXKey, 0.8f);
        dropdownQuality.value = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        dropdownLanguage.value = PlayerPrefs.GetInt(LanguageKey, 0); // 0: 한국어, 1: 영어 , 2 : 기타 언어
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
        Resolution res = availableResolutions[index];
        Screen.SetResolution(res.width, res.height, FullScreenMode.FullScreenWindow);
        PlayerPrefs.SetInt(ResolutionKey, index);
    }

    private void OnLanguageChanged(int index)
    {
        PlayerPrefs.SetInt(LanguageKey, index);
        Debug.Log($"[SettingsSceneUI] 언어 변경됨: {(index == 0 ? "한국어" : "English")}");
    }

    private void OnBackClicked()
    {
        PlayerPrefs.Save();

        // 뒤로가기 클릭 시 페이드 아웃 후 메인 메뉴로 복귀
        if (FadeController.Instance != null)
        {
            FadeController.Instance.FadeOut(() =>
            {
                SceneManager.LoadScene(mainMenuSceneName);
            });
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
