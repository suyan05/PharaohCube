using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 아누비스 방 퍼즐 UI 자동 생성 툴 (에디터 전용, 반드시 Editor 폴더에 둘 것)
// 메뉴: Tools > Anubis > Puzzle UI Builder
// 1) Canvas_AnubisOverlay 아래에 퍼즐 패널 5개 + 사자의 서 UI 생성
// 2) 퍼즐 스크립트의 [SerializeField] 필드를 이름으로 찾아 자동 연결
// 3) 제단 오브젝트에 BoxCollider2D + AnubisAltarStation 부착 및 패널 연결

public class AnubisPuzzleUIBuilder : EditorWindow
{
    private const string CanvasName = "Canvas_AnubisOverlay";
    private const int UILayer = 5;

    private static readonly string[] PanelNames = { "Panel_Sand", "Panel_Gear", "Panel_Lever", "Panel_MagicSquare", "Panel_MainScale" };
    private static readonly string[] PromptNames = { "모래 분배기", "도르래 천칭", "지렛대 천칭", "42인 마방진", "심판 저울" };

    private static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.85f);
    private static readonly Color FrameCol = new Color(0.16f, 0.12f, 0.09f, 0.97f);
    private static readonly Color Gold = new Color(0.91f, 0.70f, 0.23f, 1f);
    private static readonly Color GoldDark = new Color(0.62f, 0.45f, 0.14f, 1f);
    private static readonly Color Sand = new Color(0.91f, 0.85f, 0.72f, 1f);
    private static readonly Color BtnCol = new Color(0.35f, 0.25f, 0.15f, 1f);
    private static readonly Color SlotCol = new Color(0.10f, 0.08f, 0.06f, 1f);
    private static readonly Color TextCol = new Color(0.96f, 0.93f, 0.86f, 1f);
    private static readonly Color InkCol = new Color(0.25f, 0.17f, 0.08f, 1f);
    private static readonly Color NoticeBg = new Color(0.22f, 0.16f, 0.07f, 0.98f);

    [SerializeField] private GameObject canvasRoot;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private GameObject[] altars = new GameObject[5];

    private Transform canvasT;
    private int undoGroup;
    private Vector2 scroll;

    private static Sprite uiSprite;
    private static Sprite knobSprite; 
    private static TMP_FontAsset activeFont;

    [MenuItem("Tools/Anubis/Puzzle UI Builder")]
    private static void Open() => GetWindow<AnubisPuzzleUIBuilder>("Anubis UI Builder");

    private void OnGUI()
    {
        if (altars == null || altars.Length != 5) altars = new GameObject[5];
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("1. 기본 설정", EditorStyles.boldLabel);
        canvasRoot = (GameObject)EditorGUILayout.ObjectField("Overlay Canvas", canvasRoot, typeof(GameObject), true);
        font = (TMP_FontAsset)EditorGUILayout.ObjectField("한글 TMP 폰트", font, typeof(TMP_FontAsset), false);
        if (font == null)
            EditorGUILayout.HelpBox("한글 폰트를 비우면 TMP 기본 폰트를 씁니다. 한글이 □로 보이면 Fonts 폴더의 한글 SDF 에셋을 넣으세요.", MessageType.Warning);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("2. 퍼즐 UI 생성", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("같은 이름의 패널은 지우고 다시 만듭니다. 위치를 손으로 바꾼 패널은 다시 생성하지 마세요. Ctrl+Z로 되돌릴 수 있습니다.", MessageType.Info);

        if (GUILayout.Button("전체 생성 (퍼즐 5개 + 사자의 서)", GUILayout.Height(34)))
            Run(() => { BuildSand(); BuildGear(); BuildLever(); BuildMagicSquare(); BuildMainScale(); BuildNotebook(); });

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("모래")) Run(BuildSand);
            if (GUILayout.Button("기어")) Run(BuildGear);
            if (GUILayout.Button("지렛대")) Run(BuildLever);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("마방진")) Run(BuildMagicSquare);
            if (GUILayout.Button("저울")) Run(BuildMainScale);
            if (GUILayout.Button("사자의 서")) Run(BuildNotebook);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("3. 제단 연결 (Hierarchy에서 드래그)", EditorStyles.boldLabel);
        for (int i = 0; i < 5; i++)
            altars[i] = (GameObject)EditorGUILayout.ObjectField($"{PromptNames[i]} 제단", altars[i], typeof(GameObject), true);

        if (GUILayout.Button("제단 연결 (BoxCollider2D + AnubisAltarStation)", GUILayout.Height(30)))
            Run(ConnectAltars);

        EditorGUILayout.EndScrollView();
    }

    private void Run(System.Action build)
    {
        if (!Prepare()) return;
        build();
        Finish();
    }

    private bool Prepare()
    {
        undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Anubis UI Builder");

        if (canvasRoot == null) canvasRoot = GameObject.Find(CanvasName);
        if (canvasRoot == null)
        {
            canvasRoot = new GameObject(CanvasName, typeof(RectTransform));
            canvasRoot.layer = UILayer;
            Undo.RegisterCreatedObjectUndo(canvasRoot, "Create Canvas");
        }

        var canvas = canvasRoot.GetComponent<Canvas>();
        if (canvas == null) canvas = Undo.AddComponent<Canvas>(canvasRoot);
        Undo.RecordObject(canvas, "Canvas Setup");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = canvasRoot.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvasRoot);
        Undo.RecordObject(scaler, "Scaler Setup");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasRoot.GetComponent<GraphicRaycaster>() == null) Undo.AddComponent<GraphicRaycaster>(canvasRoot);

        if (Object.FindFirstObjectByType<EventSystem>() == null)
            Debug.LogWarning("[AnubisUIBuilder] 씬에 EventSystem이 없습니다. GameObject > UI > Event System을 추가하세요.");

        canvasT = canvasRoot.transform;
        uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        activeFont = font != null ? font : TMP_Settings.defaultFontAsset;
        return true;
    }

    private void Finish()
    {
        var nb = canvasT.Find("Panel_Notebook");
        if (nb != null) nb.SetAsLastSibling();
        var hud = canvasT.Find("Btn_Notebook");
        if (hud != null) hud.SetAsFirstSibling();

        EditorSceneManager.MarkSceneDirty(canvasRoot.scene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("[AnubisUIBuilder] 완료. Ctrl+S로 씬을 저장하세요.");
    }

    private void BuildSand()
    {
        var root = CreatePanelRoot(PanelNames[0], "사자의 모래 분배기");
        var t = root.transform;
        Txt("Desc", t, V(0, 370), V(1500, 50), "옮길 단지를 고른 뒤 부을 단지를 고르십시오.   목표: 8L 단지 4L + 5L 단지 4L", 26, Sand);

        string[] ids = { "8L", "5L", "3L" };
        float[] xs = { -420f, 0f, 420f };
        float[] hs = { 340f, 260f, 200f };

        var buttons = new Button[3];
        var fills = new Image[3];
        var highlights = new GameObject[3];
        var texts = new TextMeshProUGUI[3];

        for (int i = 0; i < 3; i++)
        {
            var size = V(220, hs[i]);
            var pos = V(xs[i], -200f + hs[i] * 0.5f);

            var hl = Img($"Highlight{ids[i]}", t, pos, size + V(24, 24), Gold, uiSprite);
            hl.type = Image.Type.Sliced;
            hl.gameObject.SetActive(false);
            highlights[i] = hl.gameObject;

            var jar = Img($"Jar{ids[i]}", t, pos, size, SlotCol, uiSprite, true);
            jar.type = Image.Type.Sliced;
            buttons[i] = jar.gameObject.AddComponent<Button>();
            buttons[i].targetGraphic = jar;

            var fill = Img("SandFill", jar.transform, V(0, 0), size - V(24, 24), new Color(0.86f, 0.72f, 0.42f, 1f), uiSprite);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.fillAmount = i == 0 ? 1f : 0f;
            fills[i] = fill;

            Txt($"Name{ids[i]}", t, V(xs[i], -235), V(260, 40), $"{ids[i]} 단지", 26, Sand);
            texts[i] = Txt($"Text{ids[i]}", t, V(xs[i], -285), V(260, 50), "", 32, TextCol);
        }

        var reset = Btn("Btn_Reset", t, V(0, -400), V(280, 70), "처음부터 다시", 26);
        var close = CloseBtn(t);
        var notice = Notice("SuccessNotice", t, V(0, 250), V(900, 130), "", out var noticeText);

        var c = root.AddComponent<LionSandDecanter>();
        Wire(c,
            ("btnJar8L", buttons[0]), ("btnJar5L", buttons[1]), ("btnJar3L", buttons[2]),
            ("fillSand8L", fills[0]), ("fillSand5L", fills[1]), ("fillSand3L", fills[2]),
            ("highlight8L", highlights[0]), ("highlight5L", highlights[1]), ("highlight3L", highlights[2]),
            ("textJar8L", texts[0]), ("textJar5L", texts[1]), ("textJar3L", texts[2]),
            ("btnReset", reset), ("btnClose", close),
            ("successNotice", notice), ("textSuccessNotice", noticeText));

        FinishPanel(root);
    }

    private void BuildGear()
    {
        var root = CreatePanelRoot(PanelNames[1], "도르래와 황금 기어비 천칭");
        var t = root.transform;
        Txt("Desc", t, V(0, 370), V(1500, 50), "기어 톱니 수와 도르래를 조합해 1kg 추로 4kg 유물을 들어 올리십시오.", 26, Sand);

        // 기어 2개
        var driver = Gear("DriverGear", t, V(-520, 120));
        var driven = Gear("DrivenGear", t, V(-200, 120));
        var txtDriver = Txt("Text_DriverGear", t, V(-520, -90), V(280, 90), "", 28, TextCol);
        var txtDriven = Txt("Text_DrivenGear", t, V(-200, -90), V(280, 90), "", 28, TextCol);
        var btnDriver = Btn("Btn_DriverCycle", t, V(-520, -180), V(260, 64), "구동 기어 교체", 24);
        var btnDriven = Btn("Btn_DrivenCycle", t, V(-200, -180), V(260, 64), "종동 기어 교체", 24);

        Img("Rope", t, V(380, 145), V(6, 290), Sand);
        var pulley = Img("PulleyIndicator", t, V(380, 290), V(120, 120), new Color(0.6f, 0.6f, 0.6f, 1f), knobSprite);
        var plate = Img("RelicPlate", t, V(380, 0), V(200, 90), new Color(0.45f, 0.35f, 0.20f, 1f), uiSprite);
        plate.type = Image.Type.Sliced;
        Txt("Label", plate.transform, V(0, 0), V(190, 80), "유물 4kg", 26, TextCol);
        var txtPulley = Txt("Text_PulleyMode", t, V(630, 290), V(260, 100), "", 26, TextCol);
        var btnPulley = Btn("Btn_PulleyToggle", t, V(380, -180), V(260, 64), "도르래 전환", 24);

        var txtRatio = Txt("Text_RatioCalc", t, V(0, -290), V(1500, 90), "", 28, TextCol);
        var txtResult = Txt("Text_ResultNotice", t, V(0, -370), V(1500, 50), "", 26, TextCol);
        var btnCrank = Btn("Btn_OperateCrank", t, V(0, -445), V(320, 76), "크랭크 작동", 30, GoldDark);
        var close = CloseBtn(t);
        var notice = Notice("SuccessNotice", t, V(0, 60), V(900, 200), "봉인 해제!\n사자의 서에 두 번째 비문이 기록되었습니다.", out _);

        var c = root.AddComponent<GearPulleyPuzzle>();
        Wire(c,
            ("driverGearTransform", driver), ("drivenGearTransform", driven),
            ("relicPlateTransform", plate.rectTransform), ("imgPulleyIndicator", pulley),
            ("textDriverGear", txtDriver), ("textDrivenGear", txtDriven), ("textPulleyMode", txtPulley),
            ("textRatioCalc", txtRatio), ("textResultNotice", txtResult),
            ("btnDriverCycle", btnDriver), ("btnDrivenCycle", btnDriven), ("btnPulleyToggle", btnPulley),
            ("btnOperateCrank", btnCrank), ("btnClose", close), ("successNotice", notice));

        FinishPanel(root);
    }

    private void BuildLever()
    {
        var root = CreatePanelRoot(PanelNames[2], "아누비스의 지렛대 천칭");
        var t = root.transform;
        var hint = Txt("Text_Hint", t, V(0, 355), V(1400, 80), "", 26, Sand);

        Img("Fulcrum", t, V(0, -60), V(70, 150), new Color(0.30f, 0.22f, 0.12f, 1f), uiSprite);
        var bar = Img("LeverBar", t, V(0, 30), V(760, 22), Gold, uiSprite).rectTransform;

        for (int i = 1; i <= 4; i++)
            Txt($"Tick{i}", bar, V(75 * i, 32), V(60, 36), i.ToString(), 24, Sand);
        Txt("TickLeft", bar, V(-150, 32), V(60, 36), "2", 24, Sand);

        Img("LeftRope", bar, V(-150, -55), V(4, 110), Sand);
        var heart = Img("Heart", bar, V(-150, -140), V(100, 100), new Color(0.75f, 0.12f, 0.15f, 1f), knobSprite);
        Txt("Label", heart.transform, V(0, 0), V(100, 70), "심장\n4kg", 22, TextCol);

        var hook = Img("RightHook", bar, V(75, 0), V(24, 24), Sand, knobSprite).rectTransform;
        Img("Rope", hook, V(0, -55), V(4, 110), Sand);
        var weights = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            var w = Img($"Obsidian{i + 1}", hook, V(0, -135 - i * 58), V(80, 52), new Color(0.15f, 0.12f, 0.20f, 1f), uiSprite);
            Txt("Label", w.transform, V(0, 0), V(80, 50), "2kg", 22, TextCol);
            weights[i] = w.gameObject;
        }

        // 조작부
        var txtDist = Txt("Text_Distance", t, V(-420, -300), V(300, 80), "", 28, TextCol);
        var distMinus = Btn("Btn_DistMinus", t, V(-500, -390), V(90, 64), "-", 36);
        var distPlus = Btn("Btn_DistPlus", t, V(-340, -390), V(90, 64), "+", 36);
        var txtCount = Txt("Text_WeightCount", t, V(0, -300), V(300, 80), "", 28, TextCol);
        var countMinus = Btn("Btn_CountMinus", t, V(-80, -390), V(90, 64), "-", 36);
        var countPlus = Btn("Btn_CountPlus", t, V(80, -390), V(90, 64), "+", 36);
        var check = Btn("Btn_CheckBalance", t, V(440, -345), V(300, 90), "평형 확인", 30, GoldDark);
        var feedback = Txt("Text_Feedback", t, V(0, -460), V(1500, 50), "", 26, TextCol);
        var close = CloseBtn(t);
        var notice = Notice("SuccessNotice", t, V(0, 60), V(900, 200), "평형 달성!\n사자의 서에 세 번째 비문이 기록되었습니다.", out _);

        var c = root.AddComponent<AnubisLeverPuzzle>();
        Wire(c,
            ("leverBar", bar), ("rightHookAnchor", hook),
            ("textDistanceDisplay", txtDist), ("textWeightCountDisplay", txtCount),
            ("textHintNotice", hint), ("textFeedbackNotice", feedback),
            ("btnDistMinus", distMinus), ("btnDistPlus", distPlus),
            ("btnCountMinus", countMinus), ("btnCountPlus", countPlus),
            ("btnCheckBalance", check), ("btnClose", close), ("successNotice", notice));
        WireArray(c, "obsidianWeights", weights);

        FinishPanel(root);
    }

    private void BuildMagicSquare()
    {
        var root = CreatePanelRoot(PanelNames[3], "42인의 제약 마방진");
        var t = root.transform;
        var hint = Txt("Text_Hint", t, V(0, 345), V(1400, 100), "", 24, Sand);

        var slotButtons = new Button[9];
        var slotTexts = new TextMeshProUGUI[9];
        var slotImages = new Image[9];
        for (int i = 0; i < 9; i++)
        {
            int row = i / 3, col = i % 3;
            var cell = Img($"Slot{i}", t, V(-300 + (col - 1) * 140, 120 - row * 140), V(128, 128), SlotCol, uiSprite, true);
            cell.type = Image.Type.Sliced;
            slotButtons[i] = cell.gameObject.AddComponent<Button>();
            slotButtons[i].targetGraphic = cell;
            slotImages[i] = cell;
            slotTexts[i] = Txt("Value", cell.transform, V(0, 0), V(120, 120), "", 56, TextCol);
        }

        var sums = new TextMeshProUGUI[8];
        for (int r = 0; r < 3; r++) sums[r] = Txt($"Sum_Row{r}", t, V(-70, 120 - r * 140), V(80, 50), "0", 30, Sand);
        for (int cIdx = 0; cIdx < 3; cIdx++) sums[3 + cIdx] = Txt($"Sum_Col{cIdx}", t, V(-300 + (cIdx - 1) * 140, -270), V(80, 50), "0", 30, Sand);
        sums[6] = Txt("Sum_Diag0", t, V(-70, -270), V(80, 50), "0", 30, Sand);
        sums[7] = Txt("Sum_Diag1", t, V(-530, -270), V(80, 50), "0", 30, Sand);

        var numButtons = new Button[9];
        for (int i = 0; i < 9; i++)
        {
            int row = i / 3, col = i % 3;
            numButtons[i] = Btn($"Num{i + 1}", t, V(420 + (col - 1) * 120, 120 - row * 120), V(104, 104), (i + 1).ToString(), 44);
        }

        var check = Btn("Btn_CheckSolution", t, V(420, -250), V(360, 80), "봉인 판정", 30, GoldDark);
        var clear = Btn("Btn_ClearBoard", t, V(420, -345), V(360, 64), "보드 비우기", 24);
        var feedback = Txt("Text_Feedback", t, V(0, -445), V(1500, 50), "", 26, TextCol);
        var close = CloseBtn(t);
        var notice = Notice("SuccessNotice", t, V(0, 60), V(900, 200), "마방진 완성!\n사자의 서에 네 번째 비문이 기록되었습니다.", out _);

        var c = root.AddComponent<MaatMagicSquarePuzzle>();
        WireArray(c, "boardSlotButtons", slotButtons);
        WireArray(c, "boardSlotTexts", slotTexts);
        WireArray(c, "boardSlotHighlights", slotImages);
        WireArray(c, "numPadButtons", numButtons);
        WireArray(c, "lineSumTexts", sums);
        Wire(c,
            ("btnCheckSolution", check), ("btnClearBoard", clear), ("btnClose", close),
            ("successNotice", notice), ("textFeedbackNotice", feedback), ("textHintNotice", hint));

        FinishPanel(root);
    }

    private void BuildMainScale()
    {
        var root = CreatePanelRoot(PanelNames[4], "아누비스의 심판");
        var t = root.transform;
        var weightText = Txt("Text_Weight", t, V(0, 370), V(1200, 56), "", 34, Gold);
        Txt("Desc", t, V(0, 318), V(1500, 40), "유물을 왼쪽 접시에 끌어다 놓고 심판 레버를 당기십시오. 기회는 단 한 번입니다.", 24, Sand);

        // 기둥 + 받침 + 저울대
        Img("Pillar", t, V(0, -40), V(44, 380), new Color(0.20f, 0.16f, 0.10f, 1f), uiSprite);
        Img("Base", t, V(0, -225), V(280, 40), Gold, uiSprite);
        var beam = Img("ScaleBeam", t, V(0, 150), V(640, 18), Gold, uiSprite).rectTransform;
        Img("Pivot", beam, V(0, 0), V(44, 44), GoldDark, knobSprite);

        // 접시 2개 (저울대 자식 → 함께 기울어짐)
        var leftPan = Pan("LeftPan", beam, -300f);
        var rightPan = Pan("RightPan", beam, 300f);
        var feather = Img("Feather", rightPan.transform, V(0, 45), V(130, 50), new Color(0.95f, 0.95f, 0.95f, 1f), uiSprite);
        Txt("Label", feather.transform, V(0, 0), V(130, 50), "깃털 7kg", 22, InkCol);

        // 유물 받침대 + 유물 5개
        Img("RelicTray", t, V(0, -350), V(1150, 190), SlotCol, uiSprite).type = Image.Type.Sliced;
        var relics = new (string id, string label, float w, Color c, bool heart, bool gold)[]
        {
            ("Heart",      "망자의 심장", 4f, new Color(0.75f, 0.12f, 0.15f, 1f), true,  false),
            ("GoldJar",    "황금 단지",   3f, Gold,                               false, true),
            ("ClayPot",    "흙의 단지",   1f, new Color(0.47f, 0.33f, 0.24f, 1f), false, false),
            ("Obsidian",   "흑요석 추",   2f, new Color(0.15f, 0.12f, 0.20f, 1f), false, false),
            ("BronzeBowl", "청동 그릇",   5f, new Color(0.69f, 0.47f, 0.26f, 1f), false, false),
        };
        for (int i = 0; i < relics.Length; i++)
        {
            var r = relics[i];
            var img = Img($"Relic_{r.id}", t, V(-400 + i * 200, -330), V(110, 110), r.c, knobSprite, true);
            var drag = img.gameObject.AddComponent<AnubisUIDragItem>();
            drag.itemName = r.label;
            drag.weightValue = r.w;
            drag.isHeart = r.heart;
            drag.isGold = r.gold;
            Txt("Name", img.transform, V(0, -78), V(200, 40), r.label, 22, TextCol);
        }

        var judge = Btn("Btn_JudgeLever", t, V(620, -120), V(280, 90), "심판 레버 당기기", 26, GoldDark);
        var close = CloseBtn(t);

        var clearPanel = Notice("ClearText", t, V(0, 80), V(1100, 240), "", out _);

        var ammit = Img("AmmitPenaltyPanel", t, V(0, 60), V(1100, 400), new Color(0.60f, 0.08f, 0.08f, 0.97f), uiSprite, true);
        ammit.type = Image.Type.Sliced;
        Txt("Text", ammit.transform, V(0, 50), V(1040, 260), "", 30, Color.black);
        var retry = Btn("Btn_Retry", ammit.transform, V(0, -140), V(260, 70), "다시 도전", 26);
        retry.gameObject.SetActive(false);
        ammit.gameObject.SetActive(false);

        var c = root.AddComponent<AnubisPuzzleManager2D>();
        Wire(c,
            ("scaleBeam", beam),
            ("leftPan", leftPan.GetComponent<AnubisUIScalePan>()),
            ("rightPan", rightPan.GetComponent<AnubisUIScalePan>()),
            ("btnJudgeLever", judge), ("ammitPenaltyPanel", ammit.gameObject),
            ("weightText", weightText), ("clearText", clearPanel), ("btnRetry", retry));

        FinishPanel(root);
    }

    private void BuildNotebook()
    {
        var managers = GameObject.Find("Managers");
        if (managers == null)
        {
            managers = new GameObject("Managers");
            Undo.RegisterCreatedObjectUndo(managers, "Create Managers");
        }
        var nb = managers.GetComponent<AnubisNotebookManager>();
        if (nb == null) nb = Undo.AddComponent<AnubisNotebookManager>(managers);

        if (Object.FindFirstObjectByType<ItemInventory>() == null) Undo.AddComponent<ItemInventory>(managers);

        foreach (var n in new[] { "Panel_Notebook", "Btn_Notebook" })
        {
            var old = canvasT.Find(n);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        }

        var toggle = Btn("Btn_Notebook", canvasT, V(0, 0), V(240, 70), "사자의 서 [Tab]", 24);
        var trt = (RectTransform)toggle.transform;
        trt.anchorMin = V(1, 0);
        trt.anchorMax = V(1, 0);
        trt.anchoredPosition = V(-150, 70);
        Undo.RegisterCreatedObjectUndo(toggle.gameObject, "Create Btn_Notebook");

        var popup = Img("Panel_Notebook", canvasT, V(0, 0), V(1100, 880), new Color(0.85f, 0.76f, 0.58f, 0.98f), uiSprite, true);
        popup.type = Image.Type.Sliced;
        var p = popup.transform;
        Txt("Title", p, V(0, 390), V(900, 60), "사자의 서", 44, InkCol);
        Txt("Sub", p, V(0, 340), V(900, 34), "[Tab] / [M] 키로 열고 닫기", 22, InkCol);
        var close = Btn("Btn_Close", p, V(460, 395), V(130, 56), "닫기", 22);

        var clues = new TextMeshProUGUI[7];
        for (int i = 0; i < 7; i++)
            clues[i] = Txt($"Clue{i + 1}", p, V(0, 260 - i * 95), V(1000, 88), $"[비문 {i + 1}] 아직 해독되지 않았습니다.", 22, InkCol, TextAlignmentOptions.TopLeft);

        popup.gameObject.SetActive(false);
        Undo.RegisterCreatedObjectUndo(popup.gameObject, "Create Panel_Notebook");

        Wire(nb, ("notebookPopup", popup.gameObject), ("btnToggleNotebook", toggle), ("btnCloseNotebook", close));
        WireArray(nb, "clueSlotTexts", clues);
    }

    private void ConnectAltars()
    {
        for (int i = 0; i < 5; i++)
        {
            var altar = altars[i];
            if (altar == null)
            {
                Debug.LogWarning($"[AnubisUIBuilder] {PromptNames[i]} 제단이 비어 있어 건너뜀");
                continue;
            }

            var panel = canvasT.Find(PanelNames[i]);
            if (panel == null)
            {
                Debug.LogError($"[AnubisUIBuilder] {PanelNames[i]}가 없습니다. 먼저 UI를 생성하세요.");
                continue;
            }

            if (altar.GetComponent<Collider2D>() == null) Undo.AddComponent<BoxCollider2D>(altar);

            var station = altar.GetComponent<AnubisAltarStation>();
            if (station == null) station = Undo.AddComponent<AnubisAltarStation>(altar);

            var so = new SerializedObject(station);
            so.FindProperty("overlayPanel").objectReferenceValue = panel.gameObject;
            so.FindProperty("promptName").stringValue = PromptNames[i];
            so.ApplyModifiedProperties();

            Debug.Log($"[AnubisUIBuilder] {altar.name} ← {PanelNames[i]} 연결 완료", altar);
        }
    }

    private static Vector2 V(float x, float y) => new Vector2(x, y);

    private GameObject CreatePanelRoot(string panelName, string title)
    {
        var old = canvasT.Find(panelName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var go = new GameObject(panelName, typeof(RectTransform));
        go.layer = UILayer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvasT, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = go.AddComponent<Image>();
        bg.color = Backdrop;
        bg.raycastTarget = true; 

        Img("Frame", rt, V(0, 0), V(1720, 1000), FrameCol, uiSprite).type = Image.Type.Sliced;
        Txt("Title", rt, V(0, 440), V(1300, 70), title, 48, Gold);
        return go;
    }

    private static void FinishPanel(GameObject root)
    {
        root.SetActive(false);
        Undo.RegisterCreatedObjectUndo(root, "Build " + root.name);
    }

    private static RectTransform NewRect(string n, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.layer = UILayer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static Image Img(string n, Transform parent, Vector2 pos, Vector2 size, Color color, Sprite sprite = null, bool raycast = false)
    {
        var img = NewRect(n, parent, pos, size).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    private static TextMeshProUGUI Txt(string n, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var tmp = NewRect(n, parent, pos, size).gameObject.AddComponent<TextMeshProUGUI>();
        if (activeFont != null) tmp.font = activeFont;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button Btn(string n, Transform parent, Vector2 pos, Vector2 size, string label, float fontSize = 28, Color? bg = null)
    {
        var img = Img(n, parent, pos, size, bg ?? BtnCol, uiSprite, true);
        img.type = Image.Type.Sliced;
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        Txt("Label", img.transform, V(0, 0), size - V(10, 6), label, fontSize, TextCol);
        return b;
    }

    private static Button CloseBtn(Transform parent) => Btn("Btn_Close", parent, V(760, 440), V(180, 64), "닫기 [Esc]", 24);

    private static GameObject Notice(string n, Transform parent, Vector2 pos, Vector2 size, string text, out TextMeshProUGUI tmp)
    {
        var img = Img(n, parent, pos, size, NoticeBg, uiSprite);
        img.type = Image.Type.Sliced;
        tmp = Txt("Text", img.transform, V(0, 0), size - V(40, 30), text, 32, Gold);
        img.gameObject.SetActive(false);
        return img.gameObject;
    }

    private static RectTransform Gear(string n, Transform parent, Vector2 pos)
    {
        var gear = Img(n, parent, pos, V(200, 200), Gold, knobSprite);
        for (int i = 0; i < 4; i++)
        {
            var spoke = Img($"Spoke{i}", gear.transform, V(0, 0), V(24, 236), GoldDark, uiSprite);
            spoke.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 45f);
        }
        Img("Hub", gear.transform, V(0, 0), V(50, 50), SlotCol, knobSprite);
        return gear.rectTransform;
    }

    private static RectTransform Pan(string n, RectTransform beam, float x)
    {
        Img(n + "_Chain", beam, V(x, -80), V(4, 160), Sand);
        var pan = Img(n, beam, V(x, -170), V(200, 28), Gold, uiSprite, true);
        pan.gameObject.AddComponent<AnubisUIScalePan>();
        return pan.rectTransform;
    }

    private static void Wire(Component target, params (string field, Object value)[] pairs)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in pairs)
        {
            var p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[AnubisUIBuilder] {target.GetType().Name}에 '{field}' 필드가 없습니다. 스크립트가 최신 코드인지 확인하세요.", target);
                continue;
            }
            p.objectReferenceValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireArray(Component target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null || !p.isArray)
        {
            Debug.LogError($"[AnubisUIBuilder] {target.GetType().Name}에 배열 '{field}'가 없습니다.", target);
            return;
        }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}