using UnityEngine;

// 라 엔딩 컷신에 쓰는 그림 묶음 (에셋: Assets/Resources/RaFinaleArt.asset)
// - Ra Illustration: 아트가 그린 라 일러스트 (1920x1080). 비어 있으면 도형으로 그린 임시 그림이 나옴
// - Cube Sprite: 큐브 그림 (Assets/image/Cube/Cube.png) - 먼저 큐브 전체를 보여줌
// - Face Sprite: 큐브의 라 면 (Assets/image/Cube/Ra.jpg) - 큐브에서 이 면으로 바뀌고, 가운데 빈 칸에 형판이 박힘
// - Plate Sprite: 태양 형판 (Assets/image/ui/I_Item_PlateRa.png)
// 아트가 오면 RaFinaleArt.asset을 클릭 -> Ra Illustration 칸에 끌어다 넣기만 하면 끝
[CreateAssetMenu(fileName = "RaFinaleArt", menuName = "PharaohCube/Ra Finale Art")]
public class RaFinaleArt : ScriptableObject
{
    private const string ResourcePath = "RaFinaleArt";
    private static RaFinaleArt cached;

    [Header("라 일러스트 (비우면 임시 도형)")]
    public Sprite raIllustration;

    [Header("큐브 공개")]
    public Sprite cubeSprite;
    public Sprite faceSprite;
    public Sprite plateSprite;
    [Tooltip("라 면 그림 안에서 빈 칸(검은 네모)의 가운데. 0~1, 왼쪽 아래가 (0,0)")]
    public Vector2 faceSlot = new Vector2(0.506f, 0.378f);
    [Tooltip("빈 칸 높이 / 면 그림 높이 (형판이 이 크기로 박힘)")]
    [Range(0.05f, 1f)] public float faceSlotHeight = 0.238f;

    public static RaFinaleArt Main
    {
        get
        {
            if (cached == null) cached = Resources.Load<RaFinaleArt>(ResourcePath);
            return cached;
        }
    }
}
