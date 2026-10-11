using UnityEngine;

// 양피지 데이터 (방마다 1개 에셋으로 생성)
// 조각을 모두 모으면 fullText(푸는 방법)가 나타남

[CreateAssetMenu(fileName = "Scroll_Room1", menuName = "Anubis/양피지 데이터", order = 0)]
public class AnubisScrollData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("고유 ID (다른 양피지와 겹치면 안 됨). 완성 시 이 ID가 ItemInventory에 지급됨")]
    [SerializeField] private string scrollId = "SCROLL_ANUBIS_1";
    [SerializeField] private string title = "모래의 서";
    [Range(1, 8)]
    [SerializeField] private int pieceCount = 5;

    [Header("완성 시 나타나는 글 (푸는 방법)")]
    [TextArea(6, 14)]
    [SerializeField] private string fullText = "";

    [Header("그림 (선택, 비우면 기본 도형)")]
    [Tooltip("조각 그림. 순서 = 조각 번호 1, 2, 3 ...")]
    [SerializeField] private Sprite[] pieceSprites;
    [Tooltip("완성된 양피지 전체 그림")]
    [SerializeField] private Sprite fullSprite;

    public string ScrollId => scrollId;
    public string Title => title;
    public int PieceCount => Mathf.Max(1, pieceCount);
    public string FullText => fullText;
    public Sprite FullSprite => fullSprite;

    public Sprite GetPieceSprite(int index) =>
        (pieceSprites != null && index >= 0 && index < pieceSprites.Length) ? pieceSprites[index] : null;
}