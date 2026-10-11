using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static AnubisUIKit;

// 실전 모래 분배기 패널 생성 (에디터 전용)
// 메뉴: Tools > Anubis > 실전 퍼즐 > 1. 모래 분배기 (항아리 4개) 만들기
// 기존 Panel_Sand를 지우고 새로 만든 뒤, A1 제단에 다시 연결

public static class AnubisRealSandBuilder
{
    [MenuItem("Tools/Anubis/실전 퍼즐/1. 모래 분배기 (항아리 4개) 만들기")]
    private static void Build()
    {
        if (!Prepare()) return;
        if (!Confirm("실전 모래 분배기", "Panel_Sand를 실전 버전(항아리 4개, 숫자 숨김)으로 바꿉니다.")) return;

        var root = ReplacePanel("Panel_Sand", "사자의 모래 분배기", out var stations);
        var t = root.transform;
        Txt("Desc", t, V(240, 370), V(1100, 50), "항아리를 하나 고른 뒤, 모래를 부을 항아리를 고르십시오.   눈금은 오래전에 지워졌습니다.", 24, Sand);

        // 왼쪽: 양피지 힌트 상자
        AddHintBox(root, "Scroll_Room1", V(-560, -20), V(500, 660));

        // 오른쪽: 항아리 4개
        string[] names = { "첫째 항아리", "둘째 항아리", "셋째 항아리", "넷째 항아리" };
        int[] caps = { 12, 8, 5, 3 };
        float[] xs = { -150f, 110f, 370f, 630f };

        var buttons = new Button[4];
        var fills = new Image[4];
        var highlights = new GameObject[4];

        for (int i = 0; i < 4; i++)
        {
            float h = 70f + caps[i] * 26f;
            var size = V(200, h);
            var pos = V(xs[i], -220f + h * 0.5f);

            var hl = Img($"Highlight{i + 1}", t, pos, size + V(24, 24), Gold, UISprite);
            hl.type = Image.Type.Sliced;
            hl.gameObject.SetActive(false);
            highlights[i] = hl.gameObject;

            var jar = Img($"Jar{i + 1}", t, pos, size, SlotCol, UISprite, true);
            jar.type = Image.Type.Sliced;
            buttons[i] = jar.gameObject.AddComponent<Button>();
            buttons[i].targetGraphic = jar;

            var fill = Img("SandFill", jar.transform, V(0, 0), size - V(24, 24), new Color(0.86f, 0.72f, 0.42f, 1f), UISprite);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.fillAmount = i == 0 ? 1f : 0f;
            fills[i] = fill;

            Txt($"Name{i + 1}", t, V(xs[i], -255), V(240, 40), names[i], 24, Sand);
        }

        var moves = Txt("Text_MoveCount", t, V(240, -320), V(600, 44), "붓기 횟수: 0", 26, TextCol);
        var reset = Btn("Btn_Reset", t, V(240, -410), V(280, 70), "처음부터 다시", 26);
        var close = CloseBtn(t);
        var notice = Notice("SuccessNotice", t, V(240, 60), V(900, 200), "", out var noticeText);

        var c = root.AddComponent<SandPuzzleReal>();
        WireArray(c, "jarButtons", buttons);
        WireArray(c, "jarFills", fills);
        WireArray(c, "jarHighlights", highlights);
        Wire(c, ("btnReset", reset), ("btnClose", close), ("successNotice", notice),
                ("textSuccessNotice", noticeText), ("textMoveCount", moves));

        Relink(stations, root);
        FinishPanel(root);
        Debug.Log("[AnubisRealSandBuilder] 실전 모래 분배기 생성 완료. Ctrl+S로 저장하세요.");
    }
}