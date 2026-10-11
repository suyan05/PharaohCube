using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static AnubisUIKit;

// 실전 도르래·기어 천칭 패널 생성 (에디터 전용)
// 메뉴: Tools > Anubis > 실전 퍼즐 > 2. 기어 천칭 (기어 3 + 도르래 2) 만들기
// 기존 Panel_Gear를 지우고 새로 만든 뒤, A2 제단에 다시 연결
public static class AnubisRealGearBuilder
{
    [MenuItem("Tools/Anubis/실전 퍼즐/2. 기어 천칭 (기어 3 + 도르래 2) 만들기")]
    private static void Build()
    {
        if (!Prepare()) return;
        if (!Confirm("실전 기어 천칭", "Panel_Gear를 실전 버전(기어 3개, 도르래 2개, 숫자 숨김)으로 바꿉니다.")) return;

        var root = ReplacePanel("Panel_Gear", "도르래와 황금 기어비 천칭", out var stations);
        var t = root.transform;
        Txt("Desc", t, V(240, 370), V(1100, 50), "바퀴를 눌러 크기를 바꾸고, 도르래를 눌러 종류를 바꾼 뒤 크랭크를 돌리십시오.", 24, Sand);

        // 왼쪽: 양피지 힌트 상자
        AddHintBox(root, "Scroll_Room2", V(-560, -20), V(500, 660));

        // 기어 3개
        string[] roles = { "첫 바퀴", "가운데 바퀴", "마지막 바퀴" };
        float[] xs = { -140f, 110f, 360f };
        var gears = new RectTransform[3];
        var labels = new TextMeshProUGUI[3];
        var buttons = new Button[3];

        for (int i = 0; i < 3; i++)
        {
            gears[i] = Gear($"Gear{i + 1}", t, V(xs[i], 170));
            Txt($"Role{i + 1}", t, V(xs[i], 20), V(240, 36), roles[i], 24, Sand);
            labels[i] = Txt($"Text_Gear{i + 1}", t, V(xs[i], -20), V(240, 40), "작은 바퀴", 26, TextCol);
            buttons[i] = Btn($"Btn_Gear{i + 1}", t, V(xs[i], -80), V(200, 56), "바퀴 바꾸기", 22);
        }

        // 도르래 2개 + 밧줄 + 석관
        Img("Rope", t, V(620, 145), V(6, 290), Sand);
        var pulleyBtns = new Button[2];
        var pulleyImgs = new Image[2];
        var pulleyLabels = new TextMeshProUGUI[2];
        float[] py = { 290f, 170f };
        for (int i = 0; i < 2; i++)
        {
            var img = Img($"Pulley{i + 1}", t, V(620, py[i]), V(100, 100), new Color(0.6f, 0.6f, 0.6f, 1f), KnobSprite, true);
            pulleyBtns[i] = img.gameObject.AddComponent<Button>();
            pulleyBtns[i].targetGraphic = img;
            pulleyImgs[i] = img;
            Img("Hub", img.transform, V(0, 0), V(26, 26), SlotCol, KnobSprite);
            pulleyLabels[i] = Txt($"Text_Pulley{i + 1}", t, V(765, py[i]), V(170, 60), "고정 도르래", 22, TextCol);
        }

        var relic = Img("RelicPlate", t, V(620, -10), V(190, 90), new Color(0.45f, 0.35f, 0.20f, 1f), UISprite);
        relic.type = Image.Type.Sliced;
        Txt("Label", relic.transform, V(0, 0), V(180, 80), "봉인된 석관", 24, TextCol);

        // 하단: 피드백 / 시도 횟수 / 크랭크
        var feedback = Txt("Text_Feedback", t, V(240, -190), V(1100, 50), "", 26, TextCol);
        var attempts = Txt("Text_Attempts", t, V(240, -250), V(600, 40), "시도 횟수: 0", 24, Sand);
        var crank = Btn("Btn_Crank", t, V(240, -340), V(320, 80), "크랭크 돌리기", 30, GoldDark);
        var close = CloseBtn(t);
        var notice = Notice("SuccessNotice", t, V(240, 60), V(900, 200), "", out var noticeText);

        var c = root.AddComponent<GearPuzzleReal>();
        WireArray(c, "gearTransforms", gears);
        WireArray(c, "gearLabels", labels);
        WireArray(c, "gearButtons", buttons);
        WireArray(c, "pulleyButtons", pulleyBtns);
        WireArray(c, "pulleyImages", pulleyImgs);
        WireArray(c, "pulleyLabels", pulleyLabels);
        Wire(c, ("relicPlate", relic.rectTransform), ("btnCrank", crank), ("btnClose", close),
                ("textFeedback", feedback), ("textAttempts", attempts),
                ("successNotice", notice), ("textSuccessNotice", noticeText));

        Relink(stations, root);
        FinishPanel(root);
        Debug.Log("[AnubisRealGearBuilder] 실전 기어 천칭 생성 완료. Ctrl+S로 저장하세요.");
    }

    // 회전이 보이도록 막대 4개를 꽂은 기어
    private static RectTransform Gear(string n, Transform parent, Vector2 pos)
    {
        var gear = Img(n, parent, pos, V(160, 160), Gold, KnobSprite);
        for (int i = 0; i < 4; i++)
        {
            var spoke = Img($"Spoke{i}", gear.transform, V(0, 0), V(20, 190), GoldDark, UISprite);
            spoke.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 45f);
        }
        Img("Hub", gear.transform, V(0, 0), V(40, 40), SlotCol, KnobSprite);
        return gear.rectTransform;
    }
}