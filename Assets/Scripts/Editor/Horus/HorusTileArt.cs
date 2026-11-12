using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// 호루스 전용 타일 (밤하늘 신전) 만들기 / 불러오기 (HorusRoomMap이 부름)
// - 그림: Assets/image/Horus/Tile/HO_*.png (32 x 32 도트, 코드로 그린 임시 아트 -> 진짜 아트가 오면 같은 이름으로 덮어쓰기)
// - 타일 에셋: Assets/Tile/Horus/HO_*.asset (없으면 그림으로 자동 생성)
// - 그림 설정: Sprite, 32px = 1칸, Point(도트 그대로), 압축 없음
public static class HorusTileArt
{
    private const string ImageDir = "Assets/image/Horus/Tile/";
    private const string TileDir = "Assets/Tile/Horus";

    public const string FloorA = "HO_Floor_A";
    public const string FloorB = "HO_Floor_B";       // 금 별가루
    public const string FloorC = "HO_Floor_C";       // 금 간 바닥
    public const string FloorStar = "HO_Floor_Star"; // 행렬길 / 제단 고리
    public const string FloorAnte = "HO_Floor_Ante"; // 입구 전실 (모래빛 돌)
    public const string WallTop = "HO_Wall_Top";
    public const string WallFace = "HO_Wall_Face";
    public const string WallFaceEye = "HO_Wall_Face_Eye";
    public const string WallFaceFalcon = "HO_Wall_Face_Falcon";
    public const string WallFaceLow = "HO_Wall_Face_Low";   // [추가] 2줄 벽의 아랫줄 (금 띠 없음)

    // [추가] 입구 복도의 찢긴 벽화 (4칸 x 2줄 한 점). n 1~4, col 0~3, row 0(위)~1
    public static string Mural(int n, int col, int row) => $"HO_Wall_Mural{n}_{col}_{row}";

    public static TileBase Get(string name)
    {
        string tilePath = $"{TileDir}/{name}.asset";
        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(tilePath);
        if (tile != null) return tile;
        Sprite sprite = SpriteOf(name);
        if (sprite == null)
        {
            Debug.LogWarning($"[HorusTileArt] 타일 그림 없음: {ImageDir}{name}.png");
            return null;
        }
        PrologueAssetFinder.EnsureFolder(TileDir);
        Tile created = ScriptableObject.CreateInstance<Tile>();
        created.sprite = sprite;
        AssetDatabase.CreateAsset(created, tilePath);
        return created;
    }

    public static Sprite SpriteOf(string name)
    {
        string path = ImageDir + name + ".png";
        ImportAsPixelTile(path);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void ImportAsPixelTile(string path)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter imp)) return;
        bool ok = imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single
                  && Mathf.Approximately(imp.spritePixelsPerUnit, 32f) && imp.filterMode == FilterMode.Point
                  && imp.textureCompression == TextureImporterCompression.Uncompressed;
        if (ok) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 32f;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.SaveAndReimport();
    }
}
