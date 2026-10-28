using UnityEngine;

// 라 엔딩 컷신에 쓰는 그림 묶음 (에셋: Assets/Resources/RaFinaleArt.asset)
// - Ra Illustration: 아트가 그린 라 일러스트 (1920x1080). 비어 있으면 도형으로 그린 임시 그림이 나옴
// - Cube Sprite: 큐브 그림 (Assets/image/Cube/Cube.png)
// - Face Sprites: 큐브 여섯 면 칸에 들어갈 면 그림 (Assets/image/Cube/라, 바스테트, 토트, 호루스, 소베크, 아누비스)
//   -> 바스테트 엔딩(BastetEnding)과 같은 순서, 같은 그림
// 아트가 오면 RaFinaleArt.asset을 클릭 -> Ra Illustration 칸에 끌어다 넣기만 하면 끝
[CreateAssetMenu(fileName = "RaFinaleArt", menuName = "PharaohCube/Ra Finale Art")]
public class RaFinaleArt : ScriptableObject
{
    public const int FaceCount = 6;
    public const int RaFace = 0;

    private const string ResourcePath = "RaFinaleArt";
    private static RaFinaleArt cached;

    [Header("라 일러스트 (비우면 임시 도형)")]
    public Sprite raIllustration;

    [Header("큐브 공개 (바스테트 엔딩과 같은 방식)")]
    public Sprite cubeSprite;
    [Tooltip("0 라, 1 바스테트, 2 토트, 3 호루스, 4 소베크, 5 아누비스")]
    public Sprite[] faceSprites = new Sprite[FaceCount];

    public static RaFinaleArt Main
    {
        get
        {
            if (cached == null) cached = Resources.Load<RaFinaleArt>(ResourcePath);
            return cached;
        }
    }

    public Sprite Face(int index)
    {
        if (faceSprites == null || index < 0 || index >= faceSprites.Length) return null;
        return faceSprites[index];
    }
}
