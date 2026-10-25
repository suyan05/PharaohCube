using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// [수정] 게임 시작 화면 (Title 씬)
// - 화면 오브젝트(배경, 로고, 버튼 4개, 글자)가 씬에 저장되어 Hierarchy에서 보이고 직접 고칠 수 있음
//   (Tools/UI/시작 화면 다시 만들기 메뉴 또는 이 컴포넌트 ⋮ 메뉴 "화면 오브젝트 만들기")
// - 배경·로고·설정 창 배경 그림은 아래 [그림] 칸에 끌어다 놓으면 바로 Scene/Game 창에 보임
// - 버튼: 이어하기(저장이 있을 때만) / 새 게임 / 설정 / 종료
public class TitleScreen : MonoBehaviour
{
    [Header("그림 (비우면 MenuTheme 값 -> 그것도 비우면 단색)")]
    [Tooltip("시작 화면 배경 (1920x1080 권장)")]
    [SerializeField] private Sprite titleBackground;
    [Tooltip("게임 로고 (넣으면 제목 글자 대신 그림)")]
    [SerializeField] private Sprite logo;
    [Tooltip("시작 화면에서 설정을 열 때 뒤에 깔리는 배경 (1920x1080 권장)")]
    [SerializeField] private Sprite settingsBackground;

    [Header("연출")]
    [Range(0f, 3f)] [SerializeField] private float fadeInTime = 1.2f;

    [Header("화면 오브젝트 (메뉴가 자동으로 채움, 직접 바꾸지 않아도 됨)")]
    [SerializeField] private Image background;
    [SerializeField] private Image logoImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_Text lastSaveText;
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private Image fade;

    private MenuTheme Theme => MenuUI.Theme;
    private Sprite BgSprite => titleBackground != null ? titleBackground : Theme.titleBackground;
    private Sprite LogoSprite => logo != null ? logo : Theme.logo;
    public Sprite SettingsBackground => settingsBackground != null ? settingsBackground : Theme.settingsBackground;

    // ================= 실행 =================
    private void Start()
    {
        Time.timeScale = 1f;
        MenuUI.EnsureEventSystem();
        if (newGameButton == null) BuildUI(); // 씬에 화면 오브젝트가 없으면 코드로 만듦
        ApplyArt();
        Bind();
        SoundManager.PlayBgm(Theme.titleBgm, 1f, 1.5f);
        StartCoroutine(FadeIn());
    }

    private void OnDestroy() => GameSettings.LanguageChanged -= RefreshLastSave;

    private void Bind()
    {
        Hook(continueButton, OnContinue);
        Hook(newGameButton, OnNewGame);
        Hook(settingsButton, OnSettings);
        Hook(quitButton, OnQuit);

        bool hasSave = SaveSystem.HasSave;
        if (continueButton != null) continueButton.gameObject.SetActive(hasSave);
        if (lastSaveText != null) lastSaveText.gameObject.SetActive(hasSave);
        RefreshLastSave();
        GameSettings.LanguageChanged += RefreshLastSave;
        if (versionText != null) versionText.text = Theme.versionText;
        MenuUI.Select(hasSave && continueButton != null ? continueButton : newGameButton);
    }

    // 씬에 저장된 버튼에도 실행 때 동작 연결 (클릭 소리 포함)
    private static void Hook(Button b, Action action)
    {
        if (b == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(() => { MenuUI.Click(); action(); });
    }

    private void RefreshLastSave()
    {
        if (lastSaveText != null) lastSaveText.text = Loc.Format("title.lastsave", SaveSystem.LastSaveInfo());
    }

    private IEnumerator FadeIn()
    {
        if (fade == null || fadeInTime <= 0f) yield break;
        fade.gameObject.SetActive(true);
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            fade.color = new Color(0f, 0f, 0f, 1f - t / fadeInTime);
            yield return null;
        }
        fade.gameObject.SetActive(false);
    }

    // ================= 그림 적용 (에디터에서 칸을 바꾸면 바로 보임) =================
    private void ApplyArt()
    {
        if (background != null)
        {
            background.sprite = BgSprite;
            background.color = BgSprite != null ? Color.white : Theme.backgroundColor;
        }
        bool hasLogo = LogoSprite != null;
        if (logoImage != null)
        {
            logoImage.sprite = LogoSprite;
            logoImage.enabled = hasLogo;
        }
        if (titleText != null) { titleText.gameObject.SetActive(!hasLogo); titleText.text = Theme.gameTitle; }
        if (subtitleText != null) { subtitleText.gameObject.SetActive(!hasLogo); subtitleText.text = Theme.gameSubtitle; }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Inspector에서 그림 칸을 바꾸면 Scene/Game 창에 바로 반영
        if (!Application.isPlaying && background != null) UnityEditor.EditorApplication.delayCall += () => { if (this != null) ApplyArt(); };
    }
#endif

    // ================= 화면 오브젝트 만들기 (메뉴 / 실행 중 없을 때) =================
    [ContextMenu("화면 오브젝트 만들기 (기존 TitleCanvas는 지움)")]
    public void BuildUI()
    {
        Transform old = transform.Find("TitleCanvas");
        if (old != null)
        {
            if (Application.isPlaying) Destroy(old.gameObject);
            else DestroyImmediate(old.gameObject);
        }

        MenuTheme th = Theme;
        Canvas canvas = MenuUI.NewCanvas("TitleCanvas", 100);
        canvas.transform.SetParent(transform, false);
        Transform root = canvas.transform;

        background = MenuUI.FullScreen("Background", root, th.backgroundColor, false);
        background.preserveAspect = false;

        // 로고: 그림(Logo Image) + 그림이 없을 때 쓰는 제목 글자
        RectTransform logoArea = MenuUI.Rect("Logo", root);
        logoArea.anchorMin = logoArea.anchorMax = new Vector2(0.5f, 0.62f); // [수정] 배경 위쪽 큐브 빛과 겹치지 않게 조금 아래
        logoArea.sizeDelta = new Vector2(1200f, 320f);
        logoImage = logoArea.gameObject.AddComponent<Image>();
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;
        logoImage.enabled = false;
        TextMeshProUGUI t = MenuUI.Text("TitleText", logoArea, null, 110f, th.accentColor, TextAlignmentOptions.Center, th.gameTitle);
        MenuUI.Stretch(t.rectTransform);
        t.rectTransform.offsetMin = new Vector2(0f, 90f);
        titleText = t;
        TextMeshProUGUI sub = MenuUI.Text("Subtitle", logoArea, null, 40f, th.textColor, TextAlignmentOptions.Center, th.gameSubtitle);
        MenuUI.Stretch(sub.rectTransform);
        sub.rectTransform.offsetMax = new Vector2(0f, -200f);
        subtitleText = sub;

        // 버튼 세로 줄 (앵커 가운데 아래 30%: 해상도가 바뀌어도 같은 자리)
        RectTransform menu = MenuUI.Rect("Menu", root);
        menu.anchorMin = menu.anchorMax = new Vector2(0.5f, 0.26f); // [수정] 로고가 내려간 만큼 버튼도 아래
        menu.sizeDelta = new Vector2(460f, 400f);
        MenuUI.Column(menu, 18f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);
        menuRoot = menu.gameObject;
        continueButton = MenuUI.Button("Continue", menu, "title.continue", 36f, null);
        newGameButton = MenuUI.Button("NewGame", menu, "title.newgame", 36f, null);
        settingsButton = MenuUI.Button("Settings", menu, "title.settings", 36f, null);
        quitButton = MenuUI.Button("Quit", menu, "title.quit", 36f, null);
        foreach (Button b in new[] { continueButton, newGameButton, settingsButton, quitButton }) MenuUI.Size(b, 0f, 76f);

        lastSaveText = MenuUI.Text("LastSave", root, null, 24f, th.disabledText, TextAlignmentOptions.Center, "");
        lastSaveText.rectTransform.anchorMin = lastSaveText.rectTransform.anchorMax = new Vector2(0.5f, 0.045f);
        lastSaveText.rectTransform.sizeDelta = new Vector2(800f, 40f);

        versionText = MenuUI.Text("Version", root, null, 22f, th.disabledText, TextAlignmentOptions.BottomRight, th.versionText);
        versionText.rectTransform.anchorMin = versionText.rectTransform.anchorMax = versionText.rectTransform.pivot = new Vector2(1f, 0f);
        versionText.rectTransform.anchoredPosition = new Vector2(-30f, 24f);
        versionText.rectTransform.sizeDelta = new Vector2(400f, 40f);

        fade = MenuUI.FullScreen("Fade", root, Color.black, true);
        fade.gameObject.SetActive(false); // 실행 때만 켜져서 밝아짐
        ApplyArt();
        Debug.Log("[TitleScreen] 화면 오브젝트 만듦 (TitleCanvas)", this);
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
        Debug.Log($"[TitleScreen] 새 게임 -> {Theme.newGameScene}");
        SoundManager.StopBgm(0.8f);
        MenuFader.LoadScene(Theme.newGameScene);
    }

    private void OnSettings()
    {
        if (menuRoot != null) menuRoot.SetActive(false);
        SettingsWindow.Open(() => { if (menuRoot != null) menuRoot.SetActive(true); }, settingsButton, SettingsBackground);
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
