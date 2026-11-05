#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 메뉴: Tools/Sobek/1. 데이터 · 임시 그림 · 아이템 만들기
//  1) 임시 그림 PNG (Assets/image/Sobek/Placeholder) + 그림 묶음 SobekTheme
//  2) 퍼즐 데이터 6개 (S1~S4, 메인 A·B) -> 기획서 맵 그대로, BFS로 검증한 정답 걸음 수 기록
//  3) 팀 ItemCatalog에 소베크 아이템 추가 (부적 4, 형판, 이빨)
// 여러 번 눌러도 안전: 이미 있는 데이터 에셋은 덮어쓰지 않음 (고친 값 보호). 다시 만들려면 에셋을 지우고 실행
public static class SobekDataSetup
{
    public const string DataRoot = "Assets/Scripts/Puzzles/Sobek/Data";
    public const string BoardFolder = DataRoot + "/Boards";
    public const string ThemePath = DataRoot + "/SobekTheme.asset";
    public const string ArtFolder = "Assets/image/Sobek/Placeholder";
    private const string CatalogPath = "Assets/Resources/ItemCatalog.asset";

    public const string S1 = "SobekBoard_S1_Whirlpool";
    public const string S2 = "SobekBoard_S2_CrocBridge";
    public const string S3 = "SobekBoard_S3_Flood";
    public const string S4 = "SobekBoard_S4_Mirror";
    public const string MainA = "SobekBoard_MainA_Flood";
    public const string MainB = "SobekBoard_MainB_Dry";

    [MenuItem("Tools/Sobek/1. 데이터 · 임시 그림 · 아이템 만들기", priority = 1)]
    public static void Run()
    {
        var log = new List<string>();
        EnsureFolder(BoardFolder);
        EnsureFolder(ArtFolder);
        SobekTheme theme = EnsureTheme(log);
        EnsureBoards(log);
        EnsureCatalog(log);
        AssetDatabase.SaveAssets();
        Selection.activeObject = theme;
        Debug.Log("[SobekDataSetup] 완료\n- " + string.Join("\n- ", log));
    }

    public static SobekBoardData LoadBoard(string assetName) =>
        AssetDatabase.LoadAssetAtPath<SobekBoardData>($"{BoardFolder}/{assetName}.asset");

    public static SobekTheme LoadTheme() => AssetDatabase.LoadAssetAtPath<SobekTheme>(ThemePath);

    // ================= 1. 임시 그림 =================
    private static SobekTheme EnsureTheme(List<string> log)
    {
        var theme = LoadTheme();
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<SobekTheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
            log.Add("SobekTheme 만듦");
        }

        // 바닥은 흰색 계열 (높이 색이 곱해짐), 나머지는 그림 자체 색
        theme.floor = Png(theme.floor, "Sobek_Tile_Floor", (x, y) => Noise(x, y, new Color(0.97f, 0.97f, 0.95f), 0.04f), log, true);
        theme.wall = Png(theme.wall, "Sobek_Tile_Wall", (x, y) => Bricks(x, y), log, true);
        theme.water = Png(theme.water, "Sobek_Tile_Water", (x, y) => Waves(x, y), log, true);
        theme.pit = Png(theme.pit, "Sobek_Tile_Pit", (x, y) => Edge(x, y, 2) ? H("3A2A1C") : Noise(x, y, H("1E1610"), 0.05f), log);
        theme.stone = Png(theme.stone, "Sobek_Tile_Stone", (x, y) => Circle(x, y, 13f) ? Noise(x, y, H("9E9480"), 0.06f) : H("6E9FC8"), log);
        theme.stairLow = Png(theme.stairLow, "Sobek_Tile_Stair01", (x, y) => Stairs(x, y, H("B08A52")), log);
        theme.stairHigh = Png(theme.stairHigh, "Sobek_Tile_Stair12", (x, y) => Stairs(x, y, H("CDA468")), log);
        theme.raft = Png(theme.raft, "Sobek_Tile_Raft", (x, y) => (x % 8 == 0) ? H("5A4024") : (Edge(x, y, 1) ? H("4A3420") : H("8B6A3E")), log);
        theme.island = Png(theme.island, "Sobek_Tile_Island", (x, y) => Edge(x, y, 2) ? H("8A6A3A") : Noise(x, y, H("CDAE70"), 0.05f), log);
        theme.bridgeUp = Png(theme.bridgeUp, "Sobek_Tile_BridgeUp", (x, y) => Scales(x, y, H("6E9A3A"), H("4E7424")), log);
        theme.bridgeDown = Png(theme.bridgeDown, "Sobek_Tile_BridgeDown", (x, y) => Circle(x, y, 9f) ? H("4E7424") * 0.8f : WavesColor(x, y), log);
        theme.plateRotate = Png(theme.plateRotate, "Sobek_Plate_Rotate", (x, y) => Plate(x, y), log);
        theme.plateLift = Png(theme.plateLift, "Sobek_Plate_Lift", (x, y) => Plate(x, y), log);
        theme.plateWater = Png(theme.plateWater, "Sobek_Plate_Water", (x, y) => Plate(x, y), log);
        theme.plateMirror = Png(theme.plateMirror, "Sobek_Plate_Mirror", (x, y) => Plate(x, y), log);
        theme.egg = Png(theme.egg, "Sobek_Obj_Egg", (x, y) => Ellipse(x, y, 9f, 12f) ? (Ellipse(x, y, 7.5f, 10.5f) ? Noise(x, y, H("F2EEE2"), 0.04f) : H("6F675A")) : Color.clear, log);
        theme.goal = Png(theme.goal, "Sobek_Obj_Goal", (x, y) => Diamond(x, y, 12f) ? (Diamond(x, y, 9f) ? Color.white : H("B07C1E")) : Color.clear, log);
        theme.gate = Png(theme.gate, "Sobek_Obj_Gate", (x, y) => Circle(x, y, 14f) && !Circle(x, y, 9f) ? Color.white : Color.clear, log);
        theme.altar = Png(theme.altar, "Sobek_Obj_Altar", (x, y) => Circle(x, y, 13f) ? (Circle(x, y, 8f) ? Color.white : H("D9A03A")) : Color.clear, log);
        Png(null, "Sobek_Obj_Door", (x, y) => Edge(x, y, 3) ? H("4E7424") : (y > 20 && (x / 4) % 2 == 0 ? Color.white : H("6E5A3A")), log);
        Png(null, "Sobek_Obj_EntranceStone", (x, y) => Diamond(x, y, 13f) ? (Diamond(x, y, 10f) ? H("C9C0A8") : H("6F675A")) : Color.clear, log);
        EditorUtility.SetDirty(theme);
        return theme;
    }

    public static Sprite LoadArt(string fileName) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{fileName}.png");

    private delegate Color Painter(int x, int y);

    // PNG가 없을 때만 만들고, 테마 칸이 비었을 때만 연결 (아트가 들어간 칸은 그대로)
    private static Sprite Png(Sprite current, string fileName, Painter paint, List<string> log, bool tiled = false)
    {
        string path = $"{ArtFolder}/{fileName}.png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    tex.SetPixel(x, y, paint(x, y));
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            log.Add($"임시 그림 {fileName}.png");
        }
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        var current0 = new TextureImporterSettings();
        if (imp != null) imp.ReadTextureSettings(current0);
        if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spritePixelsPerUnit != 32 ||
                            imp.filterMode != FilterMode.Point || current0.spriteMeshType != SpriteMeshType.FullRect))
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 32;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // 타일(Tiled) 그리기용
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
        }
        if (current != null) return current;
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---- 간단한 도트 붓 ----
    private static Color H(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }
    private static bool Edge(int x, int y, int w) => x < w || y < w || x > 31 - w || y > 31 - w;
    private static bool Circle(int x, int y, float r) { float dx = x - 15.5f, dy = y - 15.5f; return dx * dx + dy * dy < r * r; }
    private static bool Ellipse(int x, int y, float rx, float ry) { float dx = (x - 15.5f) / rx, dy = (y - 15.5f) / ry; return dx * dx + dy * dy < 1f; }
    private static bool Diamond(int x, int y, float r) => Mathf.Abs(x - 15.5f) + Mathf.Abs(y - 15.5f) < r;
    private static Color Noise(int x, int y, Color c, float amt)
    {
        float n = ((x * 73856093) ^ (y * 19349663)) % 100 / 100f - 0.5f;
        return new Color(c.r + n * amt, c.g + n * amt, c.b + n * amt, 1f);
    }
    private static Color Bricks(int x, int y)
    {
        bool mortar = y % 8 == 0 || ((x + (y / 8 % 2) * 8) % 16 == 0);
        return mortar ? H("4A4540") : Noise(x, y, H("7A746A"), 0.08f);
    }
    private static Color WavesColor(int x, int y) => ((y + (int)(Mathf.Sin(x * 0.4f) * 2f)) % 8 == 0) ? H("9CC9F0") : H("5E8FB8");
    private static Color Waves(int x, int y) => WavesColor(x, y);
    private static Color Stairs(int x, int y, Color c) => (y % 8 < 2) ? c * 0.7f : c;
    private static Color Scales(int x, int y, Color a, Color b) => ((x / 4 + y / 4) % 2 == 0) ? a : b;
    private static Color Plate(int x, int y)
    {
        if (!Circle(x, y, 14f)) return Color.clear;
        return Circle(x, y, 11f) ? Color.white : new Color(0.75f, 0.75f, 0.75f);
    }

    // ================= 2. 퍼즐 데이터 =================
    private static void EnsureBoards(List<string> log)
    {
        // S1 소용돌이 섬 (최단 11걸음, 회전 3번)
        Make(log, S1, "S1 소용돌이 섬",
            "회 발판을 밟을 때마다 섬이 90° 돈다. 맞물린 섬은 반대로 돈다.",
            new[] { "~~~~~~.~~", "~~~.~~.~~", "~~~~~~~~~", "..~~~~~~~", "~~~~~~~~~", "~~~~~~.~~", "~~~~~~.~~" },
            V(0, 3), V(-1, 3), V(6, 0), SobekBoardData.GoalKind.Goal, true, 1, 0, false, false,
            new List<Vector2Int>(),
            new List<SobekBoardData.IslandDef> { Island(3, 3, false, false, true, true), Island(6, 3, true, true, false, false) },
            new List<SobekBoardData.PlateDef> { Rotate(3, 3, Spin(0, true), Spin(1, false)), Rotate(6, 3, Spin(1, true), Spin(0, false)) },
            new List<SobekBoardData.BridgeDef>(),
            SobekIds.AmuletRotate, SobekIds.FlagS1Clear, 0f, 3,
            "한 번 돌린 뒤 북쪽 팔에 잠깐 내렸다가 다시 가운데를 밟아 보자.");

        // S2 악어 등 다리 (최단 19걸음)
        Make(log, S2, "S2 악어 등 다리",
            "글자 발판에 누가 올라가 있는 동안만 같은 글자의 악어 다리가 떠 있다. 돌 악어알은 밀기만 된다.",
            new[] { "..~...~..", "..~...~..", "..~...~..", "..~...~..", "..~...~..", "..~...~..", "..~...~.." },
            V(1, 6), V(1, 7), V(8, 4), SobekBoardData.GoalKind.Goal, true, 1, 0, false, false,
            new List<Vector2Int> { V(0, 2), V(1, 4) },
            new List<SobekBoardData.IslandDef>(),
            new List<SobekBoardData.PlateDef> { Lift(0, 0, "X"), Lift(4, 1, "Y") },
            new List<SobekBoardData.BridgeDef> { Bridge(2, 2, "X"), Bridge(6, 4, "Y") },
            SobekIds.AmuletLift, SobekIds.FlagS2Clear, 0f, 3,
            "알 하나는 X 발판에 올려 두고, 다른 알을 다리 X 너머로 밀어 Y 발판에 올리자.");

        // S3 범람하는 나일 (최단 13걸음, 수위 3번)
        Make(log, S3, "S3 범람하는 나일",
            "물 발판: 낮음 → 중간 → 높음 → 낮음. 뗏목은 물을 따라 뜨고, 높이가 다르면 계단으로만 오간다.",
            new[] { "########", "#000~22#", "0000F22#", "#000F#b#", "#000~a1#", "#####00#", "########" },
            V(1, 2), V(-1, 2), V(5, 5), SobekBoardData.GoalKind.Goal, true, 3, 0, true, false,
            new List<Vector2Int>(),
            new List<SobekBoardData.IslandDef>(),
            new List<SobekBoardData.PlateDef> { Water(4, 3), Water(6, 2) },
            new List<SobekBoardData.BridgeDef>(),
            SobekIds.AmuletWater, SobekIds.FlagS3Clear, 0f, 3,
            "뗏목 위에서 물을 두 번 올려 높은 언덕으로 건너간 뒤, 언덕 발판으로 물을 빼자.");

        // S4 두 개의 수면 (최단 11걸음, 거울 2번)
        Make(log, S4, "S4 두 개의 수면",
            "거 발판을 밟으면 네 자리는 그대로, 방이 좌우로 뒤집힌다.",
            new[] { "#########", "..#.....#", "#....##.#", "#....#.##", "#...#.#.#", "#########" },
            V(1, 1), V(-1, 1), V(7, 4), SobekBoardData.GoalKind.Goal, true, 1, 0, false, false,
            new List<Vector2Int>(),
            new List<SobekBoardData.IslandDef>(),
            new List<SobekBoardData.PlateDef> { Mirror(1, 4), Mirror(1, 2) },
            new List<SobekBoardData.BridgeDef>(),
            SobekIds.AmuletMirror, SobekIds.FlagS4Clear, 0f, 3,
            "거울 세계에서 보이는 목표는 가짜. 진짜 목표는 반대쪽 거울 발판 밑에 있다.");

        // 메인 A 범람기 (최단 34걸음)
        Make(log, MainA, "셰데트 신전 · 범람기",
            "물이 높다. 물 발판으로 물을 빼면 디딤돌이 드러난다.",
            new[] { "#############", "#~~~....~~~~#", "#~~~....~~~~#", "#~~~~s..~~~~#", "#...~s~~~~~~#", "#...~..~~~.~#", "#...~..~~~.~#", "#...~..~~~~~#", "#############" },
            V(1, 7), V(1, 7), V(10, 6), SobekBoardData.GoalKind.Gate, false, 2, 1, false, false,
            new List<Vector2Int> { V(2, 5), V(2, 6) },
            new List<SobekBoardData.IslandDef> { Island(10, 3, true, false, false, true) },
            new List<SobekBoardData.PlateDef> { Lift(1, 4, "X"), Lift(7, 1, "Y"), Water(6, 7), Rotate(10, 3, Spin(0, true)) },
            new List<SobekBoardData.BridgeDef> { Bridge(4, 6, "X"), Bridge(4, 7, "X"), Bridge(8, 3, "Y") },
            "", SobekIds.FlagMainADone, 300f, 3,
            "물을 빼기 전에 알을 디딤돌 쪽으로 밀면 빠진다. 다리 X는 두 칸이라 돌아서 밀 수 있다.");

        // 메인 B 건기 (최단 19걸음)
        Make(log, MainB, "셰데트 신전 · 건기",
            "물이 빠진 아래층. 제단은 벽에 둘러싸여 있다.",
            new[] { "#############", "#.....#.....#", "#.....##.##.#", "######~#.####", "#...~~~~.##.#", "#...##~#.####", "########....#", "###########.#", "#############" },
            V(1, 1), V(1, 1), V(11, 4), SobekBoardData.GoalKind.Altar, false, 1, 0, false, true,
            new List<Vector2Int> { V(2, 2) },
            new List<SobekBoardData.IslandDef> { Island(6, 4, true, true, false, false) },
            new List<SobekBoardData.PlateDef> { Lift(4, 2, "B"), Rotate(6, 4, Spin(0, true)), Mirror(1, 4), Mirror(5, 1) },
            new List<SobekBoardData.BridgeDef> { Bridge(4, 4, "B") },
            "", "", 300f, 3,
            "알을 B 발판에 올려 다리를 먼저 띄운 뒤 거울 세계로 넘어가자.");
    }

    private static void Make(List<string> log, string assetName, string displayName, string intro, string[] rows,
        Vector2Int start, Vector2Int entrance, Vector2Int goal, SobekBoardData.GoalKind kind, bool allowExit,
        int levels, int level0, bool terraces, bool dry, List<Vector2Int> eggs,
        List<SobekBoardData.IslandDef> islands, List<SobekBoardData.PlateDef> plates, List<SobekBoardData.BridgeDef> bridges,
        string reward, string flag, float timeLimit, int hintResets, string hint)
    {
        string path = $"{BoardFolder}/{assetName}.asset";
        var data = AssetDatabase.LoadAssetAtPath<SobekBoardData>(path);
        if (data != null) { log.Add($"{assetName} 이미 있음 (그대로 둠)"); return; }

        data = ScriptableObject.CreateInstance<SobekBoardData>();
        data.EditorSet(displayName, intro, rows, start, entrance, goal, kind, allowExit, levels, level0, terraces, dry,
            eggs, islands, plates, bridges, reward, flag, timeLimit, hintResets, hint);
        AssetDatabase.CreateAsset(data, path);

        List<int> sol = new SobekBoardLogic(data.ToSpec()).Solve();
        log.Add($"{assetName} 만듦 -> 최단 {(sol == null ? "정답 없음!" : sol.Count + "걸음")}");
    }

    private static Vector2Int V(int x, int y) => new Vector2Int(x, y);

    private static SobekBoardData.IslandDef Island(int x, int y, bool n, bool e, bool s, bool w) =>
        new SobekBoardData.IslandDef { center = V(x, y), north = n, east = e, south = s, west = w };

    private static SobekBoardData.SpinDef Spin(int island, bool cw) => new SobekBoardData.SpinDef { island = island, clockwise = cw };

    private static SobekBoardData.PlateDef Rotate(int x, int y, params SobekBoardData.SpinDef[] spins) =>
        new SobekBoardData.PlateDef { cell = V(x, y), kind = SobekPlateKind.Rotate, spins = new List<SobekBoardData.SpinDef>(spins) };

    private static SobekBoardData.PlateDef Lift(int x, int y, string id) =>
        new SobekBoardData.PlateDef { cell = V(x, y), kind = SobekPlateKind.Lift, bridgeId = id };

    private static SobekBoardData.PlateDef Water(int x, int y) =>
        new SobekBoardData.PlateDef { cell = V(x, y), kind = SobekPlateKind.Water };

    private static SobekBoardData.PlateDef Mirror(int x, int y) =>
        new SobekBoardData.PlateDef { cell = V(x, y), kind = SobekPlateKind.Mirror };

    private static SobekBoardData.BridgeDef Bridge(int x, int y, string id) => new SobekBoardData.BridgeDef { cell = V(x, y), id = id };

    // ================= 3. 아이템 카탈로그 =================
    private struct Def
    {
        public string id, name, desc; public bool hidden;
        public Def(string i, string n, string d, bool h) { id = i; name = n; desc = d; hidden = h; }
    }

    private static readonly Def[] Items =
    {
        new Def(SobekIds.AmuletRotate, "소용돌이 부적", "나일의 소용돌이를 다스리는 악어 부적", false),
        new Def(SobekIds.AmuletLift, "악어 등 부적", "등을 내어 주는 소베크의 악어 부적", false),
        new Def(SobekIds.AmuletWater, "범람 부적", "나일의 범람을 담은 악어 부적", false),
        new Def(SobekIds.AmuletMirror, "수면 부적", "물 위와 물속을 비추는 악어 부적", false),
        new Def(SobekIds.Plate, "악어 형판", "큐브의 다섯 번째 면에 끼우는 소베크의 형판", false),
        new Def(SobekIds.Tooth, "소베크의 이빨", "메인 퍼즐을 한 번 더 도전할 수 있게 해 주는 악어 이빨", true),
    };

    private static void EnsureCatalog(List<string> log)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
        if (catalog == null) { log.Add($"{CatalogPath} 없음 -> 아이템 추가 건너뜀"); return; }

        var so = new SerializedObject(catalog);
        SerializedProperty list = so.FindProperty("items");
        if (list == null) { log.Add("ItemCatalog에 items 목록이 없음 -> 건너뜀"); return; }
        var have = new HashSet<string>();
        for (int i = 0; i < list.arraySize; i++) have.Add(list.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue);

        foreach (Def d in Items)
        {
            if (have.Contains(d.id)) { log.Add($"{d.id} 이미 있음"); continue; }
            list.arraySize++;
            SerializedProperty e = list.GetArrayElementAtIndex(list.arraySize - 1);
            e.FindPropertyRelative("id").stringValue = d.id;
            e.FindPropertyRelative("displayName").stringValue = d.name;
            e.FindPropertyRelative("description").stringValue = d.desc;
            e.FindPropertyRelative("hidden").boolValue = d.hidden;
            e.FindPropertyRelative("icon").objectReferenceValue = null;
            log.Add($"아이템 {d.id} ({d.name}) 추가");
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(catalog);
    }

    // ================= 공용 =================
    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    [MenuItem("Tools/Sobek/모든 판 정답 확인 (BFS)", priority = 20)]
    public static void CheckAll()
    {
        var lines = new List<string>();
        foreach (string n in new[] { S1, S2, S3, S4, MainA, MainB })
        {
            SobekBoardData d = LoadBoard(n);
            if (d == null) { lines.Add($"{n}: 에셋 없음 (1번 메뉴 먼저)"); continue; }
            List<int> sol = new SobekBoardLogic(d.ToSpec()).Solve();
            lines.Add($"{d.DisplayName}: {(sol == null ? "정답 없음!" : sol.Count + "걸음")}");
        }
        Debug.Log("[Sobek] 정답 확인 (기획서: S1 11 · S2 19 · S3 13 · S4 11 · A 34 · B 19)\n- " + string.Join("\n- ", lines));
    }
}
#endif
