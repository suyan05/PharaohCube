using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// B5 "해와 달의 기억" 퍼즐 화면을 메뉴 한 번으로 만들고 연결 (에디터 전용)
// - 선택한 Canvas(없으면 씬의 첫 Canvas) 아래에 Popup_Bastet5_SunMoon 생성
// - 석상 버튼 6개, 해/달 원반, 진행 점 5개, 안내 글, 닫기/처음부터 버튼, 성공 알림
// - 데이터 에셋(BastetSunMoonData)이 없으면 만들어서 연결
// - 기존 B5(BastetMainPuzzle)의 보상 패널, 출구 막이 연결을 그대로 옮기고
//   B5 제단(Altar_Bastet5_SacredMain)이 새 퍼즐을 열도록 바꿈
public static class BastetSunMoonBuilder
{
    private const string RootName = "Popup_Bastet5_SunMoon";
    private const string AltarName = "Altar_Bastet5_SacredMain";
    private const string DataFolderParent = "Assets/Scripts/Puzzles/Bastet";
    private const string DataFolder = DataFolderParent + "/Data";
    private const string DataPath = DataFolder + "/BastetSunMoonData.asset";

    // 석상 6개: 원반 위쪽 반원 배치 (1920x1080 기준 좌표)
    private static readonly Vector2[] StatuePositions =
    {
        new Vector2(-480, -10), new Vector2(-300, 150), new Vector2(-100, 230),
        new Vector2(100, 230), new Vector2(300, 150), new Vector2(480, -10)
    };

    private static readonly Vector2 StatueSize = new Vector2(170, 200);

    [MenuItem("Tools/Bastet/B5 해와 달의 기억 UI 만들기")]
    private static void Build()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Canvas 없음", "씬에 Canvas가 없습니다. 먼저 UI > Canvas를 만들어 주세요.", "확인");
            return;
        }

        Transform old = canvas.transform.Find(RootName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("다시 만들기", $"{RootName}이 이미 있습니다. 지우고 새로 만들까요?", "새로 만들기", "취소")) return;
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        TMP_FontAsset font = FindFont();
        BastetSunMoonData data = FindOrCreateData();

        // ---------- 루트 (화면 전체를 어둡게 덮음) ----------
        RectTransform root = NewRect(RootName, canvas.transform, Vector2.zero, Vector2.zero);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Bastet B5 SunMoon");
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        // ---------- 판 ----------
        Image frame = NewImage("Frame", root, new Vector2(0, 0), new Vector2(1300, 880), Hex("#A86A5C"));
        Outline outline = frame.gameObject.AddComponent<Outline>();
        outline.effectColor = Hex("#4A3423");
        outline.effectDistance = new Vector2(5, -5);
        NewImage("FrameInner", frame.transform, Vector2.zero, new Vector2(1250, 830), Hex("#8E6A60"));

        NewText("Title", root, new Vector2(0, 385), new Vector2(900, 60), "해와 달의 기억", 44, font, Hex("#E8B23A"));

        // ---------- 해/달 원반 ----------
        Image disk = NewImage("SunMoonDisk", root, new Vector2(0, -70), new Vector2(210, 210), Hex("#E8B23A"));
        TextMeshProUGUI diskLabel = NewText("Label", disk.transform, Vector2.zero, new Vector2(210, 210), "해", 64, font, Hex("#4A3423"));

        // ---------- 석상 버튼 6개 ----------
        var buttons = new Button[StatuePositions.Length];
        var footprints = new Image[StatuePositions.Length];
        var eyes = new Image[StatuePositions.Length];
        for (int i = 0; i < StatuePositions.Length; i++)
        {
            Image statueBg = NewImage($"Statue_{i + 1}", root, StatuePositions[i], StatueSize, Hex("#5E3A36"));
            statueBg.raycastTarget = true;

            Button btn = statueBg.gameObject.AddComponent<Button>();
            btn.targetGraphic = statueBg;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.95f, 0.85f);
            cb.pressedColor = new Color(0.8f, 0.72f, 0.62f);
            cb.selectedColor = Color.white;
            cb.disabledColor = Color.white; // 보여 주는 동안 버튼이 회색으로 죽지 않게
            btn.colors = cb;

            eyes[i] = NewImage("Eye", statueBg.transform, new Vector2(0, 55), new Vector2(120, 50), Hex("#5E3A36"));
            footprints[i] = NewImage("Footprint", statueBg.transform, new Vector2(0, -40), new Vector2(96, 96), Hex("#5E3A36"));
            NewText("Number", statueBg.transform, new Vector2(0, -112), new Vector2(60, 26), (i + 1).ToString(), 18, font, Hex("#EFDCC2"));
            buttons[i] = btn;
        }

        // ---------- 진행 점 (해 라운드 + 달 1개) ----------
        int dotCount = data.SunRounds + 1;
        var dots = new Image[dotCount];
        float startX = -(dotCount - 1) * 30f;
        for (int i = 0; i < dotCount; i++)
        {
            bool moonDot = i == dotCount - 1;
            dots[i] = NewImage(moonDot ? "Dot_Moon" : $"Dot_Sun_{i + 1}", root, new Vector2(startX + i * 60f, -230),
                moonDot ? new Vector2(34, 34) : new Vector2(26, 26), Hex("#5E3A36"));
        }

        // ---------- 글자 / 버튼 ----------
        TextMeshProUGUI status = NewText("Status", root, new Vector2(0, -305), new Vector2(1150, 90), "", 30, font, Hex("#F6F1E7"));
        NewText("Hint", root, new Vector2(0, -395), new Vector2(1200, 36), "석상을 눌러 따라 하기  |  Esc : 닫기", 22, font, Hex("#EFDCC2"));

        Button btnClose = NewButton("Btn_Close", root, new Vector2(560, 385), new Vector2(150, 56), "닫기", font);
        Button btnRestart = NewButton("Btn_Restart", root, new Vector2(-540, 385), new Vector2(190, 56), "처음부터", font);

        // ---------- 성공 알림 ----------
        Image success = NewImage("SuccessNotice", root, new Vector2(0, -70), new Vector2(760, 230), Hex("#4A3423"));
        NewText("Text", success.transform, Vector2.zero, new Vector2(720, 200), "해와 달의 기억 완성\n고양이 형판 획득", 38, font, Hex("#E8B23A"));
        success.gameObject.SetActive(false);

        // ---------- 소리 ----------
        AudioSource audio = root.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;

        // ---------- 퍼즐 컴포넌트 연결 ----------
        var puzzle = root.gameObject.AddComponent<BastetSunMoonMemoryPuzzle>();
        var so = new SerializedObject(puzzle);
        so.FindProperty("data").objectReferenceValue = data;

        SerializedProperty statuesProp = so.FindProperty("statues");
        statuesProp.arraySize = buttons.Length;
        for (int i = 0; i < buttons.Length; i++)
        {
            SerializedProperty el = statuesProp.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("button").objectReferenceValue = buttons[i];
            el.FindPropertyRelative("footprint").objectReferenceValue = footprints[i];
            el.FindPropertyRelative("eye").objectReferenceValue = eyes[i];
        }

        so.FindProperty("frameImage").objectReferenceValue = frame; // [추가] 배경 그림 자리
        so.FindProperty("sunMoonDisk").objectReferenceValue = disk;
        so.FindProperty("diskLabel").objectReferenceValue = diskLabel;

        SerializedProperty dotsProp = so.FindProperty("progressDots");
        dotsProp.arraySize = dots.Length;
        for (int i = 0; i < dots.Length; i++) dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];

        so.FindProperty("puzzleRoot").objectReferenceValue = root.gameObject;
        so.FindProperty("textStatus").objectReferenceValue = status;
        so.FindProperty("btnRestart").objectReferenceValue = btnRestart;
        so.FindProperty("btnClose").objectReferenceValue = btnClose;
        so.FindProperty("successNotice").objectReferenceValue = success.gameObject;
        so.FindProperty("audioSource").objectReferenceValue = audio;

        CopyRewardsFromOldMain(so);
        so.ApplyModifiedPropertiesWithoutUndo();

        root.gameObject.SetActive(false); // 평소에는 꺼 두고 제단에서 E로 엶
        RewireAltar(puzzle, root.gameObject);
        CheckCanvasScaler(canvas);

        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log($"[BastetB5] {RootName} 생성 완료. 데이터: {AssetDatabase.GetAssetPath(data)}. 씬 저장(Ctrl+S)", root);
    }

    // 기존 B5의 보상 패널과 출구 막이 연결을 새 퍼즐로 옮김
    private static void CopyRewardsFromOldMain(SerializedObject target)
    {
        var oldMain = Object.FindFirstObjectByType<BastetMainPuzzle>(FindObjectsInactive.Include);
        if (oldMain == null) return;

        var oldSo = new SerializedObject(oldMain);
        target.FindProperty("catStencilRewardPanel").objectReferenceValue = oldSo.FindProperty("catStencilRewardPanel").objectReferenceValue;
        target.FindProperty("worldExitDoor").objectReferenceValue = oldSo.FindProperty("worldExitDoor").objectReferenceValue;
        Debug.Log($"[BastetB5] 기존 {oldMain.name}의 보상 패널, 출구 막이 연결을 옮김", oldMain);
    }

    // B5 제단이 새 퍼즐을 열도록 연결
    private static void RewireAltar(BastetSunMoonMemoryPuzzle puzzle, GameObject panel)
    {
        GameObject altar = GameObject.Find(AltarName);
        if (altar == null)
        {
            Debug.LogWarning($"[BastetB5] '{AltarName}'이 없어 제단 연결을 건너뜀 -> 제단의 BastetAltarInteractable에 직접 연결");
            return;
        }

        var interact = altar.GetComponent<BastetAltarInteractable>();
        if (interact == null)
        {
            Debug.LogWarning($"[BastetB5] {AltarName}에 BastetAltarInteractable이 없음", altar);
            return;
        }

        Undo.RecordObject(interact, "Rewire Bastet B5 Altar");
        var so = new SerializedObject(interact);
        so.FindProperty("promptText").stringValue = "바스테트 신상 조사";
        so.FindProperty("targetPuzzle").objectReferenceValue = puzzle;
        so.FindProperty("puzzleUIPanel").objectReferenceValue = panel;
        so.ApplyModifiedProperties();
        Debug.Log($"[BastetB5] {AltarName}이 새 퍼즐을 열도록 연결함", altar);
    }

    private static void CheckCanvasScaler(Canvas canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            Debug.LogWarning("[BastetB5] Canvas Scaler를 Scale With Screen Size, 1920 x 1080, Match 0.5로 두는 것을 추천", canvas);
    }

    // ================= 찾기 / 만들기 도우미 =================
    private static Canvas FindCanvas()
    {
        if (Selection.activeGameObject != null)
        {
            Canvas selected = Selection.activeGameObject.GetComponentInParent<Canvas>();
            if (selected != null) return selected.rootCanvas;
        }
        return Object.FindFirstObjectByType<Canvas>();
    }

    private static TMP_FontAsset FindFont()
    {
        string[] guids = AssetDatabase.FindAssets("MonoplexKR t:TMP_FontAsset");
        if (guids.Length > 0) return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        Debug.LogWarning("[BastetB5] MonoplexKR 폰트를 못 찾아 TMP 기본 폰트 사용 (한글이 네모로 나오면 폰트 교체)");
        return TMP_Settings.defaultFontAsset;
    }

    private static BastetSunMoonData FindOrCreateData()
    {
        string[] guids = AssetDatabase.FindAssets("t:BastetSunMoonData");
        if (guids.Length > 0) return AssetDatabase.LoadAssetAtPath<BastetSunMoonData>(AssetDatabase.GUIDToAssetPath(guids[0]));

        if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder(DataFolderParent, "Data");
        var data = ScriptableObject.CreateInstance<BastetSunMoonData>();
        AssetDatabase.CreateAsset(data, DataPath);
        AssetDatabase.SaveAssets();
        return data;
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; // UI
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static Image NewImage(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        var img = NewRect(name, parent, pos, size).gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, Vector2 pos, Vector2 size,
        string text, float fontSize, TMP_FontAsset font, Color color)
    {
        var t = NewRect(name, parent, pos, size).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    private static Button NewButton(string name, Transform parent, Vector2 pos, Vector2 size, string label, TMP_FontAsset font)
    {
        Image bg = NewImage(name, parent, pos, size, Hex("#4A3423"));
        bg.raycastTarget = true;
        var btn = bg.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        NewText("Label", bg.transform, Vector2.zero, size, label, 26, font, Hex("#F6F1E7"));
        return btn;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
