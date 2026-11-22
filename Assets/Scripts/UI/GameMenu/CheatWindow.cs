using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// [추가] 치트키 창 (설정 창의 "치트키" 버튼 -> 이 창, 게임 중 일시 정지 메뉴에서 연 설정에서만 보임)
// 버튼 11개 (2줄 격자): 서브 퍼즐 완료 / 소베크 메인 완료 / 모든 메인 완료 / 다음 스테이지 / 비석 해석 / 횃불 / 아이템 전부 / 라 F9 / 라 F10 / 이속 +5 / 이속 원래대로
// - 방을 바꾸는 치트(서브 퍼즐 완료, 소베크 메인 완료, 모든 메인 완료, 다음 스테이지, 라 밤 전환): 창·설정·일시 정지를 모두 닫고 다음 프레임에 실행
// - 나머지: 창을 연 채로 바로 실행 (여러 개 연달아 누를 수 있음), 결과는 화면 위 알림
// 치트 내용은 GameCheats, 끄려면 GameCheats.AllowInBuild = false
public class CheatWindow : MonoBehaviour
{
    private static CheatWindow current;

    public static bool IsOpen => current != null;
    public static int ClosedFrame { get; private set; } = -1;

    private Action onClosed;
    private int openedFrame;
    private TextMeshProUGUI status;
    private Button first;

    public static void Open(Action closed = null)
    {
        if (current != null || !GameCheats.Enabled) return;
        Canvas canvas = MenuUI.NewCanvas("CheatWindow", 1250);
        current = canvas.gameObject.AddComponent<CheatWindow>();
        current.onClosed = closed;
        current.openedFrame = Time.frameCount;
        current.Build(canvas.transform);
        Debug.Log("[CheatWindow] 열림");
    }

    // ================= 만들기 =================
    private void Build(Transform root)
    {
        MenuTheme th = MenuUI.Theme;
        MenuUI.FullScreen("Dim", root, th.dimColor);
        Image panel = MenuUI.Panel("Panel", root, new Vector2(1000f, 910f));
        MenuUI.Column(panel.transform, 14f, new RectOffset(50, 50, 36, 36));

        TextMeshProUGUI title = MenuUI.Text("Title", panel.transform, "cheat.title", 48f, th.accentColor);
        MenuUI.Size(title, 0f, 72f);

        // 버튼 격자 (2줄)
        RectTransform grid = MenuUI.Rect("Grid", panel.transform);
        MenuUI.Size(grid, 0f, 6 * 74f + 5 * 14f); // [수정] 버튼 11개 -> 6줄
        var g = grid.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(440f, 74f);
        g.spacing = new Vector2(20f, 14f);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 2;
        g.childAlignment = TextAnchor.UpperCenter;

        first = Add(grid, "Subs", "cheat.subs", GameCheats.ClearSubPuzzles, true);
        Add(grid, "SobekMain", "cheat.sobekmain", GameCheats.ClearSobekMain, true); // [추가]
        Add(grid, "AllMains", "cheat.allmains", GameCheats.ClearAllMains, true); // [추가]
        Add(grid, "Next", "cheat.next", GameCheats.NextStage, true);
        Add(grid, "Steles", "cheat.steles", GameCheats.DecodeAllSteles, false);
        Add(grid, "Torch", "cheat.torch", GameCheats.GiveTorch, false);
        Add(grid, "Items", "cheat.items", GameCheats.GiveAllItems, false);
        Add(grid, "RaNight", "cheat.ranight", GameCheats.RaNight, true);
        Add(grid, "RaPhase", "cheat.raphase", GameCheats.RaPhase, false);
        Add(grid, "Speed", "cheat.speed", GameCheats.SpeedUp, false);
        Add(grid, "SpeedReset", "cheat.speedreset", GameCheats.SpeedReset, false);

        status = MenuUI.Text("Status", panel.transform, null, 26f, th.textColor, TextAlignmentOptions.Center);
        MenuUI.Size(status, 0f, 40f);
        TextMeshProUGUI note = MenuUI.Text("Note", panel.transform, "cheat.note", 24f, th.disabledText, TextAlignmentOptions.Center);
        MenuUI.Size(note, 0f, 34f);

        // 아래: Esc 안내 + 닫기
        RectTransform bottom = MenuUI.Rect("Bottom", panel.transform);
        MenuUI.Size(bottom, 0f, 76f);
        var h = bottom.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 30f;
        h.childAlignment = TextAnchor.MiddleRight;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = true;
        TextMeshProUGUI hint = MenuUI.Text("Hint", bottom, "hint.back", 24f, th.disabledText, TextAlignmentOptions.Left);
        MenuUI.Size(hint, 600f, 0f);
        Button close = MenuUI.Button("Close", bottom, "settings.close", 30f, Close);
        MenuUI.Size(close, 220f, 0f);

        RefreshStatus();
        MenuUI.Select(first);
    }

    // closesMenus: 방이 바뀌는 치트 -> 메뉴를 모두 닫고 일시 정지를 푼 뒤 실행
    private Button Add(Transform parent, string objName, string key, Action action, bool closesMenus)
    {
        return MenuUI.Button(objName, parent, key, 28f, () =>
        {
            if (closesMenus)
            {
                CloseAll();
                CheatRunner.Run(action, true);
            }
            else
            {
                CheatRunner.Run(action, false);
                RefreshStatus();
            }
        });
    }

    private void RefreshStatus()
    {
        if (status == null) return;
        string room = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        status.text = $"지금 방: {room}   |   이동 속도 보너스: +{GameCheats.SpeedBonus:0}";
    }

    // ================= 닫기 =================
    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || Time.frameCount == openedFrame) return;
        if (kb.escapeKey.wasPressedThisFrame) Close();
    }

    private void Close()
    {
        if (current != this) return;
        ClosedFrame = Time.frameCount;
        current = null;
        Action a = onClosed;
        Destroy(gameObject);
        Debug.Log("[CheatWindow] 닫힘");
        a?.Invoke();
    }

    // 치트 창 + 설정 창 + 일시 정지 메뉴를 모두 닫음
    private void CloseAll()
    {
        if (current == this) { ClosedFrame = Time.frameCount; current = null; }
        Destroy(gameObject);
        SettingsWindow.CloseIfOpen();
        PauseMenu.ResumeIfPaused();
        Debug.Log("[CheatWindow] 치트 실행 -> 메뉴 모두 닫음");
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }
}
