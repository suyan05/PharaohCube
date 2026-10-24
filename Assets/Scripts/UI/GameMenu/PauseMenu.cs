using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// [추가] 게임 중 Esc 일시 정지 메뉴 (계속하기 / 설정 / 시작 화면으로 / 게임 종료)
// GameMenuBootstrap이 게임 씬마다 자동으로 하나 만듦 (씬에 직접 둘 필요 없음)
// Esc를 다른 화면이 쓰는 중이면 열리지 않음: 퍼즐 화면, 대화창, 획득 팝업, 연출, 고고학 수첩, 확인 창
public class PauseMenu : MonoBehaviour
{
    private static PauseMenu instance;

    public static bool IsPaused { get; private set; }

    private GameObject root;
    private Button resumeButton, settingsButton;
    private float prevTimeScale = 1f;
    private bool blockedLastFrame;
    private readonly object lockOwner = new object();

    public static void EnsureInScene()
    {
        if (instance != null) return;
        var go = new GameObject("PauseMenu");
        instance = go.AddComponent<PauseMenu>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsPaused = false;
        instance = null;
    }

    // ================= Esc =================
    private void Update()
    {
        bool blocked = OtherUIOpen();
        Keyboard kb = Keyboard.current;
        bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;

        if (esc)
        {
            if (IsPaused)
            {
                // 설정 창 / 확인 창이 열려 있으면 그쪽이 Esc를 씀
                if (!SettingsWindow.IsOpen && !ConfirmDialog.IsOpen &&
                    SettingsWindow.ClosedFrame != Time.frameCount && ConfirmDialog.ClosedFrame != Time.frameCount)
                    Resume();
            }
            else if (!blocked && !blockedLastFrame) // 이번 프레임에 다른 화면이 Esc로 닫혔으면 열지 않음
            {
                Pause();
            }
        }
        blockedLastFrame = blocked;
    }

    private static bool OtherUIOpen()
    {
        return PuzzleOverlayFocus.IsEscapeConsumed || DialogueBox.IsOpen || ItemPopup.IsShowing || Cine.Running
               || NotebookUI.IsOpen || SettingsWindow.IsOpen || ConfirmDialog.IsOpen || MenuFader.Busy;
    }

    // ================= 열기 / 닫기 =================
    public void Pause()
    {
        if (IsPaused) return;
        if (root == null) Build();
        IsPaused = true;
        prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        PlayerLock.Lock(lockOwner); // 멈춘 동안 E 조사·이동 막기
        root.SetActive(true);
        MenuUI.Select(resumeButton);
        Debug.Log("[PauseMenu] 일시 정지");
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = prevTimeScale;
        PlayerLock.Unlock(lockOwner);
        if (root != null) root.SetActive(false);
        Debug.Log("[PauseMenu] 계속하기");
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (IsPaused) { IsPaused = false; Time.timeScale = 1f; PlayerLock.Unlock(lockOwner); }
    }

    // ================= 버튼 =================
    private void OpenSettings()
    {
        root.SetActive(false);
        SettingsWindow.Open(() => { if (IsPaused) root.SetActive(true); }, settingsButton);
    }

    private void ToTitle(Selectable from)
    {
        ConfirmDialog.Show("confirm.totitle", () =>
        {
            SaveSystem.SaveNow();
            PlayerLock.Unlock(lockOwner);
            IsPaused = false;
            MenuFader.LoadScene(MenuUI.Theme.titleScene); // 페이더가 timeScale을 1로 돌림
        }, null, from);
    }

    private void QuitGame(Selectable from)
    {
        ConfirmDialog.Show("confirm.quit", () =>
        {
            SaveSystem.SaveNow();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }, null, from);
    }

    private void Build()
    {
        MenuTheme th = MenuUI.Theme;
        Canvas canvas = MenuUI.NewCanvas("PauseCanvas", 1100);
        canvas.transform.SetParent(transform, false);
        root = canvas.gameObject;

        MenuUI.FullScreen("Dim", root.transform, th.dimColor);
        Image panel = MenuUI.Panel("Panel", root.transform, new Vector2(620f, 640f));
        MenuUI.Column(panel.transform, 22f, new RectOffset(70, 70, 50, 50), TextAnchor.MiddleCenter);

        TextMeshProUGUI title = MenuUI.Text("Title", panel.transform, "pause.title", 50f, th.accentColor);
        MenuUI.Size(title, 0f, 90f);
        resumeButton = MenuUI.Button("Resume", panel.transform, "pause.resume", 34f, Resume);
        settingsButton = MenuUI.Button("Settings", panel.transform, "pause.settings", 34f, OpenSettings);
        Button toTitle = null, quit = null;
        toTitle = MenuUI.Button("ToTitle", panel.transform, "pause.totitle", 34f, () => ToTitle(toTitle));
        quit = MenuUI.Button("Quit", panel.transform, "pause.quit", 34f, () => QuitGame(quit));
        foreach (Button b in new[] { resumeButton, settingsButton, toTitle, quit }) MenuUI.Size(b, 0f, 78f);

        root.SetActive(false);
    }
}
