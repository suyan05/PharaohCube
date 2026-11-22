#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// [추가] 메뉴: Tools/Bastet/아트 팀 그림으로 교체 (오브젝트·초상·단서·검은 고양이)
// 프로그램 담당이 AI로 만든 임시 그림을 아트 팀이 올린 그림으로 바꿈
//  - 하연 님 (khy1004): image/Bastet/Bastet_Obj (석상·발판·상자·봉인석·신상·비석), image/Bastet/Bastet_UI (초상·단서 벽화)
//  - 예림 님 (Kangyelim1): image/A/A-02 검은 고양이 -> image/Bastet/Story/Bastet_Story_Cat_Rest, _Walk_0/1, _Eyes 로 복사해 둔 그림
// 하는 일
//  1) 가져오기 설정: 월드 그림 = Single, 1칸 32px(PPU 32), Point, 바닥 기준 / 눈 빛 = 눈 위치 기준
//  2) 열린 씬 + 단서 데이터(ClueData)에서 AI 그림을 가리키는 칸을 모두 아트 그림으로 바꿈
//  3) 그림 크기가 달라진 오브젝트는 바닥 위치를 맞추고, 눈 빛 위치를 다시 맞춤
//  4) 상태 그림 연결: B1 석상 앞발 들기/내리기, B2 눈 석상 입 벌림, B3/B4 상자 열림, 고양이 걷기
// AI 그림 파일은 지우지 않음 (게임에서 확인한 뒤 정리). 여러 번 눌러도 안전
// B3 목걸이 / B4 발톱 팝업은 판 배치가 달라서 이 메뉴에서 바꾸지 않음 (다음 단계)
public static class BastetTeamArtSwap
{
    private const string AiW = "Assets/image/Bastet/Puzzle/";
    private const string AiU = "Assets/image/Bastet/UI/";
    private const string AiS = "Assets/image/Bastet/Story/";
    private const string TeamW = "Assets/image/Bastet/Bastet_Obj/";
    private const string TeamU = "Assets/image/Bastet/Bastet_UI/";
    private const float WorldPpu = 32f;   // 1칸 = 32px
    private const float CatPpu = 256f;    // 고양이 320px 그림 -> 약 1.25칸

    // AI 그림 -> 아트 그림 (1:1로 바꾸는 것)
    private static readonly string[,] Pairs =
    {
        { AiW + "Bastet_Floor_Footprint",            TeamW + "Bastet_Obj_Footprint_Base" },
        { AiW + "Bastet_Floor_Footprint_Glow_Light", TeamW + "Bastet_Obj_Footprint_Light" },
        { AiW + "Bastet_Obj_PawStatue",              TeamW + "Bastet_Obj_PawStatue_PawDown" },
        { AiW + "Bastet_Obj_PawStatue_Eyes_Light",   TeamW + "Bastet_Obj_PawStatue_Eyes_Light" },
        { AiW + "Bastet_Obj_EyeCatBig",              TeamW + "Bastet_Obj_EyeCatBig_Off" },
        { AiW + "Bastet_Obj_EyeCatBig_Eyes_Light",   TeamW + "Bastet_Obj_EyeCatBig_Light" },
        { AiW + "Bastet_Obj_EyeCat",                 TeamW + "Bastet_Obj_EyeCat_Off" },
        { AiW + "Bastet_Obj_EyeCat_Eyes_Light",      TeamW + "Bastet_Obj_EyeCat_Light" },
        { AiW + "Bastet_Obj_Altar_Necklace",         TeamW + "Bastet_Obj_NecklaceAltar_Locked" },
        { AiW + "Bastet_Obj_Altar_Claw",             TeamW + "Bastet_Obj_ClawWall_Locked" },
        { AiW + "Bastet_Obj_SealStone",              TeamW + "Bastet_Obj_SealStone_V" },
        { AiW + "Bastet_Obj_Shrine",                 TeamW + "Bastet_Obj_Shrine_Sleep" },
        { AiW + "Bastet_Obj_Shrine_Eyes_Light",      TeamW + "Bastet_Obj_Shrine_Light" },
        { AiW + "Bastet_Obj_ShrineDisc_Sun",         TeamW + "Bastet_Obj_ShrineDisc_Sun" },
        { AiW + "Bastet_Obj_ShrineDisc_Moon",        TeamW + "Bastet_Obj_ShrineDisc_Moon" },
        { AiW + "Bastet_Obj_Stele",                  TeamW + "Bastet_Obj_Stele" },
        { AiU + "UI_Portrait_Bastet_Default",        TeamU + "UI_Portrait_Bastet_Default" },
        { AiU + "UI_Portrait_Bastet_Glow",           TeamU + "UI_Portrait_Bastet_Glow" },
        { AiU + "Bastet_UI_Clue_B1_Footprint",       TeamU + "Bastet_UI_Clue_Footprint" },
        { AiU + "Bastet_UI_Clue_B2_Eye",             TeamU + "Bastet_UI_Clue_LookBack" },
        { AiU + "Bastet_UI_Clue_B3_Necklace",        TeamU + "Bastet_UI_Clue_Mirror" },
        { AiU + "Bastet_UI_Clue_B4_Claw",            TeamU + "Bastet_UI_Clue_Claw" },
        { AiU + "Bastet_UI_Clue_B5_SunMoon",         TeamU + "Bastet_UI_Clue_DayNight" },
        { AiU + "Bastet_UI_Clue_Hall",               TeamU + "Bastet_UI_Clue_Hall" },
        { AiS + "Bastet_Story_Cat_Sit_Temp",         AiS + "Bastet_Story_Cat_Rest" },
        { AiS + "Bastet_Story_Cat_Eyes_Temp",        AiS + "Bastet_Story_Cat_Eyes" },
    };

    // 월드 그림 기준점 (그림 크기에 대한 비율). 없으면 바닥 가운데 (0.5, 0)
    // 눈 빛 = 불투명 픽셀의 가운데 (몸 그림과 같은 크기라 Eyes 자식 위치만 맞추면 겹쳐짐)
    private static readonly Dictionary<string, Vector2> Pivots = new Dictionary<string, Vector2>
    {
        { "Bastet_Obj_Footprint_Base", new Vector2(0.5f, 0.5f) },
        { "Bastet_Obj_Footprint_Light", new Vector2(0.5f, 0.5f) },
        { "Bastet_Obj_SealStone_V", new Vector2(0.5f, 0.5f) },
        { "Bastet_Obj_SealStone_H", new Vector2(0.5f, 0.5f) },
        { "Bastet_Obj_ShrineDisc_Sun", new Vector2(0.5f, 0.5f) },
        { "Bastet_Obj_ShrineDisc_Moon", new Vector2(0.5f, 0.5f) },
        { "Bastet_Obj_PawStatue_Eyes_Light", new Vector2(0.5f, 0.7679f) },
        { "Bastet_Obj_EyeCatBig_Light", new Vector2(0.5234f, 0.7991f) },
        { "Bastet_Obj_EyeCat_Light", new Vector2(0.4844f, 0.7935f) },
        { "Bastet_Obj_Shrine_Light", new Vector2(0.5156f, 0.5681f) },
    };

    // 고양이 기준점 = 발이 닿는 줄 (그림 아래 빈 공간을 뺌)
    private static readonly Dictionary<string, Vector2> CatPivots = new Dictionary<string, Vector2>
    {
        { "Bastet_Story_Cat_Rest", new Vector2(0.4844f, 0.15f) },
        { "Bastet_Story_Cat_Eyes", new Vector2(0.4844f, 0.15f) },   // 쉬는 그림과 같은 기준 (겹쳐짐)
        { "Bastet_Story_Cat_Walk_0", new Vector2(0.4594f, 0.0531f) },
        { "Bastet_Story_Cat_Walk_1", new Vector2(0.4594f, 0.0531f) },
    };

    [MenuItem("Tools/Bastet/아트 팀 그림으로 교체 (오브젝트·초상·단서·검은 고양이)", priority = 1)]
    private static void Run()
    {
        if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("아트 그림 교체", "Play를 멈춘 뒤 눌러 주세요.", "확인"); return; }
        var log = new StringBuilder("[BastetTeamArtSwap] 아트 팀 그림으로 교체\n");

        // 1) 가져오기 설정
        //    Multiple -> Single로 바뀌면 이미 아트 그림을 쓰던 칸(비석 3곳 등)의 연결이 끊기므로 먼저 기억해 두고 다시 연결
        var already = new List<(SpriteRenderer sr, string path)>();
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string ap = sr.sprite != null ? AssetDatabase.GetAssetPath(sr.sprite) : null;
            if (ap != null && ap.StartsWith(TeamW)) already.Add((sr, ap));
        }
        int fixedImports = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TeamW.TrimEnd('/') }))
            fixedImports += FixWorldImport(AssetDatabase.GUIDToAssetPath(guid)) ? 1 : 0;
        foreach (var kv in CatPivots)
            fixedImports += FixImport(AiS + kv.Key + ".png", CatPpu, kv.Value, false) ? 1 : 0;
        log.AppendLine($"- 가져오기 설정 고침: {fixedImports}장");
        foreach (var (sr, ap) in already)
        {
            Sprite again = LoadSprite(ap);
            if (sr == null || again == null || sr.sprite == again) continue;
            Undo.RecordObject(sr, "다시 연결");
            sr.sprite = again;
            EditorUtility.SetDirty(sr);
            log.AppendLine($"- 다시 연결: {sr.name} -> {again.name}");
        }

        // 2) 바꿀 짝 만들기
        var map = new Dictionary<Object, Sprite>();
        int missing = 0;
        for (int i = 0; i < Pairs.GetLength(0); i++)
        {
            Sprite team = LoadSprite(Pairs[i, 1] + ".png");
            if (team == null) { missing++; log.AppendLine($"- 아트 그림 없음: {Pairs[i, 1]}.png"); continue; }
            foreach (Object ai in AssetDatabase.LoadAllAssetsAtPath(Pairs[i, 0] + ".png"))
                if (ai is Sprite) map[ai] = team;
        }

        // 3) 씬의 SpriteRenderer는 위치까지 맞추며 교체, 나머지 칸은 그림만 교체
        int renderers = SwapRenderers(map, log);
        int refs = SwapReferences(map, log);
        int clues = SwapClueData(map, log);

        // 4) 상태 그림 연결
        int states = LinkStates(log);
        states += FillHudIcons(log);

        EditorSceneManager.MarkAllScenesDirty();
        AssetDatabase.SaveAssets();
        int left = CountAiLeft(log);
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("아트 그림 교체",
            $"그림 {renderers}곳, 다른 칸 {refs}곳, 단서 데이터 {clues}개, 상태 그림 {states}곳을 바꿨습니다.\n" +
            (missing > 0 ? $"아트 그림을 못 찾은 짝 {missing}개 (Console 확인)\n" : "") +
            $"아직 남은 AI 그림 칸: {left}곳 (B3/B4 팝업 등, Console 확인)\n\nScene에서 위치를 확인한 뒤 Ctrl+S", "확인");
    }

    // ================= 1) 가져오기 설정 =================
    private static bool FixWorldImport(string path)
    {
        string file = System.IO.Path.GetFileNameWithoutExtension(path);
        Vector2 pivot = Pivots.TryGetValue(file, out Vector2 p) ? p : new Vector2(0.5f, 0f);
        return FixImport(path, WorldPpu, pivot, file.StartsWith("Bastet_Obj_SealStone"));
    }

    private static bool FixImport(string path, float ppu, Vector2 pivot, bool fullRect)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return false;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        bool ok = ti.textureType == TextureImporterType.Sprite && ti.spriteImportMode == SpriteImportMode.Single
                  && Mathf.Approximately(ti.spritePixelsPerUnit, ppu) && ti.filterMode == FilterMode.Point
                  && ti.textureCompression == TextureImporterCompression.Uncompressed
                  && (st.spritePivot - pivot).sqrMagnitude < 0.00001f;
        if (ok) return false;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = pivot;
        if (fullRect) st.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
        return true;
    }

    // ================= 3) 교체 =================
    private static int SwapRenderers(Dictionary<Object, Sprite> map, StringBuilder log)
    {
        int n = 0;
        var bodies = new List<SpriteRenderer>();
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!sr.gameObject.scene.IsValid() || sr.sprite == null || !map.TryGetValue(sr.sprite, out Sprite team)) continue;
            bool overlay = sr.name == "Eyes" || sr.name == "Glow" || sr.name == "Disc";
            Transform t = sr.transform;
            Bounds old = sr.bounds;
            KeepChildren(t, () =>
            {
                Undo.RecordObject(t, "아트 그림");
                Undo.RecordObject(sr, "아트 그림");
                sr.sprite = team;
                if (team.name.StartsWith("Bastet_Obj_SealStone"))
                {
                    // 봉인석: 타일 반복 대신 한 장 (가운데 둥근 홈이 반복되지 않게)
                    sr.drawMode = SpriteDrawMode.Simple;
                    t.localScale = Vector3.one;
                }
                else if (!overlay && sr.drawMode == SpriteDrawMode.Simple && t.GetComponentInParent<Canvas>() == null)
                {
                    // 몸통: 그림 바닥과 가로 가운데를 원래 자리에 맞춤
                    Bounds now = sr.bounds;
                    t.position += new Vector3(old.center.x - now.center.x, old.min.y - now.min.y, 0f);
                }
            });
            if (!overlay) bodies.Add(sr);
            EditorUtility.SetDirty(sr);
            n++;
        }
        // [수정] 메뉴를 다시 눌러도 맞추도록: 이미 아트 그림인 몸통도 모두 포함
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!sr.gameObject.scene.IsValid() || sr.sprite == null || bodies.Contains(sr)) continue;
            if (sr.name == "Eyes" || sr.name == "Glow" || sr.name == "Disc") continue;
            if (AssetDatabase.GetAssetPath(sr.sprite).StartsWith(TeamW)) bodies.Add(sr);
        }
        // 눈 빛 자식 위치 = (눈 그림 기준점 - 몸 그림 기준점), 신상 원반 = 몸 위
        foreach (SpriteRenderer body in bodies)
        {
            Transform e = body.transform.Find("Eyes");
            var er = e != null ? e.GetComponent<SpriteRenderer>() : null;
            if (er != null && er.sprite != null && body.sprite != null)
            {
                Undo.RecordObject(e, "눈 위치");
                e.localScale = Vector3.one;
                e.localPosition = (Vector3)((er.sprite.pivot - body.sprite.pivot) / body.sprite.pixelsPerUnit);
            }
            Transform d = body.transform.Find("Disc");
            if (d != null && body.sprite != null)
            {
                Undo.RecordObject(d, "원반 위치");
                if (body.sprite.name == "Bastet_Obj_Shrine_Sleep")
                {
                    // [수정] 하연 님 신상은 머리 위에 원반 자리(어두운 원)가 그려져 있음 -> 그 자리에 해/달을 겹침
                    //        원반 자리 가운데 = 그림 (49.5, 96.5)px, 지름 24px (해/달 그림 32px -> 0.75배)
                    Vector2 slot = (new Vector2(49.5f, 96.5f) - body.sprite.pivot) / body.sprite.pixelsPerUnit;
                    d.localPosition = new Vector3(slot.x, slot.y, 0f);
                    d.localScale = Vector3.one * 0.75f;
                }
                else d.localPosition = new Vector3(0f, body.sprite.bounds.max.y + 0.15f, 0f);
            }
            var box = body.GetComponent<BoxCollider2D>();
            if (box != null && body.sprite != null && !body.sprite.name.StartsWith("Bastet_Obj_SealStone") && box.offset.y > 0.01f)
            {
                // 바닥 기준 그림: 단단한 부분은 아래쪽만 (폭만 그림에 맞춤)
                Undo.RecordObject(box, "충돌");
                box.size = new Vector2(Mathf.Max(0.4f, body.sprite.bounds.size.x * 0.8f), box.size.y);
            }
        }
        log.AppendLine($"- SpriteRenderer {n}곳 교체 (바닥 위치, 눈 빛, 원반 위치 맞춤)");
        return n;
    }

    // SpriteRenderer 말고 그림을 가리키는 칸 (초상, 고양이 그림, 원반 그림 등)
    private static int SwapReferences(Dictionary<Object, Sprite> map, StringBuilder log)
    {
        int n = 0;
        foreach (var c in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c == null || !c.gameObject.scene.IsValid()) continue;
            n += SwapIn(c, map);
        }
        log.AppendLine($"- 컴포넌트 그림 칸 {n}곳 교체");
        return n;
    }

    private static int SwapClueData(Dictionary<Object, Sprite> map, StringBuilder log)
    {
        int n = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:ClueData"))
        {
            var a = AssetDatabase.LoadAssetAtPath<ClueData>(AssetDatabase.GUIDToAssetPath(guid));
            if (a != null && SwapIn(a, map) > 0) n++;
        }
        log.AppendLine($"- 단서 데이터(ClueData) {n}개 교체");
        return n;
    }

    private static int SwapIn(Object target, Dictionary<Object, Sprite> map)
    {
        var so = new SerializedObject(target);
        SerializedProperty it = so.GetIterator();
        int n = 0;
        while (it.Next(true))
        {
            if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
            if (!map.TryGetValue(it.objectReferenceValue, out Sprite team)) continue;
            it.objectReferenceValue = team;
            n++;
        }
        if (n > 0) { so.ApplyModifiedProperties(); EditorUtility.SetDirty(target); }
        return n;
    }

    // ================= 4) 상태 그림 =================
    private static int LinkStates(StringBuilder log)
    {
        int n = 0;
        Sprite pawDown = LoadSprite(TeamW + "Bastet_Obj_PawStatue_PawDown.png"), pawUp = LoadSprite(TeamW + "Bastet_Obj_PawStatue_PawUp.png");
        Sprite mouth = LoadSprite(TeamW + "Bastet_Obj_EyeCat_Mouth.png");
        Sprite neckOpen = LoadSprite(TeamW + "Bastet_Obj_NecklaceAltar_Open.png"), clawOpen = LoadSprite(TeamW + "Bastet_Obj_ClawWall_Open.png");

        // B1 앞발 석상: E를 누르면 앞발을 들었다 내림
        foreach (var st in Object.FindObjectsByType<BastetPuzzleStarter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!st.gameObject.scene.IsValid() || st.name != "PawStatue" || pawDown == null || pawUp == null) continue;
            Undo.RecordObject(st, "앞발");
            st.EditorSetArt(st.GetComponent<SpriteRenderer>(), pawUp, pawDown);
            EditorUtility.SetDirty(st);
            n++;
            log.AppendLine($"- {st.name}: 앞발 들기/내리기 그림 연결");
        }
        // B2·B5 눈 석상: 횃불을 삼킬 때 입 벌림
        if (mouth != null)
        {
            int m = 0;
            foreach (var es in Object.FindObjectsByType<BastetEyeStatue>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!es.gameObject.scene.IsValid()) continue;
                var so = new SerializedObject(es);
                so.FindProperty("mouthSprite").objectReferenceValue = mouth;
                if (so.FindProperty("bodyRenderer").objectReferenceValue == null)
                    so.FindProperty("bodyRenderer").objectReferenceValue = es.GetComponent<SpriteRenderer>();
                so.ApplyModifiedProperties();
                m++;
            }
            n += m;
            log.AppendLine($"- 눈 석상 {m}개: 입 벌림 그림 연결");
        }
        // B3·B4 퍼즐 상자: 풀면 열린 그림
        foreach (var a in Object.FindObjectsByType<BastetAltarInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!a.gameObject.scene.IsValid()) continue;
            Sprite open = a.TargetPuzzle is BastetNecklacePuzzle ? neckOpen : a.TargetPuzzle is BastetClawPatternPuzzle ? clawOpen : null;
            if (open == null) continue;
            Undo.RecordObject(a, "열린 그림");
            a.EditorSetOpenArt(a.GetComponent<SpriteRenderer>(), open);
            EditorUtility.SetDirty(a);
            n++;
            log.AppendLine($"- {a.name}: 퍼즐을 풀면 {open.name}");
        }
        // 검은 고양이: 걷기 2프레임, 크기는 그림(PPU)으로 맞췄으니 배율 1
        Sprite w0 = LoadSprite(AiS + "Bastet_Story_Cat_Walk_0.png"), w1 = LoadSprite(AiS + "Bastet_Story_Cat_Walk_1.png");
        foreach (var cat in Object.FindObjectsByType<BastetCatGuide>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!cat.gameObject.scene.IsValid() || w0 == null || w1 == null) continue;
            var so = new SerializedObject(cat);
            SerializedProperty walk = so.FindProperty("walkSprites");
            walk.arraySize = 2;
            walk.GetArrayElementAtIndex(0).objectReferenceValue = w0;
            walk.GetArrayElementAtIndex(1).objectReferenceValue = w1;
            so.FindProperty("size").floatValue = 1f;
            so.ApplyModifiedProperties();
            n++;
            log.AppendLine($"- 검은 고양이 {cat.name}: 쉬는 그림 + 걷기 2프레임 (예림 님 A-02)");
        }
        return n;
    }

    // [추가] 왼쪽 아래 소지품 바(InventoryHUD): 아이콘 칸이 비어 이름 글자만 나오던 것 -> 아이템 목록(ItemCatalog)의 아이콘
    private static int FillHudIcons(StringBuilder log)
    {
        ItemCatalog catalog = Resources.Load<ItemCatalog>("ItemCatalog");
        if (catalog == null) { log.AppendLine("- 소지품 바: Resources/ItemCatalog 없음"); return 0; }
        int n = 0;
        foreach (var hud in Object.FindObjectsByType<InventoryHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!hud.gameObject.scene.IsValid()) continue;
            var so = new SerializedObject(hud);
            SerializedProperty list = so.FindProperty("entries");
            if (list == null) continue;
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                SerializedProperty icon = e.FindPropertyRelative("icon");
                string id = e.FindPropertyRelative("itemId").stringValue;
                if (icon.objectReferenceValue != null || !catalog.TryGet(id, out ItemDefinition def) || def.icon == null) continue;
                icon.objectReferenceValue = def.icon;
                n++;
                log.AppendLine($"- 소지품 바 {id}: 아이콘 {def.icon.name}");
            }
            so.ApplyModifiedProperties();
        }
        if (n == 0) log.AppendLine("- 소지품 바: 바꿀 아이콘 없음 (이미 있거나 목록에 아이콘이 없음)");
        return n;
    }

    // ================= 확인 =================
    // 아직 AI 폴더 그림을 쓰는 칸 수 (B3/B4 팝업, 벽화 석판 등 아직 아트가 없는 것)
    private static int CountAiLeft(StringBuilder log)
    {
        var left = new Dictionary<string, int>();
        foreach (var c in Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c == null || !c.gameObject.scene.IsValid() || c is Transform) continue;
            var it = new SerializedObject(c).GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference || !(it.objectReferenceValue is Sprite s)) continue;
                string path = AssetDatabase.GetAssetPath(s);
                if (!path.StartsWith(AiW) && !path.StartsWith(AiU) && !path.Contains("_Temp")) continue;
                left[s.name] = left.TryGetValue(s.name, out int k) ? k + 1 : 1;
            }
        }
        int total = 0;
        foreach (var kv in left) { total += kv.Value; log.AppendLine($"  남은 AI 그림: {kv.Key} x{kv.Value}"); }
        return total;
    }

    // ================= 도우미 =================
    private static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite sp) return sp;
        return null;
    }

    // 부모 위치를 옮겨도 자식("!" 말풍선, 빛 등)은 화면 위 제자리에 남게 (눈·원반은 따로 맞춤)
    private static void KeepChildren(Transform t, System.Action change)
    {
        int c = t.childCount;
        var pos = new Vector3[c];
        for (int i = 0; i < c; i++) pos[i] = t.GetChild(i).position;
        change();
        for (int i = 0; i < c; i++)
        {
            Transform ch = t.GetChild(i);
            if (ch.name == "Eyes" || ch.name == "Disc") continue;
            Undo.RecordObject(ch, "자식 위치");
            ch.position = pos[i];
        }
    }
}
#endif
