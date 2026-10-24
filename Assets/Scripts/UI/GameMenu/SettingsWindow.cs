using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// [추가] 설정 창 (시작 화면과 게임 중 일시 정지 메뉴에서 같은 창이 뜸, 씬 이동 없음)
// 소리: 전체 / 배경음 / 효과음
// 화면: 해상도 / 화면 모드 / 수직 동기화 (해상도·모드는 빌드에서만 바뀜, 에디터 Game 창은 그대로)
// 언어: 한국어 / English (메뉴 글자만)
// 값은 바꾸는 즉시 적용 + 저장 (GameSettings)
public class SettingsWindow : MonoBehaviour
{
    private static SettingsWindow current;

    public static bool IsOpen => current != null;
    public static int ClosedFrame { get; private set; } = -1;

    private Action onClosed;
    private Selectable returnTo;
    private int openedFrame;

    private Slider master, bgm, sfx;
    private OptionSelector resolution, mode, vsync, language;
    private Button closeButton;

    // returnTo: 닫은 뒤 다시 고를 버튼 (키보드 조작용)
    public static void Open(Action closed = null, Selectable returnTo = null)
    {
        if (current != null) return;
        GameSettings.Load();
        Canvas canvas = MenuUI.NewCanvas("SettingsWindow", 1200);
        current = canvas.gameObject.AddComponent<SettingsWindow>();
        current.onClosed = closed;
        current.returnTo = returnTo;
        current.openedFrame = Time.frameCount;
        current.Build(canvas.transform);
        Debug.Log("[SettingsWindow] 열림");
    }

    public static void CloseIfOpen()
    {
        if (current != null) current.Close();
    }

    // ================= 만들기 =================
    private void Build(Transform root)
    {
        MenuTheme th = MenuUI.Theme;
        MenuUI.FullScreen("Dim", root, th.dimColor);
        Image panel = MenuUI.Panel("Panel", root, new Vector2(1060f, 980f));
        MenuUI.Column(panel.transform, 6f, new RectOffset(70, 70, 40, 40));

        TextMeshProUGUI title = MenuUI.Text("Title", panel.transform, "settings.title", 52f, th.accentColor);
        MenuUI.Size(title, 0f, 80f);

        // 소리
        MenuUI.Header(panel.transform, "settings.sound");
        master = MenuUI.Slider("Master", MenuUI.Row("Row_Master", panel.transform, "settings.master"), GameSettings.Master, GameSettings.SetMaster, out _);
        bgm = MenuUI.Slider("Bgm", MenuUI.Row("Row_Bgm", panel.transform, "settings.bgm"), GameSettings.Bgm, GameSettings.SetBgm, out _);
        sfx = MenuUI.Slider("Sfx", MenuUI.Row("Row_Sfx", panel.transform, "settings.sfx"), GameSettings.Sfx, v =>
        {
            GameSettings.SetSfx(v);
            MenuUI.Click(); // 효과음 크기 미리 듣기
        }, out _);

        // 화면
        MenuUI.Header(panel.transform, "settings.display");
        List<Vector2Int> resList = GameSettings.Resolutions;
        resolution = OptionSelector.Create("Resolution", MenuUI.Row("Row_Resolution", panel.transform, "settings.resolution"),
            resList.Count, GameSettings.ResolutionIndex(), i => $"{resList[i].x} x {resList[i].y}", i => GameSettings.SetResolution(resList[i]));
        mode = OptionSelector.Create("Mode", MenuUI.Row("Row_Mode", panel.transform, "settings.mode"),
            2, GameSettings.Fullscreen ? 0 : 1, i => Loc.Get(i == 0 ? "mode.full" : "mode.window"), i => GameSettings.SetFullscreen(i == 0));
        vsync = OptionSelector.Create("VSync", MenuUI.Row("Row_VSync", panel.transform, "settings.vsync"),
            2, GameSettings.VSync ? 0 : 1, i => Loc.Get(i == 0 ? "common.on" : "common.off"), i => GameSettings.SetVSync(i == 0));

        // 언어
        MenuUI.Header(panel.transform, "settings.language");
        language = OptionSelector.Create("Language", MenuUI.Row("Row_Language", panel.transform, "settings.lang"),
            2, (int)GameSettings.Language, i => Loc.Get(i == 0 ? "lang.ko" : "lang.en"), i => GameSettings.SetLanguage((GameLanguage)i));
        TextMeshProUGUI note = MenuUI.Text("LangNote", panel.transform, "settings.langnote", 24f, th.disabledText, TextAlignmentOptions.Right);
        MenuUI.Size(note, 0f, 36f);

        // 아래 버튼
        RectTransform bottom = MenuUI.Rect("Bottom", panel.transform);
        MenuUI.Size(bottom, 0f, 84f);
        var h = bottom.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 30f;
        h.childAlignment = TextAnchor.MiddleRight;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = true;
        h.padding = new RectOffset(0, 0, 14, 0);
        TextMeshProUGUI hint = MenuUI.Text("Hint", bottom, "hint.back", 24f, th.disabledText, TextAlignmentOptions.Left);
        MenuUI.Size(hint, 380f, 0f);
        Button reset = MenuUI.Button("Reset", bottom, "settings.reset", 30f, ResetDefaults);
        closeButton = MenuUI.Button("Close", bottom, "settings.close", 30f, Close);
        MenuUI.Size(reset, 220f, 0f);
        MenuUI.Size(closeButton, 220f, 0f);

        GameSettings.LanguageChanged += RefreshLabels;
        MenuUI.Select(master);
    }

    // 언어가 바뀌면 "<  값  >" 글자도 다시 (LocalizedText가 아닌 글자)
    private void RefreshLabels()
    {
        if (mode != null) mode.RefreshLabel();
        if (vsync != null) vsync.RefreshLabel();
        if (language != null) language.RefreshLabel();
    }

    private void ResetDefaults()
    {
        GameSettings.ResetToDefault();
        master.value = GameSettings.Master;
        bgm.value = GameSettings.Bgm;
        sfx.SetValueWithoutNotify(GameSettings.Sfx);
        resolution.SetIndex(GameSettings.ResolutionIndex(), false);
        mode.SetIndex(GameSettings.Fullscreen ? 0 : 1, false);
        vsync.SetIndex(GameSettings.VSync ? 0 : 1, false);
    }

    // ================= 닫기 =================
    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || Time.frameCount == openedFrame || ConfirmDialog.IsOpen) return;
        if (kb.escapeKey.wasPressedThisFrame) Close();
    }

    private void Close()
    {
        if (current != this) return;
        ClosedFrame = Time.frameCount;
        current = null;
        GameSettings.LanguageChanged -= RefreshLabels;
        Action a = onClosed;
        Selectable back = returnTo;
        Destroy(gameObject);
        Debug.Log("[SettingsWindow] 닫힘 (설정 저장됨)");
        a?.Invoke();                              // 메뉴를 다시 켠 뒤
        if (back != null) MenuUI.Select(back);    // 버튼 고르기 (꺼진 버튼은 못 고름)
    }

    private void OnDestroy()
    {
        GameSettings.LanguageChanged -= RefreshLabels;
        if (current == this) current = null;
    }
}
