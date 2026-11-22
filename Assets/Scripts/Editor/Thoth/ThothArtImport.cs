using System;
using UnityEditor;
using UnityEngine;

// 토트의 방 그림(Assets/Resources/Thoth/...)을 넣으면 자동으로 알맞게 가져옴 (빌더를 안 돌려도 적용)
// - World/  : 맵 타일, 물건 (32px = 1칸, Point = 도트 그대로, 압축 없음)
// - UI/, Fresco/ : 퍼즐 화면 그림, 벽화 조각 (매끈하게 Bilinear, 100px = 1유닛, 압축 없음 -> 조각 테두리가 뭉개지지 않게)
// 아트팀 원본(Assets/image/Thoth)은 건드리지 않고 복사본만 씀
public class ThothArtImport : AssetPostprocessor
{
    private const string Root = "Assets/Resources/Thoth/";

    private void OnPreprocessTexture()
    {
        try
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(Root, StringComparison.Ordinal)) return;
            var imp = (TextureImporter)assetImporter;
            bool world = path.StartsWith(Root + "World/", StringComparison.Ordinal);
            imp.textureType = TextureImporterType.Sprite;
            imp.textureShape = TextureImporterShape.Texture2D; // [수정] 처음 가져올 때 Cube(큐브맵)로 잡혀서 스프라이트가 안 만들어졌었음
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = world ? 32f : 100f;
            imp.filterMode = world ? FilterMode.Point : FilterMode.Bilinear;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
        }
        catch (Exception e)
        {
            Debug.LogError($"[토트 그림 가져오기] {assetPath}: {e.Message}");
        }
    }

    // [수정] 그림을 스프라이트로 못 읽으면 강제로 다시 가져옴 (빌더가 처음에 부름 + 메뉴)
    // - 그림이 이 스크립트보다 먼저 들어오면 처음 가져올 때 이 설정이 안 들어가서 스프라이트가 없었음 -> 맵, 물건이 다 안 보였음
    [MenuItem("PharaohCube/Thoth/토트 그림 다시 가져오기")]
    public static void ReimportAll()
    {
        int fixedCount = 0;
        foreach (string p in TexturePaths())
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(p) != null) continue;
            TextureImporter imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp != null)
            {
                imp.textureShape = TextureImporterShape.Texture2D;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.SaveAndReimport();
            }
            fixedCount++;
        }
        AssetDatabase.Refresh();
        int still = 0;
        foreach (string p in TexturePaths()) if (AssetDatabase.LoadAssetAtPath<Sprite>(p) == null) still++;
        Debug.Log($"[토트 그림] 다시 가져옴 {fixedCount}개 / 아직 스프라이트가 아닌 그림 {still}개");
    }

    private static System.Collections.Generic.List<string> TexturePaths()
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (string g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Thoth", "Assets/Resources/Horus/Mural" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            string file = System.IO.Path.GetFileName(p);
            if (p.StartsWith(Root, StringComparison.Ordinal) || file.StartsWith("MU_ThF", StringComparison.Ordinal) || file.StartsWith("MU_VaultTh", StringComparison.Ordinal)) list.Add(p);
        }
        return list;
    }
}
