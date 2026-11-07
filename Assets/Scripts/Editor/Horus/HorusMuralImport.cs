using System;
using UnityEditor;
using UnityEngine;

// 벽화 컷신 그림(Assets/Resources/Horus/Mural/*.png)을 넣으면 자동으로 UI용 스프라이트로 가져옴
// - 도트가 아니라 매끈한 벽화라 Bilinear, 1280x720이 넘지 않게 최대 2048, 밉맵 없음, 압축은 고품질
// - 빌더를 안 돌려도 파일만 들어오면 적용됨 (MuralStage가 Resources.Load<Sprite>로 읽음)
public class HorusMuralImport : AssetPostprocessor
{
    private const string Folder = "Assets/Resources/Horus/Mural/";

    private void OnPreprocessTexture()
    {
        try
        {
            if (!assetPath.Replace('\\', '/').StartsWith(Folder, StringComparison.Ordinal)) return;
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.filterMode = FilterMode.Bilinear;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
        }
        catch (Exception e)
        {
            Debug.LogError($"[벽화 가져오기] {assetPath}: {e.Message}");
        }
    }
}
