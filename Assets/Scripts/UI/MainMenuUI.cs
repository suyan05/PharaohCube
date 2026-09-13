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

    [Header("Target Scene Names")]
    [SerializeField] private string gameSceneName = "SampleScene"; // 신전 탐험 씬 이름
    [SerializeField] private string settingsSceneName = "Settings"; // 설정 씬 이름

    private void Start()
    {
        // 메인 메뉴 시작 시 화면 서서히 밝아짐
        if (FadeController.Instance != null)
        {
            FadeController.Instance.FadeIn();
        }

        // 버튼 클릭 이벤트 등록
        if (btnStart != null) btnStart.onClick.AddListener(OnStartClicked);
        if (btnSettings != null) btnSettings.onClick.AddListener(OnSettingsClicked);
        if (btnExit != null) btnExit.onClick.AddListener(OnExitClicked);

        // 팝업 내부 버튼 이벤트
        if (btnConfirmExit != null) btnConfirmExit.onClick.AddListener(OnConfirmExitClicked);
        if (btnCancelExit != null) btnCancelExit.onClick.AddListener(OnCancelExitClicked);

        // 시작 시 종료 팝업 비활성화
        if (popupExitConfirm != null)
        {
            popupExitConfirm.SetActive(false);
        }
    }

    // 게임 시작 버튼: 페이드 아웃 후 신전 탐험 씬 로드
    private void OnStartClicked()
    {
        if (FadeController.Instance != null)
        {
            FadeController.Instance.FadeOut(() =>
            {
                SceneManager.LoadScene(gameSceneName);
            });
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    // 설정 버튼: 페이드 아웃 후 설정 씬 로드
    private void OnSettingsClicked()
    {
        if (FadeController.Instance != null)
        {
            FadeController.Instance.FadeOut(() =>
            {
                SceneManager.LoadScene(settingsSceneName);
            });
        }
        else
        {
            SceneManager.LoadScene(settingsSceneName);
        }
    }

    // 종료 버튼: 확인 팝업창 띄움
    private void OnExitClicked()
    {
        if (popupExitConfirm != null)
        {
            popupExitConfirm.SetActive(true);
        }
    }

    // 팝업 확인: 게임 종료
    private void OnConfirmExitClicked()
    {
        Debug.Log("[MainMenuUI] 게임 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 팝업 취소: 팝업 닫기
    private void OnCancelExitClicked()
    {
        if (popupExitConfirm != null)
        {
            popupExitConfirm.SetActive(false);
        }
    }
}