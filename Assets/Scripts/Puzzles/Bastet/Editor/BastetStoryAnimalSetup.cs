#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// [추가] 메뉴: Tools/Bastet/★ 스토리·검은 고양이 세팅 (벽화 4 + 이야기 비석 3 + 길잡이)
// 스토리 기획서 B안 바스테트의 방을 씬에 한 번에 배치
//  1) [임시] 그림 PNG 저장 (Assets/image/Bastet/Story) - 이미 있으면 안 덮어씀 (아트로 교체 가능)
//  2) 가운데 홀 벽화 4장 (Mural_Bastet_1~4)
//  3) 이야기 비석 3개 (발자국의 방 / 목걸이의 방 / 신상의 방)
//  4) 검은 고양이 길잡이 (입구 비석 -> 봉인석 1~4 -> 신상의 방 봉인석 -> 북쪽 출구)
//  5) 스토리 감독 (벽화 4장 -> 방 연출, 메인 클리어 연출)
//  6) 여신 대사를 스토리 기획서 문장으로 + 자동 소개 끔 (방 연출이 대신함)
//  7) 목표 띠 맨 앞에 "벽화 4장을 살펴보자" 추가
// 먼저 실행할 것: ★ 지금 맵(타일)에 맞춰 배치, 실전 방 세팅, ★ 비석·벽화·여신 대사 세팅
// 이미 만든 오브젝트는 위치를 바꾸지 않음 (직접 옮긴 자리 유지). 내용·연결만 다시 넣음
public static class BastetStoryAnimalSetup
{
    private const string ArtFolder = "Assets/image/Bastet/Story/";
    private const string RootName = "Bastet_Story";

    // 벽화 4장: 제목·글·연출은 BastetMuralScenes, 여기선 수첩 노트 번호만
    private static readonly int[] MuralNotes = { 16, 17, 18, 19 };
    private const string MuralCorridorName = "MuralCorridor";

    // 이야기 비석 3개 (방 이름, 제목, 내용, 반응 대사, 노트 번호)
    private static readonly (string room, string title, string text, DialogueLine[] after, int note)[] Steles =
    {
        ("Room1_Footprint", "고양이의 법", "고양이를 해친 자는 왕의 사면으로도 살아남지 못했다.",
            new[] { new DialogueLine("나", "고양이 한 마리 때문에... 그만큼 여신의 동물이었다는 거네.") }, 20),
        ("Room3_Necklace", "흩어진 재", "향로의 재가 뱀이 기어간 모양으로 흩어져 있다.",
            new[] { new DialogueLine("케프리", "아펩이 여기서 무언가를 바꿔치기했다. 붉은 글씨를 조심해라.") }, 21),
        ("Room5_Shrine", "바스테트의 축제", "바스테트의 축제에는 해마다 수많은 사람이 배를 타고 모여 노래했다. 잊힌 신은 힘을 잃는다.",
            new[] { new DialogueLine("나", "기억해 주는 사람이 있어야 신도 힘을 낸다... 이 방의 시험도 '기억'이겠지.") }, 22),
    };

    [MenuItem("Tools/Bastet/★ 스토리·검은 고양이 세팅 (벽화·이야기 비석·길잡이)", priority = 5)]
    private static void Menu()
    {
        var log = new StringBuilder("[BastetStoryAnimalSetup] 스토리·검은 고양이\n");
        if (!Build(log))
        {
            EditorUtility.DisplayDialog("스토리·검은 고양이", log.ToString(), "확인");
            return;
        }
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("스토리·검은 고양이", log.ToString().Replace("[BastetStoryAnimalSetup] ", "") + "\n확인 후 Ctrl+S", "확인");
    }

    // ================= 벽화 복도 =================
    [MenuItem("Tools/Bastet/★ 벽화 복도 (카메라 내려오기 + 벽화 나란히 걸기)", priority = 6)]
    private static void CorridorMenu()
    {
        var log = new StringBuilder();
        bool ok = BuildCorridor(log);
        Debug.Log("[BastetStoryAnimalSetup] 벽화 복도\n" + log);
        EditorUtility.DisplayDialog("벽화 복도", log + (ok ? "\n확인 후 Ctrl+S" : ""), "확인");
    }

    // 복도 바닥 상자(CameraRoomZone + BastetMuralCorridor)를 만들고 벽화 4장을 윗벽에 나란히 검
    // 처음 만들 때 자리: 선택한 오브젝트 위치 (없으면 플레이어 위치). 이미 있으면 상자 크기·자리는 그대로 두고 다시 걸기만
    public static bool BuildCorridor(StringBuilder log)
    {
        var murals = new BastetMural[BastetMuralScenes.Count];
        foreach (BastetMural m in Object.FindObjectsByType<BastetMural>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (m.Index >= 1 && m.Index <= murals.Length) murals[m.Index - 1] = m;
        for (int i = 0; i < murals.Length; i++)
        {
            if (murals[i] != null) continue;
            log.AppendLine($"벽화 {i + 1}이 없음 -> Tools/Bastet/★ 스토리·검은 고양이 세팅을 먼저 실행");
            return false;
        }

        GameObject go = BastetWalkBuilder.FindInScene(MuralCorridorName);
        bool created = go == null;
        if (created)
        {
            Vector3 at = Vector3.zero;
            string from = "원점";
            if (Selection.activeTransform != null) { at = Selection.activeTransform.position; from = $"선택한 '{Selection.activeTransform.name}'"; }
            else
            {
                GameObject player = BastetWalkBuilder.FindInScene("Player");
                if (player != null) { at = player.transform.position; from = "플레이어"; }
            }
            go = new GameObject(MuralCorridorName);
            Undo.RegisterCreatedObjectUndo(go, MuralCorridorName);
            go.transform.SetParent(EnsureRoot(), true);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            log.AppendLine($"- 복도 상자 만듦: {from} 위치 기준 18 x 3칸");
        }

        var box = GetOrAdd<BoxCollider2D>(go);
        box.isTrigger = true;
        var zone = GetOrAdd<CameraRoomZone>(go);
        if (created) zone.EditorSetup(new Vector2(18f, 3f), new Vector4(1f, 1f, 1.25f, 2.5f));
        var so = new SerializedObject(zone);
        so.FindProperty("fitWholeRoom").boolValue = false; // 복도는 줌 아웃하지 않음
        so.ApplyModifiedProperties();

        var corridor = GetOrAdd<BastetMuralCorridor>(go);
        corridor.EditorSetup(murals);
        corridor.Arrange();
        EditorUtility.SetDirty(corridor);
        EditorUtility.SetDirty(zone);
        Selection.activeGameObject = go;
        EditorSceneManager.MarkAllScenesDirty();

        log.AppendLine("- 카메라: 복도 안에서는 복도 높이에 맞춰 내려옴 (위아래 고정, 좌우만 따라감)");
        log.AppendLine("- 벽화 4장을 복도 윗벽에 같은 간격으로 걸었음");
        log.AppendLine("\n초록 상자가 복도 바닥과 다르면: BoxCollider2D의 Edit Collider로 바닥에 맞추고");
        log.AppendLine("MuralCorridor 우클릭(⋮) > '벽화 4장을 윗벽에 나란히 걸기'");
        return true;
    }

    public static bool Build(StringBuilder log)
    {
        var areas = new Dictionary<string, BastetRoomArea>();
        foreach (BastetRoomArea a in Object.FindObjectsByType<BastetRoomArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            areas[a.name] = a;
        if (!areas.TryGetValue("Hall_Center", out BastetRoomArea hall))
        {
            log.AppendLine("가운데 홀(Hall_Center)이 없음 -> Tools/Bastet/★ 지금 맵(타일)에 맞춰 배치를 먼저 실행");
            return false;
        }

        // 1) 임시 그림
        Sprite catSit = SaveTempSprite("Bastet_Story_Cat_Sit_Temp", BastetPixelArt.CatTexture(false), SpriteAlignment.BottomCenter, log);
        Sprite catEyes = SaveTempSprite("Bastet_Story_Cat_Eyes_Temp", BastetPixelArt.CatTexture(true), SpriteAlignment.BottomCenter, log);
        Sprite eyesIcon = SaveTempSprite("Bastet_Story_EyesIcon_Temp", BastetPixelArt.EyesIconTexture(), SpriteAlignment.Center, log);
        Sprite bubbleSprite = LoadFirstSprite("Assets/image/ui/UI_Prompt_New.png");
        Sprite steleSprite = LoadFirstSprite("Assets/image/Bastet/Bastet_Obj/Bastet_Obj_Stele.png");

        CluePanel panel = Object.FindFirstObjectByType<CluePanel>(FindObjectsInactive.Include);

        // 2) 벽화 4장: 가운데 홀 양옆 (서쪽 아래 1, 서쪽 위 2, 동쪽 위 3, 동쪽 아래 4)
        Vector2 hs = hall.Size;
        float mx = hs.x * 0.5f - 2.8f;
        Vector2[] muralPos = { new Vector2(-mx, -1.5f), new Vector2(-mx, 2.5f), new Vector2(mx, 2.5f), new Vector2(mx, -1.5f) };
        for (int i = 0; i < MuralNotes.Length; i++)
            MakeMural(i + 1, hall.transform, muralPos[i], MuralSprite(i + 1, log), bubbleSprite, log);

        // 3) 이야기 비석 3개 (방 왼쪽/오른쪽 위 구석)
        for (int i = 0; i < Steles.Length; i++)
        {
            if (!areas.TryGetValue(Steles[i].room, out BastetRoomArea room))
            {
                log.AppendLine($"- 이야기 비석 {i + 1}: {Steles[i].room} 방이 없음 (건너뜀)");
                continue;
            }
            Vector2 s = room.Size;
            Vector2 local = i == 0 ? new Vector2(-s.x * 0.5f + 1.0f, s.y * 0.5f - 1.6f)  // 발자국의 방: 왼쪽 위
                                   : new Vector2(s.x * 0.5f - 1.2f, s.y * 0.5f - 1.6f); // 목걸이·신상의 방: 오른쪽 위
            MakeStoryStele(i + 1, room.transform, local, steleSprite, log);
        }

        // 4) 검은 고양이 + 5) 감독
        Transform root = EnsureRoot();
        BastetCatGuide cat = MakeCat(root, catSit, catEyes, eyesIcon, log);
        BastetGoddess goddess = Object.FindFirstObjectByType<BastetGoddess>(FindObjectsInactive.Include);
        RaDialogue voice = Object.FindFirstObjectByType<RaDialogue>(FindObjectsInactive.Include);
        if (goddess == null) log.AppendLine("- 경고: 바스테트 여신상 없음 -> ★ 비석·벽화·여신 대사 세팅을 먼저 실행");
        MakeDirector(root, cat, voice, goddess != null ? goddess.transform : null, panel, log);

        // 6) 여신 대사 + 7) 목표 띠
        if (goddess != null) UpdateGoddess(goddess, log);
        UpdateObjective(log);

        EditorSceneManager.MarkAllScenesDirty();
        log.AppendLine("\nPlay 테스트: BastetStoryDirector 우클릭 > '테스트: 벽화 4장을 본 것으로 처리'");
        return true;
    }

    // ================= 그림 =================
    private static Sprite SaveTempSprite(string fileName, Texture2D tex, SpriteAlignment pivot, StringBuilder log)
    {
        string path = ArtFolder + fileName + ".png";
        if (!Directory.Exists(ArtFolder)) Directory.CreateDirectory(ArtFolder);
        if (!File.Exists(path))
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = BastetPixelArt.PixelsPerUnit;
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)pivot;
                ti.SetTextureSettings(settings);
                ti.SaveAndReimport();
            }
            log.AppendLine($"- [임시] 그림 저장: {path}");
        }
        Object.DestroyImmediate(tex);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 벽에 거는 도트 벽화 (Tools/BastetMural/bastet_murals.py가 만든 Bastet_Mural{n}.png, 32 PPU = 4x2칸)
    private static Sprite MuralSprite(int index, StringBuilder log)
    {
        string path = $"{ArtFolder}Bastet_Mural{index}.png";
        if (!File.Exists(path))
        {
            log.AppendLine($"- 경고: {path} 없음 -> 임시 석판으로 표시");
            return null;
        }
        ConfigureSprite(path, 32f, SpriteAlignment.BottomCenter);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 도트 스프라이트 설정 (이미 맞으면 다시 가져오지 않음)
    private static void ConfigureSprite(string path, float ppu, SpriteAlignment pivot)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return;
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        bool ok = ti.textureType == TextureImporterType.Sprite && Mathf.Approximately(ti.spritePixelsPerUnit, ppu)
                  && ti.filterMode == FilterMode.Point && settings.spriteAlignment == (int)pivot;
        if (ok) return;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)pivot;
        ti.SetTextureSettings(settings);
        ti.SaveAndReimport();
    }

    private static Sprite LoadFirstSprite(string path)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o is Sprite s) return s;
        return null;
    }

    // ================= 오브젝트 =================
    private static Transform EnsureRoot()
    {
        GameObject go = BastetWalkBuilder.FindInScene(RootName);
        if (go == null)
        {
            go = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(go, RootName);
        }
        return go.transform;
    }

    // 없으면 만들고 처음 자리에 둠, 있으면 자리 그대로
    private static GameObject GetOrCreate(string objName, Transform parent, Vector2 localPos, out bool created)
    {
        GameObject go = BastetWalkBuilder.FindInScene(objName);
        created = go == null;
        if (created)
        {
            go = new GameObject(objName);
            Undo.RegisterCreatedObjectUndo(go, objName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
        }
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    private static void MakeMural(int index, Transform hall, Vector2 local, Sprite sprite, Sprite bubbleSprite, StringBuilder log)
    {
        GameObject go = GetOrCreate($"Mural_Bastet_{index}", hall, local, out bool created);
        var sr = GetOrAdd<SpriteRenderer>(go);
        // 비어 있거나 예전 임시 석판이면 새 도트 벽화로 (직접 넣은 그림은 그대로)
        if (sprite != null && (sr.sprite == null || sr.sprite.name.StartsWith("Bastet_Story_Mural_Temp") || sr.sprite.name.StartsWith("Bastet_Mural")))
            sr.sprite = sprite;
        sr.sortingLayerName = "Objects";

        float w = sr.sprite != null ? sr.sprite.bounds.size.x : 4f;
        float h = sr.sprite != null ? sr.sprite.bounds.size.y : 2f;
        // 벽에 걸린 그림이라 막지 않는 트리거, 아래 바닥 한 줄까지 닿게 (바닥에 서서 E)
        var col = GetOrAdd<BoxCollider2D>(go);
        col.isTrigger = true;
        col.size = new Vector2(w * 0.9f, 1.4f);
        col.offset = new Vector2(0f, -0.1f);

        SpriteRenderer bubble = MakeBubble(go.transform, new Vector3(0f, h + 0.5f, 0f), bubbleSprite, sr.sortingOrder + 2);

        var m = GetOrAdd<BastetMural>(go);
        m.EditorSetup(index, MuralNotes[index - 1], sr, bubble);
        EditorUtility.SetDirty(m);
        log.AppendLine($"- 벽화 {index} '{BastetMuralScenes.Title(index)}' {(created ? "만듦" : "그림·내용 갱신")} (노트 {MuralNotes[index - 1]})");
    }

    private static void MakeStoryStele(int index, Transform room, Vector2 local, Sprite sprite, StringBuilder log)
    {
        GameObject go = GetOrCreate($"StoryStele_Bastet_{index}", room, local, out bool created);
        var sr = GetOrAdd<SpriteRenderer>(go);
        if (sr.sprite == null) sr.sprite = sprite;
        sr.color = new Color(0.78f, 0.84f, 1f); // 푸르스름 (단서 비석과 구분)
        sr.sortingLayerName = "Objects";
        var col = GetOrAdd<BoxCollider2D>(go);
        col.size = new Vector2(1f, 0.6f);
        col.offset = new Vector2(0f, 0.3f);

        var s = GetOrAdd<BastetStoryStele>(go);
        var info = Steles[index - 1];
        s.EditorSetup(index, info.title, info.text, info.after, info.note, $"비석 '{info.title}': {info.text}");
        EditorUtility.SetDirty(s);
        log.AppendLine($"- 이야기 비석 {index} '{info.title}' {(created ? "만듦" : "내용 갱신")} ({room.name}, 노트 {info.note})");
    }

    private static SpriteRenderer MakeBubble(Transform parent, Vector3 localPos, Sprite sprite, int order)
    {
        Transform bt = parent.Find("NewBubble");
        if (bt == null)
        {
            var b = new GameObject("NewBubble");
            Undo.RegisterCreatedObjectUndo(b, "말풍선");
            b.transform.SetParent(parent, false);
            bt = b.transform;
        }
        bt.localPosition = localPos;
        var bsr = GetOrAdd<SpriteRenderer>(bt.gameObject);
        if (sprite != null) bsr.sprite = sprite;
        bsr.sortingLayerName = "Objects";
        bsr.sortingOrder = order;
        return bsr;
    }

    private static BastetCatGuide MakeCat(Transform root, Sprite sit, Sprite eyes, Sprite edge, StringBuilder log)
    {
        GameObject go = GetOrCreate("BastetCatGuide", root, Vector2.zero, out _);
        var cat = GetOrAdd<BastetCatGuide>(go);

        // (목표 오브젝트 이름, 다 했다는 플래그, 앉을 자리, 이름)
        var plan = new (string obj, string flag, Vector2 offset, string label)[]
        {
            ("Slate_Bastet_Hall", BastetItemIds.FlagHallRead, new Vector2(1.1f, -0.3f), "입구 비석"),
            ("Gate_Room1", BastetItemIds.FlagB1Clear, new Vector2(1.2f, -0.4f), "발자국의 방"),
            ("Gate_Room2", BastetItemIds.FlagB2Clear, new Vector2(1.2f, -0.4f), "눈의 방"),
            ("Gate_Room3", BastetItemIds.FlagB3Clear, new Vector2(-1.2f, -0.4f), "목걸이의 방"),
            ("Gate_Room4", BastetItemIds.FlagB4Clear, new Vector2(-1.2f, -0.4f), "발톱의 방"),
            ("Gate_T", BastetItemIds.FlagMainDone, new Vector2(1.3f, -1.0f), "신상의 방 봉인석"),
            ("Door_North_Exit", "", new Vector2(1.3f, -1.0f), "북쪽 출구"),
        };
        var steps = new List<BastetCatGuide.Step>();
        foreach (var p in plan)
        {
            GameObject target = BastetWalkBuilder.FindInScene(p.obj);
            if (target == null) log.AppendLine($"- 경고: 고양이 목표 '{p.obj}' 없음 -> Inspector에서 Target 직접 넣기");
            steps.Add(new BastetCatGuide.Step { label = p.label, doneFlag = p.flag, target = target != null ? target.transform : null, sitOffset = p.offset });
        }
        Undo.RecordObject(cat, "검은 고양이");
        cat.EditorSetup(steps, sit, eyes, edge);
        EditorUtility.SetDirty(cat);
        log.AppendLine($"- 검은 고양이 길잡이: {steps.Count}단계 (입구 비석 -> 봉인석 1~4 -> 신상의 방 -> 북쪽 출구)");
        return cat;
    }

    private static void MakeDirector(Transform root, BastetCatGuide cat, RaDialogue voice, Transform goddess, CluePanel panel, StringBuilder log)
    {
        GameObject go = GetOrCreate("BastetStoryDirector", root, Vector2.zero, out _);
        var d = GetOrAdd<BastetStoryDirector>(go);
        Undo.RecordObject(d, "스토리 감독");
        d.EditorSetup(cat, voice, goddess, panel);
        EditorUtility.SetDirty(d);
        log.AppendLine("- 스토리 감독: 벽화 4장 -> 방 연출 / 메인 클리어 -> 방이 밝아지고 고양이가 곁으로");
    }

    // ================= 여신 대사 =================
    private static void UpdateGoddess(BastetGoddess goddess, StringBuilder log)
    {
        var so = new SerializedObject(goddess);
        so.FindProperty("autoIntro").boolValue = false; // 처음 소개는 방 스토리 연출이 대신함
        SetLines(so.FindProperty("introLines"), new[]
        {
            new RaDialogueLine(RaPortrait.Night, "어둠 속을 걸어온 자여. 나는 바스테트, 라의 눈이다."),
            new RaDialogueLine(RaPortrait.Default, "이 홀의 벽에 나의 이야기가 새겨져 있다. 네 개의 벽화를 먼저 보아라.")
        });
        SetLines(so.FindProperty("endLines"), new[]
        {
            new RaDialogueLine(RaPortrait.Glow, "너는 잊지 않았다. 해가 뜨고 달이 지는 순서까지."),
            new RaDialogueLine(RaPortrait.Night, "오늘 밤 아펩은 내 칼을 피했지만, 내 눈까지 피하지는 못한다."),
            new RaDialogueLine(RaPortrait.Default, "태양의 조각을 가져가라. 아펩을 물리친 고양이가 새벽을 연다."),
            new RaDialogueLine(RaPortrait.Glow, "내 고양이도 데려가라. 네 뒤의 어둠을 대신 보아 줄 것이다."),
            new RaDialogueLine(RaPortrait.Default, "눈을 감은 밤엔 달이 시간을 센다. 그 서기에게 가라.")
        });
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(goddess);
        log.AppendLine("- 여신: 자동 소개 끔 (방 연출이 대신함), 마무리 대사 5줄을 스토리 기획서 문장으로");
    }

    private static void SetLines(SerializedProperty arr, RaDialogueLine[] lines)
    {
        if (arr == null) return;
        arr.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            SerializedProperty e = arr.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("portrait").enumValueIndex = (int)lines[i].portrait;
            e.FindPropertyRelative("text").stringValue = lines[i].text;
        }
    }

    // ================= 목표 띠 =================
    private static void UpdateObjective(StringBuilder log)
    {
        AnubisObjectiveGuide guide = Object.FindFirstObjectByType<AnubisObjectiveGuide>(FindObjectsInactive.Include);
        if (guide == null) { log.AppendLine("- 경고: 목표 띠(AnubisObjectiveGuide) 없음 -> 실전 방 세팅 먼저"); return; }

        var so = new SerializedObject(guide);
        SerializedProperty steps = so.FindProperty("steps");
        for (int i = 0; i < steps.arraySize; i++)
        {
            if (steps.GetArrayElementAtIndex(i).FindPropertyRelative("doneFlag").stringValue == BastetStoryIds.FlagMuralsAll)
            {
                log.AppendLine("- 목표 띠: 벽화 단계가 이미 있음");
                return;
            }
        }
        steps.InsertArrayElementAtIndex(0);
        SerializedProperty s = steps.GetArrayElementAtIndex(0);
        s.FindPropertyRelative("doneItemId").stringValue = "";
        s.FindPropertyRelative("doneFlag").stringValue = BastetStoryIds.FlagMuralsAll;
        s.FindPropertyRelative("objective").stringValue = "가운데 홀의 벽화 4장을 모두 살펴보자";
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(guide);
        log.AppendLine("- 목표 띠: 맨 앞에 '가운데 홀의 벽화 4장을 모두 살펴보자' 추가");
    }
}
#endif
