using UnityEngine;

// 바스테트 튜토리얼 퍼즐 1개의 데이터 (규칙 글, 수치, 보상)
// 실전 B1~B4와 같은 규칙을 작은 판으로 연습시키는 용도
// 만들기: Project 창 우클릭 -> Create -> PharaohCube -> Bastet -> Tutorial Puzzle Data
//         (Tools/Bastet/튜토리얼 만들기 메뉴가 기본값 4개를 자동으로 만듦)
[CreateAssetMenu(fileName = "BastetTut_", menuName = "PharaohCube/Bastet/Tutorial Puzzle Data")]
public class BastetTutorialData : ScriptableObject
{
    public enum Mode
    {
        Footprint,   // T1: 빛난 발자국 길을 순서대로 밟기 (실전 B1)
        EyeReverse,  // T2: 빛난 눈을 거꾸로 누르기 (실전 B2)
        Necklace,    // T3: 좌우 거울 대칭으로 목걸이 조각 돌리기 (실전 B3)
        ClawPattern  // T4: 발톱 무늬 빈칸 채우기 (실전 B4)
    }

    [Header("종류와 문구")]
    [SerializeField] private Mode mode = Mode.Footprint;
    [SerializeField] private string title = "고양이 발자국";
    [Tooltip("퍼즐 화면 위 양피지 상자에 보이는 규칙 설명")]
    [TextArea(2, 5)]
    [SerializeField] private string ruleText = "";
    [TextArea(1, 3)]
    [SerializeField] private string successMessage = "시험을 통과했다!";

    [Header("T1 발자국: 판 크기와 길 (칸 번호 0 = 왼쪽 위)")]
    [Range(2, 5)] [SerializeField] private int gridSize = 3;

    [Header("T1 발자국 / T2 눈: 빛나는 순서")]
    [SerializeField] private int[] sequence = { 6, 3, 4, 1 };
    [Tooltip("하나가 켜져 있는 시간 (초)")]
    [Range(0.2f, 1.5f)] [SerializeField] private float showStepTime = 0.6f;

    [Header("T2 눈: 눈 개수")]
    [Range(2, 6)] [SerializeField] private int buttonCount = 4;

    [Header("T3 목걸이: 조각 4개의 처음 방향 (0 위, 1 오른쪽, 2 아래, 3 왼쪽). 0·1 = 왼쪽 두 개, 2·3 = 오른쪽 두 개. 짝 = 0↔3, 1↔2")]
    [SerializeField] private int[] startRotations = { 1, 0, 1, 2 };
    [Tooltip("켜면 1번 조각을 돌릴 때 2번 조각도 같이 돎 (실전의 '연결된 조각' 맛보기)")]
    [SerializeField] private bool firstPieceTurnsSecond = true;

    [Header("T4 발톱: 무늬 (1 = /, 2 = //, 3 = ///), 가로 칸 수, 빈칸 번호")]
    [SerializeField] private int[] pattern = { 1, 2, 2, 1, 2, 3, 3, 2 };
    [Range(2, 6)] [SerializeField] private int columns = 4;
    [SerializeField] private int[] blanks = { 1, 6 };

    [Header("보상")]
    [Tooltip("풀면 지급할 아이템 (석문 조건)")]
    [SerializeField] private string rewardItemId = "ITEM_BASTET_TUT_1";

    public Mode PuzzleMode => mode;
    public string Title => title;
    public string RuleText => ruleText;
    public string SuccessMessage => successMessage;
    public int GridSize => gridSize;
    public int[] Sequence => sequence;
    public float ShowStepTime => showStepTime;
    public int ButtonCount => buttonCount;
    public int[] StartRotations => startRotations;
    public bool FirstPieceTurnsSecond => firstPieceTurnsSecond;
    public int[] Pattern => pattern;
    public int Columns => columns;
    public int[] Blanks => blanks;
    public string RewardItemId => rewardItemId;

#if UNITY_EDITOR
    // 에디터 세팅 메뉴가 기본값을 채울 때 사용
    public void EditorSet(Mode m, string t, string rule, string success, string reward)
    {
        mode = m; title = t; ruleText = rule; successMessage = success; rewardItemId = reward;
    }
#endif
}
