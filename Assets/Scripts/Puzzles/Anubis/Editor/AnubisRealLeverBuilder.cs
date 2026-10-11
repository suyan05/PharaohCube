using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static AnubisUIKit;

// 실전 지렛대 천칭 패널 생성 (에디터 전용)
// 메뉴: Tools > Anubis > 실전 퍼즐 > 3. 지렛대 천칭 (5칸, 무게 숨김) 만들기
// 기존 Panel_Lever를 지우고 새로 만든 뒤, A3 제단에 다시 연결
public static class AnubisRealLeverBuilder
{
    private const int SlotCount = 5;
    private const float Spacing = 70f;   // LeverPuzzleReal의 slotSpacing과 같아야 함
    private const int HeartSlot = 3;

    [MenuItem("Tools/Anubis/실전 퍼즐/3. 지렛대 천칭 (5칸, 무게 숨김) 만들기")]
    private static void Build()
    {
        if (!Prepare()) return;
        if (!Confirm("실전 지렛대 천칭", "Panel_Lever를 실전 버전(5칸, 심장 3번째 칸, 무게 숨김)으로 바꿉니다.")) return;

        var root = ReplacePanel("Panel_Lever", "아누비스의 지렛대 천칭", out var stations);
        var t = root.transform;
        Txt("Desc", t, V(240, 370), V(1100, 50), "흑요석 추의 개수와 걸이 위치를 정한 뒤, 평형을 확인하십시오.", 24, Sand);

        // 왼쪽: 양피지 힌트 상자
        AddHintBox(root, "Scroll_Room3", V(-560, -20), V(500, 660));

        // 받침대 + 지렛대 (중심 x = 270)
        const float cx = 270f;
        Img("Fulcrum", t, V(cx, -40), V(70, 160), new Color(0.30f, 0.22f, 0.12f, 1f), UISprite);
        var bar = Img("LeverBar", t, V(cx, 60), V(SlotCount * Spacing * 2 + 80, 20), Gold, UISprite).rectTransform;
        Img("Pivot", bar, V(0, 0), V(34, 34), GoldDark, KnobSprite);

        // 눈금 (양쪽 1~5칸)
        for (int i = 1; i <= SlotCount; i++)
        {
            Img($"TickR{i}", bar, V(i * Spacing, 0), V(4, 28), SlotCol);
            Img($"TickL{i}", bar, V(-i * Spacing, 0), V(4, 28), SlotCol);
            Txt($"NumR{i}", bar, V(i * Spacing, 34), V(50, 32), i.ToString(), 22, Sand);
            Txt($"NumL{i}", bar, V(-i * Spacing, 34), V(50, 32), i.ToString(), 22, Sand);
        }

        var heart = NewRect("Heart", bar, V(-HeartSlot * Spacing, 0), V(10, 10));
        Img("Rope", heart, V(0, -55), V(4, 110), Sand);
        var heartImg = Img("HeartBody", heart, V(0, -140), V(90, 90), new Color(0.75f, 0.12f, 0.15f, 1f), KnobSprite);
        Txt("Label", heartImg.transform, V(0, -66), V(160, 34), "망자의 심장", 20, TextCol);

        var hook = Img("RightHook", bar, V(Spacing, 0), V(22, 22), Sand, KnobSprite).rectTransform;
        Img("Rope", hook, V(0, -50), V(4, 100), Sand);
        var weights = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            var w = Img($"Obsidian{i + 1}", hook, V(0, -122 - i * 50), V(70, 44), new Color(0.15f, 0.12f, 0.20f, 1f), UISprite);
            w.type = Image.Type.Sliced;
            Txt("Label", w.transform, V(0, 0), V(70, 40), "흑요석", 16, Sand);
            weights[i] = w.gameObject;
        }

        // 조작부
        var txtDist = Txt("Text_Distance", t, V(30, -290), V(260, 76), "", 26, TextCol);
        var distMinus = Btn("Btn_DistMinus", t, V(-45, -370), V(84, 60), "-", 34);
        var distPlus = Btn("Btn_DistPlus", t, V(105, -370), V(84, 60), "+", 34);

        var txtCount = Txt("Text_Count", t, V(310, -290), V(260, 76), "", 26, TextCol);
        var countMinus = Btn("Btn_CountMinus", t, V(235, -370), V(84, 60), "-", 34);
        var countPlus = Btn("Btn_CountPlus", t, V(385, -370), V(84, 60), "+", 34);

        var attempts = Txt("Text_Attempts", t, V(600, -260), V(260, 40), "시도 횟수: 0", 22, Sand);
        var check = Btn("Btn_Check", t, V(600, -340), V(260, 84), "평형 확인", 30, GoldDark);
        var feedback = Txt("Text_Feedback", t, V(240, -455), V(1100, 46), "", 26, TextCol);
        var close = CloseBtn(t);
        var notice = Notice("SuccessNotice", t, V(240, 60), V(900, 200), "", out var noticeText);

        var c = root.AddComponent<LeverPuzzleReal>();
        WireArray(c, "weights", weights);
        Wire(c, ("leverBar", bar), ("heartObject", heart), ("rightHook", hook),
                ("btnDistMinus", distMinus), ("btnDistPlus", distPlus),
                ("btnCountMinus", countMinus), ("btnCountPlus", countPlus),
                ("btnCheck", check), ("btnClose", close),
                ("textDistance", txtDist), ("textCount", txtCount),
                ("textFeedback", feedback), ("textAttempts", attempts),
                ("successNotice", notice), ("textSuccessNotice", noticeText));

        Relink(stations, root);
        FinishPanel(root);
        Debug.Log("[AnubisRealLeverBuilder] 실전 지렛대 천칭 생성 완료. Ctrl+S로 저장하세요.");
    }
}