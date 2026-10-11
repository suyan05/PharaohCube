using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static AnubisUIKit;

// 실전 마방진(신들의 상징) 패널 생성 (에디터 전용)
// 메뉴: Tools > Anubis > 실전 퍼즐 > 4. 마방진 (신들의 상징) 만들기
// 기존 Panel_MagicSquare를 지우고 새로 만든 뒤, A4 제단에 다시 연결
public static class AnubisRealMagicBuilder
{
    [MenuItem("Tools/Anubis/실전 퍼즐/4. 마방진 (신들의 상징) 만들기")]
    private static void Build()
    {
        if (!Prepare()) return;
        if (!Confirm("실전 마방진", "Panel_MagicSquare를 실전 버전(숫자 대신 신들의 상징, 합계 숨김)으로 바꿉니다.")) return;

        var root = ReplacePanel("Panel_MagicSquare", "42인의 심판관", out var stations);
        var t = root.transform;
        var hint = Txt("Text_Hint", t, V(240, 365), V(1100, 70), "", 24, Sand);

        // 왼쪽: 양피지 힌트 상자
        AddHintBox(root, "Scroll_Room4", V(-560, -20), V(500, 660));

        // 가운데: 3x3 보드
        var boardBtns = new Button[9];
        var boardImgs = new Image[9];
        var boardTexts = new TextMeshProUGUI[9];
        var boardIcons = new Image[9];
        for (int i = 0; i < 9; i++)
        {
            int row = i / 3, col = i % 3;
            var cell = Img($"Slot{i}", t, V(40 + (col - 1) * 132, 150 - row * 132), V(122, 122), new Color(0.20f, 0.18f, 0.15f, 1f), UISprite, true);
            cell.type = Image.Type.Sliced;
            boardBtns[i] = cell.gameObject.AddComponent<Button>();
            boardBtns[i].targetGraphic = cell;
            boardImgs[i] = cell;

            var icon = Img("Icon", cell.transform, V(0, 0), V(96, 96), Color.white);
            icon.preserveAspect = true;
            icon.gameObject.SetActive(false);
            boardIcons[i] = icon;
            boardTexts[i] = Txt("Name", cell.transform, V(0, 0), V(116, 116), "", 26, TextCol);
        }

        // 오른쪽: 상징 선택판 3x3
        Txt("PaletteTitle", t, V(560, 250), V(380, 40), "신들의 상징", 26, Gold);
        var palBtns = new Button[9];
        var palTexts = new TextMeshProUGUI[9];
        var palIcons = new Image[9];
        for (int i = 0; i < 9; i++)
        {
            int row = i / 3, col = i % 3;
            var img = Img($"Symbol{i}", t, V(560 + (col - 1) * 124, 160 - row * 116), V(114, 104), BtnCol, UISprite, true);
            img.type = Image.Type.Sliced;
            palBtns[i] = img.gameObject.AddComponent<Button>();
            palBtns[i].targetGraphic = img;

            var icon = Img("Icon", img.transform, V(0, 0), V(80, 80), Color.white);
            icon.preserveAspect = true;
            icon.gameObject.SetActive(false);
            palIcons[i] = icon;
            palTexts[i] = Txt("Name", img.transform, V(0, 0), V(108, 98), "", 24, TextCol);
        }

        // 하단 버튼 / 피드백
        var check = Btn("Btn_Check", t, V(560, -230), V(360, 80), "심판 받기", 30, GoldDark);
        var clear = Btn("Btn_Clear", t, V(560, -320), V(360, 60), "판 비우기", 24);
        var attempts = Txt("Text_Attempts", t, V(40, -290), V(400, 40), "판정 횟수: 0", 22, Sand);
        var feedback = Txt("Text_Feedback", t, V(240, -440), V(1100, 50), "", 26, TextCol);
        var close = CloseBtn(t);
        var notice = Notice("SuccessNotice", t, V(240, 60), V(900, 200), "", out var noticeText);

        var c = root.AddComponent<SymbolSquarePuzzleReal>();
        WireArray(c, "boardButtons", boardBtns);
        WireArray(c, "boardImages", boardImgs);
        WireArray(c, "boardTexts", boardTexts);
        WireArray(c, "boardIcons", boardIcons);
        WireArray(c, "paletteButtons", palBtns);
        WireArray(c, "paletteTexts", palTexts);
        WireArray(c, "paletteIcons", palIcons);
        Wire(c, ("btnCheck", check), ("btnClear", clear), ("btnClose", close),
                ("textHint", hint), ("textFeedback", feedback), ("textAttempts", attempts),
                ("successNotice", notice), ("textSuccessNotice", noticeText));

        Relink(stations, root);
        FinishPanel(root);
        Debug.Log("[AnubisRealMagicBuilder] 실전 마방진(신들의 상징) 생성 완료. Ctrl+S로 저장하세요.");
    }
}