using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// [추가] 게임 시작 화면 (Title 씬의 빈 오브젝트에 붙임 -> 실행하면 화면을 코드로 만듦)
// 버튼: 이어하기(저장이 있을 때만) / 새 게임 / 설정 / 종료
// 모양·배경·로고·배경음은 Resources/MenuTheme.asset에서 바꿈
public class TitleScreen : MonoBehaviour
{
    [Tooltip("처음 밝아지는 시간 (초)")]
    [Range(0f, 3f)] [SerializeField] private float fadeInTime = 1.2f;

    private Button continueButton, newGameButton, settingsButton, quitButton;
    private GameObject menuRoot;
    private Image fade;

    private void Start()
    {
        Time.timeScale = 1f;
        Build();
        SoundManager.PlayBgm(MenuUI.Theme.titleBgm, 1f, 1.5f);
        StartCoroutine(FadeIn());
    }

    private void Build()
    {
        MenuTheme th = MenuUI.Theme;
        Canvas canvas = MenuUI.NewCanvas("TitleCanvas", 100);
        Transform root = canvas.transform;

        // 배경
        Image bg = MenuUI.FullScreen("Background", root, th.titleBackground != null ? Color.white : th.backgroundColor, false);
        if (th.titleBackground != null) { bg.sprite = th.titleBackground; bg.preserveAspect = false; }

        // 로고 (그림이 있으면 그림, 없으면 글자)
        RectTransform logoArea = MenuUI.Rect("Logo", root);
        logoArea.anchorMin = logoArea.anchorMax = new Vector2(0.5f, 0.70f);
        logoArea.sizeDelta = new Vector2(1200f, 320f);
        if (th.logo != null)
        {
            var img = logoArea.gameObject.AddComponent<Image>();
            img.sprite = th.logo;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        else
        {
            TextMeshProUGUI t = MenuUI.Text("TitleText", logoArea, null, 110f, th.accentColor, TextAlignmentOptions.Center, th.gameTitle);
            MenuUI.Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(0f, 90f);
            TextMeshProUGUI sub = MenuUI.Text("Subtitle", logoArea, null, 40f, th.textColor, TextAlignmentOptions.Center, th.gameSubtitle);
            MenuUI.Stretch(sub.rectTransform);
            sub.rectTransform.offsetMax = new Vector2(0f, -200f);
        }

        // 버튼 세로 줄
        RectTransform menu = MenuUI.Rect("Menu", root);
        menu.anchorMin = menu.anchorMax = new Vector2(0.5f, 0.30f);
        menu.sizeDelta = new Vector2(460f, 420f);
        MenuUI.Column(menu, 18f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);
        menuRoot = menu.gameObject;

        bool hasSave = SaveSystem.HasSave;
        if (hasSave) continueButton = MenuUI.Button("Continue", menu, "title.continue", 36f, OnContinue);
        newGameButton = MenuUI.Button("NewGame", menu, "title.newgame", 36f, OnNewGame);
        settingsButton = MenuUI.Button("Settings", menu, "title.settings", 36f, OnSettings);
        quitButton = MenuUI.Button("Quit", menu, "title.quit", 36f, OnQuit);
        foreach (Button b in new[] { continueButton, newGameButton, settingsButton, quitButton })
            if (b != null) MenuUI.Size(b, 0f, 76f);

        // 마지막 저장 시간 (이어하기 아래 작은 글자)
        if (hasSave)
        {
            TextMeshProUGUI info = MenuUI.Text("LastSave", root, null, 24f, th.disabledText, TextAlignmentOptions.Center,
                Loc.Format("title.lastsave", SaveSystem.LastSaveInfo()));
            info.rectTransform.anchorMin = info.rectTransform.anchorMax = new Vector2(0.5f, 0.08f);
            info.rectTransform.sizeDelta = new Vector2(800f, 40f);
            GameSettings.LanguageChanged += () => { if (info != null) info.text = Loc.Format("title.lastsave", SaveSystem.LastSaveInfo()); };
        }

        // 버전 (오른쪽 아래)
        TextMeshProUGUI ver = MenuUI.Text("Version", root, null, 22f, th.disabledText, TextAlignmentOptions.BottomRight, th.versionText);
        ver.rectTransform.anchorMin = ver.rectTransform.anchorMax = ver.rectTransform.pivot = new Vector2(1f, 0f);
        ver.rectTransform.anchoredPosition = new Vector2(-30f, 24f);
        ver.rectTransform.sizeDelta = new Vector2(400f, 40f);

        // 처음엔 검은 화면에서 밝아짐
        fade = MenuUI.FullScreen("Fade", root, Color.black, true);
        MenuUI.Select(continueButton != null ? continueButton : newGameButton);
    }

    private IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            fade.color = new Color(0f, 0f, 0f, 1f - t / fadeInTime);
            yield return null;
        }
        fade.gameObject.SetActive(false);
    }

    // ================= 버튼 =================
    private void OnContinue()
    {
        string scene = SaveSystem.PrepareContinue();
        if (string.IsNullOrEmpty(scene)) { Debug.LogWarning("[TitleScreen] 저장 파일을 읽지 못함 -> 새 게임으로"); StartNew(); return; }
        Debug.Log($"[TitleScreen] 이어하기 -> {scene}");
        SoundManager.StopBgm(0.8f);
        if (!MenuFader.LoadScene(scene)) Debug.LogError($"[TitleScreen] '{scene}' 씬으로 갈 수 없음 (Scene List 확인)");
    }

    private void OnNewGame()
    {
        if (SaveSystem.HasSave) ConfirmDialog.Show("confirm.newgame", StartNew, null, newGameButton);
        else StartNew();
    }

    private void StartNew()
    {
        SaveSystem.PrepareNewGame();
        Debug.Log($"[TitleScreen] 새 게임 -> {MenuUI.Theme.newGameScene}");
        SoundManager.StopBgm(0.8f);
        MenuFader.LoadScene(MenuUI.Theme.newGameScene);
    }

    private void OnSettings()
    {
        menuRoot.SetActive(false);
        SettingsWindow.Open(() => menuRoot.SetActive(true), settingsButton);
    }

    private void OnQuit()
    {
        ConfirmDialog.Show("confirm.quit", () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }, null, quitButton);
    }
}
