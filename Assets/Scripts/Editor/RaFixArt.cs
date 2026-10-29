using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// RaFixSetup 도우미: 단서 비석 밤 벽화, 일식 석탁 밤 그림
public static class RaFixArt
{
    private const string ClueFolder = "Assets/Scripts/Data/Ra_Data/";
    private const string ClueArtFolder = "Assets/image/Ra/Object/";
    private const string P3Art = "Assets/image/Ra/P3/";

    // ================= 비석 단서 그림 (비어 있는 칸만 채움) =================
    public static void ClueArt()
    {
        FillClue("Clue_RA_P3", "night", "Ra_UI_Clue_Eclipse_Night");
        FillClue("Clue_RA_P4", "night", "Ra_UI_Clue_DayCycle_Night");
        FillClue("Clue_RA_P5", "day", "Ra_UI_Clue_SunCycle");
        FillClue("Clue_RA_P5", "night", "Ra_UI_Clue_SunCycle_Night");
        AssetDatabase.SaveAssets();
    }

    private static void FillClue(string asset, string phase, string spriteName)
    {
        ClueData data = AssetDatabase.LoadAssetAtPath<ClueData>(ClueFolder + asset + ".asset");
        Sprite sprite = LoadSprite(ClueArtFolder + spriteName + ".png", false);
        if (data == null || sprite == null)
        {
            Debug.LogWarning($"[RaFixSetup] 단서 그림 못 넣음: {asset} / {spriteName}");
            return;
        }
        var so = new SerializedObject(data);
        SerializedProperty ill = so.FindProperty($"{phase}.illustration");
        if (ill == null || ill.objectReferenceValue != null) return;
        ill.objectReferenceValue = sprite;
        so.ApplyModifiedProperties();
        Debug.Log($"[RaFixSetup] {asset} {phase} 그림 = {spriteName}");
    }

    // ================= 일식 석탁 밤 그림 =================
    public static void EclipseNightArt()
    {
        EclipseArtStation station = Object.FindAnyObjectByType<EclipseArtStation>(FindObjectsInactive.Include);
        if (station == null) return;

        RaPolishSetup.Edit(station, so =>
        {
            // 여러 조각(Multiple)으로 잘려 있어서 엉뚱한 조각이 들어갔던 문제 -> 한 장(Single)으로 바꿔서 통째로
            so.FindProperty("nightBoardSprite").objectReferenceValue = LoadSprite(P3Art + "P3_Board_Base_Night.jpg", false, true);
            so.FindProperty("nightCompleteSprite").objectReferenceValue = LoadSprite(P3Art + "P3_Complete_Night.jpg", false, true);
            SerializedProperty disks = so.FindProperty("nightDiskSprites");
            disks.arraySize = 3;
            disks.GetArrayElementAtIndex(0).objectReferenceValue = LoadSprite(P3Art + "P3_Disk_BottomLeft_Night.png", true);
            disks.GetArrayElementAtIndex(1).objectReferenceValue = LoadSprite(P3Art + "P3_Disk_Top_Night.png", true);
            disks.GetArrayElementAtIndex(2).objectReferenceValue = LoadSprite(P3Art + "P3_Disk_Right_Night.png", true);
            LinkBoard(so);
        });
        Debug.Log("[RaFixSetup] 일식 석탁 밤 그림(달이 해를 삼킨 문양) 연결");
    }

    private static void LinkBoard(SerializedObject so)
    {
        var overlay = so.FindProperty("overlayPanel").objectReferenceValue as GameObject;
        Transform board = overlay != null ? overlay.transform.Find("Board") : null;
        Image img = board != null ? board.GetComponent<Image>() : null;
        if (img != null) so.FindProperty("boardImage").objectReferenceValue = img;
    }

    // ================= 퍼즐 화면 배경판 (P1 그림자 시계, P4 카드 벤치) =================
    // [수정] 파피루스 대신 단서 화면과 같은 두루마리 틀 (테두리 9-slice라 크게 늘려도 막대가 안 늘어남)
    private const string PapyrusPath = "Assets/image/ui/UI_Journal_Frame.png";
    private static readonly string[] PapyrusOverlays = { "P1_Overlay", "P4_Overlay" };

    public static void PuzzlePapyrus()
    {
        Sprite papyrus = PapyrusSprite();
        if (papyrus == null)
        {
            Debug.LogWarning($"[RaFixSetup] 파피루스 그림 없음: {PapyrusPath}");
            return;
        }
        foreach (PuzzleOverlayTheme theme in Object.FindObjectsByType<PuzzleOverlayTheme>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (System.Array.IndexOf(PapyrusOverlays, theme.name) < 0) continue;
            RaPolishSetup.Edit(theme, so =>
            {
                so.FindProperty("papyrusSprite").objectReferenceValue = papyrus;
                so.FindProperty("papyrusSize").vector2Value = new Vector2(1500f, 1080f);
            });
            Debug.Log($"[RaFixSetup] {theme.name}에 파피루스 배경판 연결");
        }
    }

    // [수정] 두루마리 틀은 단서 화면(CluePanel)도 같이 쓰는 그림이라 설정을 바꾸지 않고 그대로 가져옴
    private static Sprite PapyrusSprite()
    {
        return FirstSprite(PapyrusPath);
    }

    // 한 장짜리 Sprite로 + (원판은 클릭 판정용 Read/Write 켬)
    private static Sprite LoadSprite(string path, bool readable, bool forceSingle = false)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter imp))
        {
            Debug.LogWarning($"[RaFixSetup] 그림 없음: {path}");
            return null;
        }
        // 이미 Sprite로 쓰고 있는 단서 그림은 설정을 안 바꾸고 첫 조각을 씀 (다른 곳 연결이 끊기지 않게)
        if (!readable && !forceSingle && imp.textureType == TextureImporterType.Sprite) return FirstSprite(path);
        bool dirty = imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || (readable && !imp.isReadable);
        if (dirty)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            if (readable) imp.isReadable = true;
            imp.SaveAndReimport();
        }
        return FirstSprite(path);
    }

    private static Sprite FirstSprite(string path)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (o is Sprite s) return s;
        }
        return null;
    }
}
