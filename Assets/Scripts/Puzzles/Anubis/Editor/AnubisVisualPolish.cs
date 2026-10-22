#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 아누비스 퍼즐 화면 그림 정리 메뉴
// 1) 마방진: 상징 그림을 버튼 배경이 아니라 SymbolSquarePuzzleReal의 Symbol Sprites(숫자 순서)에 넣어
//    선택판 순서가 바뀌어도(변형) 글자와 그림이 항상 같은 신이 되게 함
// 2) 지렛대/기어/모래 화면: 그림에 그림자, 발밑 그림자, 배경 색감, 축 핀을 붙여 "붕 뜬" 느낌을 줄임 (UIGroundShadow)
public static class AnubisVisualPolish
{
    // 숫자 1~9 순서 (SymbolSquarePuzzleReal.symbolNames와 같은 순서)
    private static readonly string[][] SymbolKeywords =
    {
        new[] { "scarab", "스카라브" },          // 1
        new[] { "snake", "apep", "apophis", "뱀" }, // 2
        new[] { "ibis", "thoth", "따오기" },      // 3
        new[] { "cat", "bastet", "고양이" },      // 4
        new[] { "jackal", "자칼" },               // 5 ("anubis"는 파일 이름 전체에 들어가서 제외)
        new[] { "crocodile", "croc", "sobek", "악어" }, // 6
        new[] { "lion", "sekhmet", "사자" },      // 7
        new[] { "ammut", "ammit", "암무트" },     // 8
        new[] { "falcon", "horus", "매" }         // 9
    };

    // ================= 1. 마방진 =================
    [MenuItem("Tools/Anubis/마방진: 상징 그림과 글자 맞추기")]
    public static void FixMagicSquareSymbols()
    {
        var puzzles = Object.FindObjectsByType<SymbolSquarePuzzleReal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (puzzles.Length == 0)
        {
            EditorUtility.DisplayDialog("마방진", "열린 씬에 SymbolSquarePuzzleReal이 없습니다. 아누비스 실전 씬을 열고 다시 실행하세요.", "확인");
            return;
        }

        var log = new List<string>();
        foreach (SymbolSquarePuzzleReal puzzle in puzzles)
        {
            Button[] palette = puzzle.EditorPaletteButtons;
            Button[] board = puzzle.EditorBoardButtons;

            // 지금 선택판 버튼 배경에 들어 있는 그림 (사용자가 1~9 순서로 넣어 둔 것)
            var onButtons = new Sprite[9];
            for (int i = 0; i < 9 && palette != null && i < palette.Length; i++)
            {
                var img = palette[i] != null ? palette[i].targetGraphic as Image : null;
                onButtons[i] = img != null ? img.sprite : null;
            }

            // 숫자별 그림 찾기: (1) 프로젝트의 UI_Anubis_Symbol_n_ (2) 이름 키워드 (3) 버튼에 넣은 순서
            var sprites = new Sprite[9];
            for (int n = 1; n <= 9; n++)
            {
                Sprite s = FindSpriteByPrefix($"Symbol_{n}_");
                if (s == null) s = FindByKeyword(onButtons, SymbolKeywords[n - 1]);
                if (s == null) s = FindProjectSpriteByKeyword(SymbolKeywords[n - 1]);
                if (s == null && !IsBuiltin(onButtons[n - 1])) s = onButtons[n - 1];
                sprites[n - 1] = s;
                log.Add($"{n} {SymbolKeywords[n - 1][SymbolKeywords[n - 1].Length - 1]}: {(s != null ? s.name : "없음 (글자로 표시)")}");
            }

            Undo.RecordObject(puzzle, "상징 그림");
            puzzle.EditorSetSprites(sprites);
            EditorUtility.SetDirty(puzzle);

            // 선택판 버튼 배경은 보드 칸과 같은 테두리로 되돌림 (그림은 자식 Icon에만)
            Image frame = board != null && board.Length > 0 && board[0] != null ? board[0].targetGraphic as Image : null;
            int cleared = 0;
            for (int i = 0; i < 9 && palette != null && i < palette.Length; i++)
            {
                var img = palette[i] != null ? palette[i].targetGraphic as Image : null;
                if (img == null || img.sprite == null || System.Array.IndexOf(sprites, img.sprite) < 0) continue;
                Undo.RecordObject(img, "선택판 배경");
                img.sprite = frame != null ? frame.sprite : null;
                img.color = frame != null ? frame.color : new Color(0.25f, 0.2f, 0.14f, 1f);
                img.type = frame != null ? frame.type : Image.Type.Simple;
                EditorUtility.SetDirty(img);
                cleared++;
            }
            log.Add($"{puzzle.name}: 선택판 버튼 {cleared}개의 배경 그림을 테두리로 되돌림 (그림은 Icon으로 표시)");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[마방진 그림 맞추기] 완료 (Ctrl+S로 저장)\n- " + string.Join("\n- ", log));
    }

    // ================= 2. 그림 바닥에 붙이기 =================
    [MenuItem("Tools/Anubis/퍼즐 그림 바닥에 붙이기 (그림자, 색감, 축 핀) - 자동")]
    public static void GroundPuzzleArtAuto()
    {
        string[] panels = { "Panel_Lever", "Panel_Gear", "Panel_Sand" };
        var log = new List<string>();
        foreach (string panelName in panels)
        {
            GameObject panel = FindInScene(panelName);
            if (panel == null) continue;

            foreach (Image img in panel.GetComponentsInChildren<Image>(true))
            {
                if (!TryGetMode(img, out UIGroundShadow.Mode mode)) continue;
                Apply(img.gameObject, mode);
                log.Add($"{panelName}/{img.name} -> {mode}");
            }
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log(log.Count == 0
            ? "[그림 바닥에 붙이기] 이름으로 찾은 그림이 없음 -> 그림을 선택하고 '선택한 그림' 메뉴 사용"
            : "[그림 바닥에 붙이기] 완료 (Play로 확인 후 Ctrl+S)\n- " + string.Join("\n- ", log));
    }

    [MenuItem("Tools/Anubis/선택한 그림: 매달린 물체 (그림자)")]
    private static void SelHanging() => ApplySelection(UIGroundShadow.Mode.Hanging);
    [MenuItem("Tools/Anubis/선택한 그림: 놓인 물체 (그림자 + 발밑)")]
    private static void SelStanding() => ApplySelection(UIGroundShadow.Mode.Standing);
    [MenuItem("Tools/Anubis/선택한 그림: 들어 올려지는 물체 (발밑 그림자 남음)")]
    private static void SelLifted() => ApplySelection(UIGroundShadow.Mode.Lifted);
    [MenuItem("Tools/Anubis/선택한 그림: 벽에 박힌 톱니·도르래 (그림자 + 축 핀)")]
    private static void SelMounted() => ApplySelection(UIGroundShadow.Mode.Mounted);

    private static void ApplySelection(UIGroundShadow.Mode mode)
    {
        int n = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go.GetComponent<Image>() == null) continue;
            Apply(go, mode);
            n++;
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[그림 바닥에 붙이기] 선택한 그림 {n}개에 {mode} 적용");
    }

    private static void Apply(GameObject go, UIGroundShadow.Mode mode)
    {
        var g = go.GetComponent<UIGroundShadow>();
        if (g == null) g = Undo.AddComponent<UIGroundShadow>(go);
        Undo.RecordObject(g, "그림 종류");
        g.EditorSetMode(mode);
        g.EditorSetCircle(AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")); // 빌드에도 포함되게 직접 연결
        EditorUtility.SetDirty(g);
    }

    // 이름으로 종류 판단 (버튼, 글자 배경, 이미 만든 그림자, 아주 작은 장식은 제외)
    private static bool TryGetMode(Image img, out UIGroundShadow.Mode mode)
    {
        mode = UIGroundShadow.Mode.Hanging;
        string n = img.name.ToLowerInvariant();
        if (img.GetComponent<Button>() != null || img.GetComponent<UIGroundShadow>() != null) return false;
        if (n.Contains("btn") || n.Contains("button") || n.Contains("label") || n.Contains("text") || n.Contains("shadow")
            || n.Contains("hub") || n.Contains("spoke") || n.Contains("tick") || n.Contains("pin") || n.Contains("notice")
            || n.Contains("rope") || n.Contains("background") || n.Contains("frame")) return false;
        if (img.rectTransform.rect.width < 40f) return false;

        if (n.Contains("gear") || n.Contains("pulley") || n.Contains("wheel") || n.Contains("cog")) { mode = UIGroundShadow.Mode.Mounted; return true; }
        if (n.Contains("relic") || n.Contains("sarcoph") || n.Contains("coffin")) { mode = UIGroundShadow.Mode.Lifted; return true; }
        if (n.Contains("fulcrum") || n.Contains("pillar") || n.Contains("stand") || n.Contains("jar") || n.Contains("pot")
            || n.Contains("vessel") || n.Contains("base")) { mode = UIGroundShadow.Mode.Standing; return true; }
        if (n.Contains("heart") || n.Contains("obsidian") || n.Contains("weight") || n.Contains("bar") || n.Contains("beam"))
        { mode = UIGroundShadow.Mode.Hanging; return true; }
        return false;
    }

    // ================= 도우미 =================
    private static bool IsBuiltin(Sprite s) => s == null || AssetDatabase.GetAssetPath(s).StartsWith("Resources/unity_builtin");

    private static Sprite FindByKeyword(Sprite[] list, string[] keys)
    {
        foreach (Sprite s in list)
        {
            if (IsBuiltin(s)) continue;
            string n = s.name.ToLowerInvariant();
            foreach (string k in keys) if (n.Contains(k)) return s;
        }
        return null;
    }

    private static Sprite FindSpriteByPrefix(string part)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{part} t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!System.IO.Path.GetFileName(path).Contains(part)) continue;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite s) return s;
        }
        return null;
    }

    // 프로젝트 안 "Symbol"이 들어간 그림 중 키워드가 맞는 것
    private static Sprite FindProjectSpriteByKeyword(string[] keys)
    {
        foreach (string guid in AssetDatabase.FindAssets("Symbol t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string file = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            foreach (string k in keys)
            {
                if (!file.Contains(k)) continue;
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (o is Sprite s) return s;
            }
        }
        return null;
    }

    private static GameObject FindInScene(string name)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && t.gameObject.scene.IsValid()) return t.gameObject;
        return null;
    }
}
#endif
