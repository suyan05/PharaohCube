using UnityEngine;

// 양피지 데이터 (방마다 1개 에셋으로 생성)
// 조각을 모두 모으면 fullText(푸는 방법)가 나타남
// [추가] 단계 공개: 글을 빈 줄(문단) 기준으로 stageCount개 단계로 나눠, 조각을 모을수록 하나씩 드러남
//        예) 3단계 + 조각 5개 -> 조각 2개: 1단계 / 4개: 2단계까지 / 5개: 전부
//        '---' 한 줄을 넣으면 그 줄을 단계 경계로 직접 정할 수도 있음

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

    [Header("단계 공개 [추가]")]
    [Tooltip("글을 몇 단계로 나눠 보여 줄지. 1 = 조각을 다 모아야 전부 보임 (기존 방식)\n" +
             "문단(빈 줄) 순서대로 나눔. 변형 퍼즐의 글도 같은 문단 구조라 그대로 적용됨")]
    [Range(1, 5)]
    [SerializeField] private int stageCount = 1;
    [Tooltip("단계마다 필요한 조각 수 (비우면 조각 수에 맞춰 자동 배분)\n예) {2, 4, 5}")]
    [SerializeField] private int[] stagePieceCounts;

    [Header("그림 (선택, 비우면 기본 도형)")]
    [Tooltip("조각 그림. 순서 = 조각 번호 1, 2, 3 ...")]
    [SerializeField] private Sprite[] pieceSprites;
    [Tooltip("완성된 양피지 전체 그림")]
    [SerializeField] private Sprite fullSprite;

    public string ScrollId => scrollId;
    public string Title => title;
    public int PieceCount => Mathf.Max(1, pieceCount);
    // [추가] 변형 퍼즐용 런타임 글 (저장되지 않음). 비어 있으면 에셋의 fullText 사용
    [System.NonSerialized] private string runtimeText;

    public string FullText => string.IsNullOrEmpty(runtimeText) ? fullText : runtimeText;

    // [추가] AnubisVariantManager가 변형에 맞는 글로 바꿀 때 호출 (null = 원래 글)
    public void SetRuntimeText(string text) => runtimeText = text;
    public Sprite FullSprite => fullSprite;

    // ================= [추가] 단계 공개 =================
    private static readonly string[] SeparatorLine = { "\n---\n" };
    private static readonly string[] BlankLine = { "\n\n" };

    // 지금 글을 단계로 나눔: '---' 줄이 있으면 그 기준, 없으면 문단을 stageCount개로 묶음
    public string[] GetStages()
    {
        string text = (FullText ?? "").Replace("\r\n", "\n").Trim();

        if (text.Contains("\n---\n"))
        {
            string[] manual = text.Split(SeparatorLine, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < manual.Length; i++) manual[i] = manual[i].Trim();
            return manual;
        }

        string[] paras = text.Split(BlankLine, System.StringSplitOptions.RemoveEmptyEntries);
        int n = paras.Length;
        int s = Mathf.Clamp(stageCount, 1, Mathf.Max(1, n));
        var stages = new string[s];
        for (int k = 0; k < s; k++)
        {
            int from = k * n / s;
            int to = (k + 1) * n / s;
            stages[k] = string.Join("\n\n", paras, from, to - from).Trim();
        }
        return stages;
    }

    // k번째 단계(0부터)가 열리는 데 필요한 조각 수
    public int PiecesForStage(int stage, int stageCount)
    {
        if (stagePieceCounts != null && stage < stagePieceCounts.Length)
            return Mathf.Clamp(stagePieceCounts[stage], 1, PieceCount);
        if (stageCount <= 1) return PieceCount; // 나누지 않은 글은 다 모아야 보임
        return Mathf.Clamp(Mathf.CeilToInt((stage + 1) * PieceCount / (float)stageCount), 1, PieceCount);
    }

    // 모은 조각 수에 맞춰 지금 보여 줄 글. 아직 열리지 않은 단계가 있으면 hasMore = true
    public string GetRevealedText(int collected, out bool hasMore, out int nextNeed)
    {
        string[] stages = GetStages();
        var sb = new System.Text.StringBuilder();
        hasMore = false;
        nextNeed = 0;

        for (int i = 0; i < stages.Length; i++)
        {
            int need = PiecesForStage(i, stages.Length);
            if (collected < need)
            {
                hasMore = true;
                nextNeed = need;
                break;
            }
            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append(stages[i]);
        }
        return sb.ToString();
    }

    public Sprite GetPieceSprite(int index) =>
        (pieceSprites != null && index >= 0 && index < pieceSprites.Length) ? pieceSprites[index] : null;
}