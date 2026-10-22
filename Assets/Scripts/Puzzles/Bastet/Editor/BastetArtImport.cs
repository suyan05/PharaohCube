using UnityEditor;
using UnityEngine;

// [추가] 바스테트 퍼즐 그림 가져오기 설정 (자동)
// Assets/image/Bastet/Puzzle/ 폴더에 PNG를 넣으면 처음 한 번 자동으로:
// Sprite / 1칸 = 32px (PPU 32) / Point 필터 / 압축 없음 / 밉맵 끔 / 기준점(Pivot) 설정
// - 석상: 바닥 가운데 (발 위치 = 오브젝트 위치)
// - 눈 빛(_Eyes_Light): 눈 가운데 (빛 Light2D가 눈에 오도록)
// - 발판: 가운데
// 이후 Inspector에서 직접 바꾼 값은 그대로 둠 (처음 가져올 때만 적용)
public class BastetArtImport : AssetPostprocessor
{
    public const string Folder = "Assets/image/Bastet/Puzzle/";
    public const int PixelsPerUnit = 32;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder) || !assetImporter.importSettingsMissing) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = PixelsPerUnit;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;

        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = PivotFor(System.IO.Path.GetFileNameWithoutExtension(assetPath));
        st.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(st);
        Debug.Log($"[BastetArtImport] {assetPath}: PPU {PixelsPerUnit}, Point, 압축 없음, Pivot {st.spritePivot}");
    }

    // 그림 이름별 기준점 (0~1, 왼쪽 아래가 0,0). 그림을 새로 그리면 눈 위치에 맞게 Sprite Editor에서 Pivot만 옮기면 됨
    public static Vector2 PivotFor(string file)
    {
        switch (file)
        {
            case "Bastet_Obj_PawStatue_Eyes_Light": return new Vector2(0.4531f, 0.8438f);
            case "Bastet_Obj_EyeCatBig_Eyes_Light": return new Vector2(0.4844f, 0.8594f);
            case "Bastet_Obj_EyeCat_Eyes_Light": return new Vector2(0.4688f, 0.8672f);
        }
        if (file.StartsWith("Bastet_Floor_")) return new Vector2(0.5f, 0.5f); // 발판: 가운데
        return new Vector2(0.5f, 0f);                                        // 석상: 바닥 가운데
    }
}
