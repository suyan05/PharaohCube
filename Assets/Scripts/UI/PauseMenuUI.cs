using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 게임 중 일시정지 메뉴 (Esc)
// 퍼즐 화면이 열려 있으면 Esc는 퍼즐 닫기에 양보 -> 닫힌 다음 Esc부터 일시정지
public class PauseMenuUI : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string SettingsSceneName = "Settings";

    [Header("UI Panels")]
    [SerializeField] private GameObject pausePanel;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;

    // 다른 스크립트(상호작용 등)가 일시정지 중인지 확인할 때 사용
    public static bool IsPaused { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsPaused = false;
    }

    private void Awake()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // 퍼즐이 열려 있거나 이번 프레임에 닫혔으면 이 Esc는 퍼즐 것
        if (!IsPaused && PuzzleOverlayFocus.IsEscapeConsumed) return;

        if (IsPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        IsPaused = true;
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OpenSettings()
    {
        try
        {
            ResumeGame();
            SettingsSceneUI.previousSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(SettingsSceneName);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PauseMenuUI] 설정 씬 이동 실패: {e}");
        }
    }

    private void GoToMainMenu()
    {
        try
        {
            ResumeGame();

            if (FadeController.Instance != null)
            {
                FadeController.Instance.FadeOutAndLoadScene(MainMenuSceneName);
                return;
            }
            SceneManager.LoadScene(MainMenuSceneName);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PauseMenuUI] 메인 메뉴 이동 실패: {e}");
        }
    }

    private void OnDestroy()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }
}