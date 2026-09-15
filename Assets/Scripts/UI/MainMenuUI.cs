using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Menu Buttons")]
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnExit;

    [Header("Exit Popup")]
    [SerializeField] private GameObject popupExitConfirm;
    [SerializeField] private Button btnConfirmExit;
    [SerializeField] private Button btnCancelExit;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string settingsSceneName = "Settings";

    private void Start()
    {
        if (FadeController.Instance != null)
        {
            FadeController.Instance.FadeIn();
        }

        BindEvents();

        if (popupExitConfirm != null)
        {
            popupExitConfirm.SetActive(false);
        }
    }

    private void BindEvents()
    {
        if (btnStart != null) btnStart.onClick.AddListener(OnStartClicked);
        if (btnSettings != null) btnSettings.onClick.AddListener(OnSettingsClicked);
        if (btnExit != null) btnExit.onClick.AddListener(OnExitClicked);

        if (btnConfirmExit != null) btnConfirmExit.onClick.AddListener(OnConfirmExitClicked);
        if (btnCancelExit != null) btnCancelExit.onClick.AddListener(OnCancelExitClicked);
    }

    private void OnStartClicked()
    {
        if (FadeController.Instance != null)
        {
            FadeController.Instance.FadeOutAndLoadScene(gameSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private void OnSettingsClicked()
    {
        SettingsSceneUI.previousSceneName = "MainMenu";

        SceneManager.LoadScene(settingsSceneName);
    }

    private void OnExitClicked()
    {
        if (popupExitConfirm != null)
        {
            popupExitConfirm.SetActive(true);
        }
        else
        {
            QuitGame();
        }
    }

    private void OnConfirmExitClicked()
    {
        QuitGame();
    }

    private void OnCancelExitClicked()
    {
        if (popupExitConfirm != null)
        {
            popupExitConfirm.SetActive(false);
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}