#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// [추가] 메뉴: Tools/Bastet/★ 비석·벽화·여신 대사 세팅 (라의 방 방식)
// 1) 벽화 그림·초상화 가져오기 설정 (Assets/image/Bastet/UI)
// 2) 비석 단서(Clue_BASTET_*)에 벽화 그림 연결
// 3) 비석(AnubisClueStele) -> BastetClueStele로 바꿈: 방 벽 횃불이 켜져야 읽힘 + 처음엔 렌즈 해독 + 수첩 메모
// 4) 대사 창(RaDialogue, 이름 "바스테트", 바스테트 초상화) + 가운데 홀 바스테트 여신상(BastetGoddess)
// 먼저 실행할 것: 실전 방 세팅(비석·CluePanel), 횃불로 방 밝히기(벽 횃불)
public static class BastetStorySetup
{
    private const string ArtFolder = "Assets/image/Bastet/UI/";
    private const string ClueFolder = "Assets/Scripts/Puzzles/Bastet/Data/Clues/";
    private const string StoryCanvasName = "BastetStoryCanvas";

    private static readonly Dictionary<string, (string art, int note, string memo)> Clues = new Dictionary<string, (string, int, string)>
    {
        { "BASTET_HALL", ("Bastet_UI_Clue_Hall", 10, "바스테트의 흔적은 넷: 발자국, 눈, 목걸이, 발톱. 모두 모이면 신상이 깨어난다.") },
        { "BASTET_B1", ("Bastet_UI_Clue_B1_Footprint", 11, "석상이 앞발을 내리치면 발자국이 빛난다. 빛난 순서 그대로 밟는다.") },
        { "BASTET_B2", ("Bastet_UI_Clue_B2_Eye", 12, "작은 석상의 눈이 빛난 순서를 기억하고, 마지막에 빛난 석상부터 거꾸로.") },
        { "BASTET_B3", ("Bastet_UI_Clue_B3_Necklace", 13, "목걸이는 가운데 선이 거울. 1↔5, 2↔6, 3↔7, 4↔8이 좌우로 마주 보게.") },
        { "BASTET_B4", ("Bastet_UI_Clue_B4_Claw", 14, "발톱 무늬: 한 줄은 좌우 대칭, 아래로 갈수록 하나씩 깊어지고 셋 다음은 하나.") },
        { "BASTET_B5", ("Bastet_UI_Clue_B5_SunMoon", 15, "낮에는 늘어나는 발자국을 따라 걷고, 밤에는 마지막에 빛난 눈부터 거꾸로.") },
    };

    [MenuItem("Tools/Bastet/★ 비석·벽화·여신 대사 세팅 (라의 방 방식)", priority = 4)]
    private static void Menu()
    {
        var log = new StringBuilder("[BastetStorySetup] 비석·벽화·여신 대사\n");
        Build(log);
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("비석·벽화·여신 대사", log.ToString().Replace("[BastetStorySetup] ", "") + "\n확인 후 Ctrl+S", "확인");
    }

    public static void Build(StringBuilder log)
    {
        ImportArt(log);
        CluePanel panel = Object.FindFirstObjectByType<CluePanel>(FindObjectsInactive.Include);
        if (panel == null) log.AppendLine("- 경고: CluePanel 없음 -> Tools/Bastet/실전 방 세팅을 먼저 실행");
        AttachMurals(log);
        ConvertSteles(panel, log);
        RaDialogue dlg = EnsureDialogue(log);
        EnsureGoddess(dlg, log);
        EditorSceneManager.MarkAllScenesDirty();
    }

    // ================= 1) 그림 =================
    private static void ImportArt(StringBuilder log)
    {
        int n = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti) || ti.textureType == TextureImporterType.Sprite) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
            n++;
        }
        log.AppendLine($"- 그림 가져오기 설정 {n}장 (Sprite, 압축 없음)");
    }

    private static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + name + ".png");

    // ================= 2) 벽화 =================
    private static void AttachMurals(StringBuilder log)
    {
        foreach (var kv in Clues)
        {
            var data = AssetDatabase.LoadAssetAtPath<ClueData>($"{ClueFolder}Clue_{kv.Key}.asset");
            Sprite art = Art(kv.Value.art);
            if (data == null || art == null) { log.AppendLine($"- {kv.Key}: 단서 또는 그림 없음 (건너뜀)"); continue; }
            var so = new SerializedObject(data);
            so.FindProperty("day.illustration").objectReferenceValue = art;
            so.FindProperty("day.illustrationTint").colorValue = Color.white;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
        log.AppendLine("- 비석 단서 6개에 벽화 그림 연결");
    }

    // ================= 3) 비석 =================
    private static void ConvertSteles(CluePanel panel, StringBuilder log)
    {
        BastetRoomArea[] rooms = Object.FindObjectsByType<BastetRoomArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int converted = 0;
        foreach (AnubisClueStele old in Object.FindObjectsByType<AnubisClueStele>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var oso = new SerializedObject(old);
            var data = oso.FindProperty("data").objectReferenceValue as ClueData;
            if (data == null || !data.ClueId.StartsWith("BASTET")) continue;
            var bubble = oso.FindProperty("newBubble").objectReferenceValue as SpriteRenderer;
            string unlock = oso.FindProperty("unlockFlag").stringValue;
            GameObject go = old.gameObject;
            Undo.DestroyObjectImmediate(old); // 꺼 두면 E 조사가 옛 비석으로 갈 수 있어서 지움
            Setup(go, data, panel, bubble, unlock, rooms);
            converted++;
        }
        // 이미 바꾼 비석도 방 횃불 다시 연결
        foreach (BastetClueStele s in Object.FindObjectsByType<BastetClueStele>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            AssignTorches(s, rooms);
        log.AppendLine($"- 비석 {converted}개를 바스테트 비석으로 바꿈 (불이 켜져야 읽힘 + 렌즈 해독 + 수첩 메모)");
    }

    private static void Setup(GameObject go, ClueData data, CluePanel panel, SpriteRenderer bubble, string unlock, BastetRoomArea[] rooms)
    {
        var s = go.GetComponent<BastetClueStele>();
        if (s == null) s = Undo.AddComponent<BastetClueStele>(go);
        int note = 0; string memo = "";
        if (Clues.TryGetValue(data.ClueId, out var info)) { note = info.note; memo = info.memo; }
        s.EditorSetup(data, panel, bubble, unlock, note, memo);
        AssignTorches(s, rooms);
        EditorUtility.SetDirty(s);
    }

    // 비석이 서 있는 방(위치로 판단)의 벽 횃불
    private static void AssignTorches(BastetClueStele s, BastetRoomArea[] rooms)
    {
        Vector3 p = s.transform.position;
        foreach (BastetRoomArea r in rooms)
        {
            Vector2 half = r.Size * 0.5f + new Vector2(0.5f, 1.5f);
            Vector3 d = p - r.transform.position;
            if (Mathf.Abs(d.x) > half.x || Mathf.Abs(d.y) > half.y) continue;
            var so = new SerializedObject(s);
            SerializedProperty arr = so.FindProperty("roomTorches");
            WallTorch[] torches = r.GetComponentsInChildren<WallTorch>(true);
            arr.arraySize = torches.Length;
            for (int i = 0; i < torches.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = torches[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return;
        }
    }

    // ================= 4) 대사 창 + 여신상 =================
    private static RaDialogue EnsureDialogue(StringBuilder log)
    {
        foreach (RaDialogue existing in Object.FindObjectsByType<RaDialogue>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Configure(existing);
            return existing;
        }
        GameObject canvasGo = GameObject.Find(StoryCanvasName);
        if (canvasGo == null)
        {
            canvasGo = new GameObject(StoryCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "대사 캔버스");
            canvasGo.layer = 5;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 65;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
        var go = new GameObject("Bastet_Dialogue", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "대사 창");
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvasGo.transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; // 화면 전체
        var dlg = go.AddComponent<RaDialogue>();
        Configure(dlg);
        log.AppendLine("- 대사 창: BastetStoryCanvas/Bastet_Dialogue (라의 대사 창, 이름 바스테트)");
        return dlg;
    }

    private static void Configure(RaDialogue dlg)
    {
        var so = new SerializedObject(dlg);
        so.FindProperty("speakerName").stringValue = "바스테트";
        so.FindProperty("portraitDefault").objectReferenceValue = Art("UI_Portrait_Bastet_Default");
        so.FindProperty("portraitGlow").objectReferenceValue = Art("UI_Portrait_Bastet_Glow");
        so.FindProperty("portraitNight").objectReferenceValue = Art("UI_Portrait_Bastet_Night");
        SetSpriteIfEmpty(so, "dialogBoxSprite", "Assets/image/ui/UI_Dialog_Box.png");
        SetSpriteIfEmpty(so, "nextSprite", "Assets/image/ui/UI_Dialog_Next.png");
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dlg);
    }

    private static void SetSpriteIfEmpty(SerializedObject so, string prop, string path)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null || p.objectReferenceValue != null) return;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o is Sprite s) { p.objectReferenceValue = s; return; }
    }

    private static void EnsureGoddess(RaDialogue dlg, StringBuilder log)
    {
        BastetRoomArea hall = null;
        foreach (BastetRoomArea r in Object.FindObjectsByType<BastetRoomArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (r.name == "Hall_Center") hall = r;
        GameObject go = BastetWalkBuilder.FindInScene("BastetGoddess");
        if (go == null)
        {
            go = new GameObject("BastetGoddess");
            Undo.RegisterCreatedObjectUndo(go, "여신상");
            if (hall != null)
            {
                go.transform.SetParent(hall.transform, false);
                go.transform.localPosition = new Vector3(-2.5f, hall.Size.y * 0.5f - 1.6f, 0f); // 북쪽 문 왼쪽, 위쪽 벽 앞
            }
        }
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(go);
        if (sr.sprite == null) sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/image/Bastet/Puzzle/Bastet_Obj_EyeCatBig.png");
        sr.sortingLayerName = "Objects";
        var col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = Undo.AddComponent<BoxCollider2D>(go);
        col.size = new Vector2(1.2f, 0.6f);
        col.offset = new Vector2(0f, 0.3f);

        Transform bt = go.transform.Find("NewBubble");
        if (bt == null)
        {
            var b = new GameObject("NewBubble");
            Undo.RegisterCreatedObjectUndo(b, "말풍선");
            b.transform.SetParent(go.transform, false);
            bt = b.transform;
        }
        bt.localPosition = new Vector3(0f, 3.4f, 0f);
        var bsr = bt.GetComponent<SpriteRenderer>();
        if (bsr == null) bsr = Undo.AddComponent<SpriteRenderer>(bt.gameObject);
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath("Assets/image/ui/UI_Prompt_New.png")) if (o is Sprite s) bsr.sprite = s;
        bsr.sortingLayerName = "Objects";
        bsr.sortingOrder = sr.sortingOrder + 2;

        var g = go.GetComponent<BastetGoddess>();
        if (g == null) g = Undo.AddComponent<BastetGoddess>(go);
        g.EditorSetup(dlg, bsr);
        EditorUtility.SetDirty(g);
        log.AppendLine("- 바스테트 여신상: 가운데 홀 북쪽 문 왼쪽 (처음 자동 소개, 서브 4개 후 안내, 클리어 후 마무리 대사)");
    }
}
#endif
