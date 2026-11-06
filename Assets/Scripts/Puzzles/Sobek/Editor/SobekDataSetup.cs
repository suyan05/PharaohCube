#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 메뉴: Tools/Sobek/1. 데이터 · 임시 그림 · 아이템 만들기
//  1) 임시 그림 PNG (Assets/image/Sobek/Placeholder) + 그림 묶음 SobekTheme
//  2) 퍼즐 데이터 7개 (서브 S1~S4, 메인 구역 1~3) -> 모두 BFS로 정답·막힘 상태를 검증한 맵
//  3) 팀 ItemCatalog에 소베크 아이템 추가 (부적 4, 형판, 이빨)
// 여러 번 눌러도 안전: 이미 판 데이터가 있으면 덮어쓸지 물어봄 (같은 에셋을 고쳐 쓰므로 씬 연결은 유지)
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
    public const string MainC = "SobekBoard_MainC_Tower";
    public static readonly string[] AllBoards = { S1, S2, S3, S4, MainA, MainB, MainC };

    [MenuItem("Tools/Sobek/1. 데이터 · 임시 그림 · 아이템 만들기", priority = 1)]
    public static void RunMenu() => Run(true);

    // ask: 이미 있는 판 데이터를 새 설계로 덮어쓸지 물어봄 (아니면 그대로 둠)
    public static void Run(bool ask = false)
    {
        var log = new List<string>();
        EnsureFolder(BoardFolder);
        EnsureFolder(ArtFolder);
        bool any = false;
        foreach (string n in AllBoards) if (LoadBoard(n) != null) any = true;
        bool overwrite = !any || (ask && EditorUtility.DisplayDialog("소베크 퍼즐 데이터",
            "이미 판 데이터가 있습니다.\n새 설계(v2: 큰 서브 4판 + 메인 3구역)로 덮어쓸까요?\n(직접 고친 값은 사라집니다. 씬 연결은 유지됩니다)", "덮어쓰기", "그대로 두기"));
        SobekTheme theme = EnsureTheme(log);
        EnsureBoards(log, overwrite);
        EnsureSteleData(log);
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
        // v2: 발판에 글자 대신 그림 아이콘 (회전 화살표 / 위 화살표 / 물결 / 거울 축)
        theme.plateRotate = Upgrade(theme.plateRotate, Png(null, "Sobek_Plate_Rotate_v2", (x, y) => PlateIcon(x, y, IconRotate(x, y)), log));
        theme.plateLift = Upgrade(theme.plateLift, Png(null, "Sobek_Plate_Lift_v2", (x, y) => PlateIcon(x, y, IconLift(x, y)), log));
        theme.plateWater = Upgrade(theme.plateWater, Png(null, "Sobek_Plate_Water_v2", (x, y) => PlateIcon(x, y, IconWater(x, y)), log));
        theme.plateMirror = Upgrade(theme.plateMirror, Png(null, "Sobek_Plate_Mirror_v2", (x, y) => PlateIcon(x, y, IconMirror(x, y)), log));
        theme.egg = Png(theme.egg, "Sobek_Obj_Egg", (x, y) => Ellipse(x, y, 9f, 12f) ? (Ellipse(x, y, 7.5f, 10.5f) ? Noise(x, y, H("F2EEE2"), 0.04f) : H("6F675A")) : Color.clear, log);
        theme.goal = Png(theme.goal, "Sobek_Obj_Goal", (x, y) => Diamond(x, y, 12f) ? (Diamond(x, y, 9f) ? Color.white : H("B07C1E")) : Color.clear, log);
        theme.gate = Png(theme.gate, "Sobek_Obj_Gate", (x, y) => Circle(x, y, 14f) && !Circle(x, y, 9f) ? Color.white : Color.clear, log);
        theme.altar = Png(theme.altar, "Sobek_Obj_Altar", (x, y) => Circle(x, y, 13f) ? (Circle(x, y, 8f) ? Color.white : H("D9A03A")) : Color.clear, log);
        Png(null, "Sobek_Obj_Door", (x, y) => Edge(x, y, 3) ? H("4E7424") : (y > 20 && (x / 4) % 2 == 0 ? Color.white : H("6E5A3A")), log);
        Png(null, "Sobek_Obj_EntranceStone", (x, y) => Diamond(x, y, 13f) ? (Diamond(x, y, 10f) ? H("C9C0A8") : H("6F675A")) : Color.clear, log);
        ApplyWorldArt(theme, log);
        EditorUtility.SetDirty(theme);
        return theme;
    }

    // ================= v3: 소베크 도트 그림 (Assets/image/Sobek/World, UI) =================
    public const string WorldFolder = "Assets/image/Sobek/World";
    public const string UiFolder = "Assets/image/Sobek/UI";

    public static Sprite World(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{WorldFolder}/{name}.png");
    public static Sprite Ui(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiFolder}/{name}.png");

    // 임시 그림이 들어 있던 칸만 소베크 그림으로 교체 (아트팀이 직접 넣은 그림은 그대로)
    private static void ApplyWorldArt(SobekTheme theme, List<string> log)
    {
        int fixedCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { WorldFolder, UiFolder }))
            if (FixImport(AssetDatabase.GUIDToAssetPath(guid))) fixedCount++;
        if (fixedCount > 0) log.Add($"소베크 그림 가져오기 설정 {fixedCount}개 (32px, 도트)");

        theme.floor = Upgrade(theme.floor, World("Sobek_Tile_FloorTint"));
        theme.wall = Upgrade(theme.wall, World("Sobek_Tile_Wall"));
        theme.water = Upgrade(theme.water, World("Sobek_Tile_Water"));
        theme.pit = Upgrade(theme.pit, World("Sobek_Tile_Pit"));
        theme.stone = Upgrade(theme.stone, World("Sobek_Tile_Stone"));
        theme.stairLow = Upgrade(theme.stairLow, World("Sobek_Tile_Stair01"));
        theme.stairHigh = Upgrade(theme.stairHigh, World("Sobek_Tile_Stair12"));
        theme.raft = Upgrade(theme.raft, World("Sobek_Tile_Raft"));
        theme.island = Upgrade(theme.island, World("Sobek_Tile_Island"));
        theme.bridgeUp = Upgrade(theme.bridgeUp, World("Sobek_Tile_BridgeUp"));
        theme.bridgeDown = Upgrade(theme.bridgeDown, World("Sobek_Tile_BridgeDown"));
        theme.plateRotate = Upgrade(theme.plateRotate, World("Sobek_Plate_Rotate"));
        theme.plateLift = Upgrade(theme.plateLift, World("Sobek_Plate_Lift"));
        theme.plateWater = Upgrade(theme.plateWater, World("Sobek_Plate_Water"));
        theme.plateMirror = Upgrade(theme.plateMirror, World("Sobek_Plate_Mirror"));
        theme.egg = Upgrade(theme.egg, World("Sobek_Obj_Egg"));
        theme.goal = Upgrade(theme.goal, World("Sobek_Obj_Goal"));
        theme.gate = Upgrade(theme.gate, World("Sobek_Obj_Gate"));
        theme.altar = Upgrade(theme.altar, World("Sobek_Obj_Altar"));
        // 새 그림은 색이 들어 있으므로 바닥 높이 색만 연하게
        theme.floorTop = new Color(1f, 0.97f, 0.9f);
        theme.floorLow = new Color(0.72f, 0.6f, 0.42f);
        log.Add("SobekTheme: 소베크 도트 그림 연결");
    }

    // 도트 그림 가져오기 설정: World = 32px/유닛·점 필터·FullRect, UI = 기본 Sprite
    private static bool FixImport(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return false;
        bool world = path.StartsWith(WorldFolder);
        var st = new TextureImporterSettings();
        imp.ReadTextureSettings(st);
        bool ok = imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single &&
                  (!world || (imp.spritePixelsPerUnit == 32 && imp.filterMode == FilterMode.Point && st.spriteMeshType == SpriteMeshType.FullRect));
        if (ok) return false;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.mipmapEnabled = false;
        if (world)
        {
            imp.spritePixelsPerUnit = 32;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            // 키 큰 물건(비석·기둥·석상)은 발밑 기준점
            if (path.Contains("_Obj_Stele") || path.Contains("_Obj_Gauge") || path.Contains("_Obj_Pillar") || path.Contains("_Obj_Statue"))
            {
                st.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            }
            imp.SetTextureSettings(st);
        }
        imp.SaveAndReimport();
        return true;
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

    // 임시 그림(Placeholder 폴더)이 들어 있던 칸만 새 그림으로 교체 (아트가 직접 넣은 그림은 그대로)
    private static Sprite Upgrade(Sprite current, Sprite placeholder)
    {
        if (current == null) return placeholder;
        string path = AssetDatabase.GetAssetPath(current);
        return path.StartsWith(ArtFolder) ? placeholder : current;
    }

    // 발판 바탕(흰 원) + 진한 아이콘. 흰 부분은 발판 색이 곱해지고 아이콘은 어둡게 남음
    private static Color PlateIcon(int x, int y, bool icon)
    {
        if (!Circle(x, y, 15f)) return Color.clear;
        if (!Circle(x, y, 13f)) return new Color(0.55f, 0.55f, 0.55f);
        return icon ? new Color(0.18f, 0.16f, 0.14f) : Color.white;
    }

    private static bool IconRotate(int x, int y)
    {
        float dx = x - 15.5f, dy = y - 15.5f;
        float r = Mathf.Sqrt(dx * dx + dy * dy);
        float a = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg; // -180~180
        bool ring = r > 6f && r < 9f && !(a > 50f && a < 110f);      // 위쪽이 끊긴 고리
        bool head = x >= 18 && x <= 24 && y >= 21 && y <= 27 && (x - 18) >= (y - 21) - 3 && (x - 18) <= 6 - Mathf.Abs(y - 24); // 끊긴 자리 화살촉
        return ring || head;
    }

    private static bool IconLift(int x, int y)
    {
        bool shaft = x >= 14 && x <= 17 && y >= 7 && y <= 19;
        bool head = y >= 18 && y <= 25 && Mathf.Abs(x - 15.5f) <= (25 - y) + 0.5f;
        return shaft || head;
    }

    private static bool IconWater(int x, int y)
    {
        for (int k = 0; k < 2; k++)
        {
            float wy = 12f + k * 7f + Mathf.Sin(x * 0.7f) * 1.6f;
            if (x >= 7 && x <= 24 && Mathf.Abs(y - wy) < 1.3f) return true;
        }
        return false;
    }

    private static bool IconMirror(int x, int y)
    {
        bool axis = x >= 15 && x <= 16 && y >= 6 && y <= 25;
        bool left = x >= 6 && x <= 12 && Mathf.Abs(y - 15.5f) <= (x - 6) * 0.9f;   // ◁
        bool right = x >= 19 && x <= 25 && Mathf.Abs(y - 15.5f) <= (25 - x) * 0.9f; // ▷
        return axis || left || right;
    }

    // ================= 2. 퍼즐 데이터 =================
    private static void EnsureBoards(List<string> log, bool overwrite)
    {
        // 아래 맵은 모두 정답 탐색(BFS)으로 검증한 값 (어려움 v4)
        // 최단: S1 67 · S2 72 · S3 41 · S4 27 · 메인 58 + 29 + 65 / 서브 힌트는 처음으로 6번 뒤, 메인은 힌트 없음
        Make(log, overwrite, S1, "S1 소용돌이 섬",
            "회전 발판을 밟을 때마다 이어진 섬이 90° 돈다. 섬 위의 알도 함께 돈다.",
            new[] { "~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~",
                "~~~~~~~~~~~..",
                "~~~~.~~~.~...",
                "..~~~~~~.~~~~",
                ".~~.~~~~~~~~~",
                "~~.~.~~~.~.~~",
                "~~.~~~~~~.~~~",
                "~~~.~~~~~~.~~" },
            V(0, 4), V(-1, 4), V(12, 3), SobekBoardData.GoalKind.Goal, true, 1, 0, false, false,
            new List<Vector2Int> { V(4, 6), V(8, 6) },
            new List<SobekBoardData.IslandDef> { Island(6, 4, 0, 1, 2, 0), Island(9, 6, 2, 2, 0, 0), Island(3, 5, 1, 1, 2, 2), Island(10, 2, 0, 2, 1, 1) },
            new List<SobekBoardData.PlateDef> { Rotate(6, 4, Spin(0, true)), Rotate(9, 6, Spin(1, true), Spin(3, true)), Rotate(3, 5, Spin(2, true)), Rotate(10, 2, Spin(1, true)), Lift(2, 7, "X") },
            new List<SobekBoardData.BridgeDef> { Bridge(0, 0, "X") },
            SobekIds.AmuletRotate, SobekIds.FlagS1Clear, 0f, 6,
            "회전 발판은 한 번 밟을 때마다 돈다. 알도 섬과 함께 돈다.");

        Make(log, overwrite, S2, "S2 악어 등 다리",
            "글자 발판에 누가 올라가 있는 동안만 같은 글자의 악어 다리가 떠 있다. 돌 악어알은 밀기만 된다.",
            new[] { "...~...~..~..",
                "...~...~..~..",
                "...~...~..~..",
                "...~...~..~..",
                "...~...~..~..",
                "...~...~..~..",
                "...~...~..~..",
                "...~...~..~..",
                "...~...~..~.." },
            V(1, 8), V(1, 9), V(12, 1), SobekBoardData.GoalKind.Goal, true, 1, 0, false, false,
            new List<Vector2Int> { V(2, 6), V(2, 2), V(0, 2) },
            new List<SobekBoardData.IslandDef> {  },
            new List<SobekBoardData.PlateDef> { Lift(0, 8, "X"), Lift(6, 7, "Y"), Lift(9, 3, "Z") },
            new List<SobekBoardData.BridgeDef> { Bridge(3, 2, "X"), Bridge(7, 3, "Y"), Bridge(10, 7, "Z") },
            SobekIds.AmuletLift, SobekIds.FlagS2Clear, 0f, 6,
            "벽에 붙인 알은 다시 꺼낼 수 없다.");

        Make(log, overwrite, S3, "S3 범람하는 나일",
            "물 발판: 낮음 → 중간 → 높음 → 낮음. 뗏목은 물 높이를 따라 오르내리고, 높이가 다르면 계단으로만 오간다.",
            new[] { "~11222~~~112",
                "#1112#~~11F2",
                "#1112221110~",
                "200F122b1000",
                "22221#2~#000",
                "2222222~0000",
                "22~2~222~000" },
            V(1, 3), V(-1, 3), V(8, 5), SobekBoardData.GoalKind.Goal, true, 3, 0, true, false,
            new List<Vector2Int> { V(2, 5), V(4, 5) },
            new List<SobekBoardData.IslandDef> {  },
            new List<SobekBoardData.PlateDef> { Water(3, 3), Water(5, 0), Water(10, 1) },
            new List<SobekBoardData.BridgeDef> {  },
            SobekIds.AmuletWater, SobekIds.FlagS3Clear, 0f, 6,
            "뗏목이 있는 높이에서만 뗏목에 오를 수 있다.");

        Make(log, overwrite, S4, "S4 두 개의 수면",
            "거울 발판을 밟으면 네 자리는 그대로, 방이 가운데 축을 기준으로 좌우로 뒤집힌다. 돌 악어알이 길을 막는다.",
            new[] { "##~.#.###~#~#",
                "#.....#~~#..#",
                "......####..#",
                "~.###.#..#..#",
                ".........#.##",
                "...~~.~..~.#~",
                "#..~#~##.#..#",
                "....#~#.....#",
                "#.#~##~##.#~~" },
            V(0, 7), V(-1, 7), V(11, 1), SobekBoardData.GoalKind.Goal, true, 1, 0, false, false,
            new List<Vector2Int> { V(10, 6), V(4, 4) },
            new List<SobekBoardData.IslandDef> {  },
            new List<SobekBoardData.PlateDef> { Lift(1, 4, "X"), Mirror(1, 6), Mirror(12, 1), Mirror(7, 6), Mirror(7, 3) },
            new List<SobekBoardData.BridgeDef> { Bridge(10, 4, "X") },
            SobekIds.AmuletMirror, SobekIds.FlagS4Clear, 0f, 6,
            "뒤집힌 뒤 내가 설 자리가 비어 있어야 한다.");

        Make(log, overwrite, MainA, "셰데트 신전 1 · 범람기",
            "물이 높다. 물 발판으로 물을 빼면 디딤돌이 드러난다.",
            new[] { "###############",
                "#~~~....~~~~###",
                "#~~~....~~~~###",
                "#~~~~s..~~~~###",
                "#...~s~~~~~~###",
                "#...~..~~~.~###",
                "#...~..~~~.~###",
                "#...~..~~~.~###",
                "##########.####",
                "#########..ss.#",
                "###############" },
            V(1, 7), V(1, 7), V(13, 9), SobekBoardData.GoalKind.Gate, false, 2, 1, false, false,
            new List<Vector2Int> { V(3, 6), V(5, 7), V(2, 5), V(2, 6) },
            new List<SobekBoardData.IslandDef> { Island(10, 3, 1, 0, 0, 1) },
            new List<SobekBoardData.PlateDef> { Lift(1, 4, "X"), Lift(7, 1, "Y"), Water(6, 7), Water(10, 8), Water(9, 9), Rotate(10, 3, Spin(0, true)) },
            new List<SobekBoardData.BridgeDef> { Bridge(4, 6, "X"), Bridge(4, 7, "X"), Bridge(8, 3, "Y") },
            "", SobekIds.FlagMainADone, 300f, 0,
            "");

        Make(log, overwrite, MainB, "셰데트 신전 2 · 건기의 미궁",
            "물이 빠진 아래층. 거울 너머의 방을 오가야 한다.",
            new[] { "###############",
                "#...#..#......#",
                "#...#..#..#...#",
                "#...#..#..#...#",
                "#####~##..#####",
                "####~~~#..#...#",
                "#...#~#####...#",
                "#...#..####~~~#",
                "#...#..####~~~#",
                "#...~..####...#",
                "###############" },
            V(1, 1), V(1, 1), V(12, 9), SobekBoardData.GoalKind.Gate, false, 1, 0, false, true,
            new List<Vector2Int> { V(2, 2), V(9, 2), V(6, 8) },
            new List<SobekBoardData.IslandDef> { Island(5, 5, 1, 1, 0, 0), Island(12, 7, 0, 1, 0, 1) },
            new List<SobekBoardData.PlateDef> { Rotate(5, 5, Spin(0, true)), Rotate(3, 8, Spin(1, true)), Lift(6, 9, "B"), Mirror(3, 3), Mirror(12, 1), Mirror(9, 4), Mirror(6, 1), Mirror(1, 6) },
            new List<SobekBoardData.BridgeDef> { Bridge(4, 9, "B") },
            "", "", 300f, 0,
            "");

        Make(log, overwrite, MainC, "셰데트 신전 3 · 소베크-라의 탑",
            "소용돌이 섬들을 돌려 소베크-라의 제단까지.",
            new[] { "~~~~~~~~~~~~s.~",
                "~~~~~~~~~~~s~s~",
                "~~~~~~~~.~~.~s.",
                ".~~~~~~~~~~.~..",
                ".~~~ss.~.~.....",
                "..~~~~~~~~~~.s~",
                ".~.~.~s~~s~.~~~",
                "~~..~s~s~~~~~~~",
                "~~sss.~~s~~~~~~",
                "~~~s~.ss~~~~~~~",
                "~~~~~.~~~~~~~~~" },
            V(0, 5), V(0, 5), V(14, 4), SobekBoardData.GoalKind.Altar, false, 2, 1, false, false,
            new List<Vector2Int> { V(6, 4), V(8, 2) },
            new List<SobekBoardData.IslandDef> { Island(6, 5, 0, 0, 2, 0), Island(9, 7, 2, 2, 0, 0), Island(3, 6, 2, 0, 1, 2), Island(10, 3, 1, 2, 1, 1) },
            new List<SobekBoardData.PlateDef> { Rotate(6, 5, Spin(0, true)), Rotate(9, 7, Spin(3, true), Spin(1, true)), Rotate(3, 6, Spin(2, true)), Rotate(10, 3, Spin(1, true), Spin(0, false)), Water(8, 4) },
            new List<SobekBoardData.BridgeDef> {  },
            "", "", 420f, 0,
            "");
    }

    private static void Make(List<string> log, bool overwrite, string assetName, string displayName, string intro, string[] rows,
        Vector2Int start, Vector2Int entrance, Vector2Int goal, SobekBoardData.GoalKind kind, bool allowExit,
        int levels, int level0, bool terraces, bool dry, List<Vector2Int> eggs,
        List<SobekBoardData.IslandDef> islands, List<SobekBoardData.PlateDef> plates, List<SobekBoardData.BridgeDef> bridges,
        string reward, string flag, float timeLimit, int hintResets, string hint)
    {
        string path = $"{BoardFolder}/{assetName}.asset";
        var data = AssetDatabase.LoadAssetAtPath<SobekBoardData>(path);
        if (data != null && !overwrite) { log.Add($"{assetName} 이미 있음 (그대로 둠)"); return; }

        bool created = data == null;
        if (created) data = ScriptableObject.CreateInstance<SobekBoardData>();
        data.EditorSet(displayName, intro, rows, start, entrance, goal, kind, allowExit, levels, level0, terraces, dry,
            eggs, islands, plates, bridges, reward, flag, timeLimit, hintResets, hint);
        if (created) AssetDatabase.CreateAsset(data, path);
        else EditorUtility.SetDirty(data); // 같은 에셋을 덮어씀 -> 씬의 연결(GUID) 유지

        List<int> sol = new SobekBoardLogic(data.ToSpec()).Solve();
        log.Add($"{assetName} 만듦 -> 최단 {(sol == null ? "정답 없음!" : sol.Count + "걸음")}");
    }

    private static Vector2Int V(int x, int y) => new Vector2Int(x, y);

    private static SobekBoardData.IslandDef Island(int x, int y, int n, int e, int s, int w) =>
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

    // ================= 비석 글 (라의 방 방식: 이야기 + 그 방 퍼즐 힌트) =================
    public const string ClueFolder = DataRoot + "/Clues";

    public static ClueData LoadClue(string id) => AssetDatabase.LoadAssetAtPath<ClueData>($"{ClueFolder}/Clue_{id}.asset");

    private static readonly string[][] SteleTexts =
    {
        // id, 제목, 본문, 벽화 그림
        new[] { "SOBEK_HALL", "셰데트, 악어의 신전",
            "밤의 일곱째 시간, 혼돈의 뱀 아펩이 저승의 강을 마셨다. 태양의 배는 마른 모래 위에 멈췄다.\n" +
            "물의 주인 소베크는 마지막 범람을 이 신전에 가두고 입을 다물었다.\n" +
            "네 개의 시험을 건너 악어 부적을 얻은 자만이 소베크의 입을 지난다.", "Sobek_UI_Clue_Hall" },
        new[] { "SOBEK_S1", "소용돌이 섬",
            "나일의 소용돌이는 소베크의 숨이다. 섬은 가운데 돌을 밟을 때마다 한 번씩 돈다.\n" +
            "섬 위에 놓인 것은 섬과 함께 돈다. 어떤 돌은 이웃 섬을 함께 돌린다.", "Sobek_UI_Clue_S1_Whirlpool" },
        new[] { "SOBEK_S2", "악어 등 다리",
            "소베크의 악어는 누군가 등을 눌러 주는 동안만 떠 있다.\n" +
            "돌알은 밀 수만 있고 당길 수 없다.", "Sobek_UI_Clue_S2_CrocBridge" },
        new[] { "SOBEK_S3", "범람하는 나일",
            "강은 낮음, 중간, 높음으로 차오르고 다시 마른다.\n" +
            "물이 땅보다 높으면 그 땅은 잠긴다. 잠긴 땅의 알은 돌아오지 않는다.", "Sobek_UI_Clue_S3_Flood" },
        new[] { "SOBEK_S4", "두 개의 수면",
            "잔잔한 수면은 신전을 좌우로 뒤집어 비춘다.\n" +
            "거울 돌을 밟으면 너는 그 자리에 서 있고, 세계만 뒤집힌다.", "Sobek_UI_Clue_S4_Mirror" },
        new[] { "SOBEK_DOOR", "소베크의 입",
            "네 부적을 악어의 입에 물리면 문이 열린다. 안쪽은 범람기, 건기의 미궁, 그리고 소베크-라의 탑.\n" +
            "강은 한 번 잘못 흐르면 스스로 돌아오지 않는다. 돌아갈 길은 네가 기억해야 한다.", "Sobek_UI_Clue_Door" },
        new[] { "SOBEK_LOWER", "물 빠진 하층",
            "강은 다시 흐른다. 이제 자칼이 마지막 길을 연다.\n" +
            "악어 머리를 한 자가 심장을 노릴 때, 소베크의 이빨이 한 번 더 기회를 준다.", "Sobek_UI_Clue_Lower" },
    };

    private static void EnsureSteleData(List<string> log)
    {
        EnsureFolder(ClueFolder);
        int made = 0;
        foreach (string[] t in SteleTexts)
        {
            string path = $"{ClueFolder}/Clue_{t[0]}.asset";
            var data = AssetDatabase.LoadAssetAtPath<ClueData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ClueData>();
                AssetDatabase.CreateAsset(data, path);
                made++;
            }
            var so = new SerializedObject(data);
            so.FindProperty("clueId").stringValue = t[0];
            SerializedProperty day = so.FindProperty("day");
            day.FindPropertyRelative("title").stringValue = t[1];
            day.FindPropertyRelative("body").stringValue = t[2];
            SerializedProperty ill = day.FindPropertyRelative("illustration");
            if (ill.objectReferenceValue == null) ill.objectReferenceValue = Ui(t[3]);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }
        log.Add($"비석 글 {SteleTexts.Length}개 ({ClueFolder}, 새로 {made}개)");
    }

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
        // 아이콘이 비어 있으면 소베크 그림 연결
        var icons = new Dictionary<string, Sprite>
        {
            { SobekIds.AmuletRotate, Ui("Common_Icon_SobekAmuletRotate") },
            { SobekIds.AmuletLift, Ui("Common_Icon_SobekAmuletLift") },
            { SobekIds.AmuletWater, Ui("Common_Icon_SobekAmuletWater") },
            { SobekIds.AmuletMirror, Ui("Common_Icon_SobekAmuletMirror") },
            { SobekIds.Tooth, Ui("Common_Icon_SobekTooth") },
            { SobekIds.Plate, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/image/Cube/Sobek.jpg") },
        };
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty e = list.GetArrayElementAtIndex(i);
            string id = e.FindPropertyRelative("id").stringValue;
            SerializedProperty icon = e.FindPropertyRelative("icon");
            if (icon != null && icon.objectReferenceValue == null && icons.TryGetValue(id, out Sprite sp) && sp != null)
            {
                icon.objectReferenceValue = sp;
                log.Add($"아이콘 연결: {id}");
            }
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
        foreach (string n in AllBoards)
        {
            SobekBoardData d = LoadBoard(n);
            if (d == null) { lines.Add($"{n}: 에셋 없음 (1번 메뉴 먼저)"); continue; }
            List<int> sol = new SobekBoardLogic(d.ToSpec()).Solve();
            lines.Add($"{d.DisplayName}: {(sol == null ? "정답 없음!" : sol.Count + "걸음")}");
        }
        Debug.Log("[Sobek] 정답 확인 (v4 어려움: S1 67 · S2 72 · S3 41 · S4 27 · 메인 58 · 29 · 65)\n- " + string.Join("\n- ", lines));
    }
}
#endif
