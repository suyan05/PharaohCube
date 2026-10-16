using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 사자의 모래 분배기 - 실전 (서브퍼즐 1)
// 숫자를 보여주지 않음: 용량과 목표는 1번 방 양피지에서만 알 수 있음
// 기본값: 12 / 8 / 5 / 3, 시작 12만 가득 → 목표 4, 4, 4, 0 (최소 8번)
public class SandPuzzleReal : MonoBehaviour, IAnubisRetryable, IAnubisVariant
{
    // [추가] 변형 1세트 = 항아리 용량, 시작 양, 목표 양 + 그에 맞는 양피지 글
    [System.Serializable]
    public class Variant
    {
        public int[] capacities;
        public int[] startAmounts;
        public int[] goalAmounts;
        [Tooltip("이 변형의 1번 방 양피지 글. 비우면 양피지 에셋에 적힌 원래 글")]
        [TextArea(4, 12)] public string scrollText;
    }

    [Header("난이도 (항아리 순서: 왼쪽부터)")]
    [Tooltip("항아리 용량")]
    [SerializeField] private int[] capacities = { 12, 8, 5, 3 };
    [Tooltip("시작 모래 양")]
    [SerializeField] private int[] startAmounts = { 12, 0, 0, 0 };
    [Tooltip("목표 양 (-1이면 상관없음)")]
    [SerializeField] private int[] goalAmounts = { 4, 4, 4, 0 };

    [Header("변형 세트 [추가] (새 게임마다 무작위, 메인 저울 실패 시 다음 변형)")]
    [Tooltip("이 퍼즐의 푸는 방법이 적힌 양피지 ID")]
    [SerializeField] private string scrollId = "SCROLL_ANUBIS_1";
    [SerializeField]
    private Variant[] variants =
    {
        // 0: 기본 12/8/5/3 -> 4,4,4,0 (최소 8번)
        new Variant { capacities = new[] { 12, 8, 5, 3 }, startAmounts = new[] { 12, 0, 0, 0 }, goalAmounts = new[] { 4, 4, 4, 0 }, scrollText = "" },
        // 1: 9/7/4/2 -> 3,3,3,0 (최소 8번)
        new Variant { capacities = new[] { 9, 7, 4, 2 }, startAmounts = new[] { 9, 0, 0, 0 }, goalAmounts = new[] { 3, 3, 3, 0 },
            scrollText = "나트론의 모래는 오직 항아리에서 항아리로만 옮겨진다.\n한 번 붓기 시작하면, 받는 항아리가 가득 차거나\n붓는 항아리가 빌 때까지 멈추지 않는다.\n\n첫째 항아리는 아홉 되, 둘째는 일곱 되,\n셋째는 네 되, 넷째는 두 되를 담는다.\n\n모래는 처음에 첫째 항아리에만 가득 담겨 있으니,\n첫째·둘째·셋째 항아리에 세 되씩 똑같이 나누고\n막내인 넷째 항아리는 비워 두어라." },
        // 2: 10/7/4/3 -> 6,2,2,0 (최소 8번)
        new Variant { capacities = new[] { 10, 7, 4, 3 }, startAmounts = new[] { 10, 0, 0, 0 }, goalAmounts = new[] { 6, 2, 2, 0 },
            scrollText = "나트론의 모래는 오직 항아리에서 항아리로만 옮겨진다.\n한 번 붓기 시작하면, 받는 항아리가 가득 차거나\n붓는 항아리가 빌 때까지 멈추지 않는다.\n\n첫째 항아리는 열 되, 둘째는 일곱 되,\n셋째는 네 되, 넷째는 세 되를 담는다.\n\n모래는 처음에 첫째 항아리에만 가득 담겨 있으니,\n첫째 항아리에는 여섯 되를 남기고\n둘째와 셋째에 두 되씩 나누며\n막내인 넷째 항아리는 비워 두어라." }
    };

    [Header("클리어 처리")]
    [Tooltip("사자의 서 단서 슬롯 (-1이면 없음)")]
    [SerializeField] private int clueIndex = 0;
    [TextArea(2, 4)]
    [SerializeField] private string clueText = "가장 비천한 흙의 단지(C)에 태양의 황금(A)을 더해야만 심장(H)의 무게를 얻으리라. (C + A = H)";
    [Tooltip("클리어 보상 아이템 ID (비우면 지급 안 함)")]
    [SerializeField] private string rewardItemId = AnubisItemIds.GoldJar;
    [TextArea(1, 3)]
    [SerializeField] private string clearMessage = "모래가 공평하게 나뉘었습니다!\n사자의 서에 첫 번째 비문이 기록되었습니다.";

    [Header("항아리 UI (왼쪽부터 같은 순서)")]
    [SerializeField] private Button[] jarButtons;
    [SerializeField] private Image[] jarFills;
    [SerializeField] private GameObject[] jarHighlights;

    [Header("시스템")]
    [SerializeField] private Button btnReset;
    [SerializeField] private Button btnClose;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private TMP_Text textSuccessNotice;
    [Tooltip("붓기 횟수 표시 (비워도 됨)")]
    [SerializeField] private TMP_Text textMoveCount;

    private int[] cur;          // 현재 모래 양
    private float[] fillStart;  // 애니메이션 시작값 (재사용)
    private int selected = -1;
    private int moveCount;
    private bool isCleared;
    private bool isPouring;
    private bool rewarded;

    private int JarCount => capacities != null ? capacities.Length : 0;

    private void Awake()
    {
        cur = new int[JarCount];
        fillStart = new float[JarCount];

        if (jarButtons == null || jarButtons.Length < JarCount || jarFills == null || jarFills.Length < JarCount)
            Debug.LogError($"[{name}] 항아리 UI 개수({jarButtons?.Length})가 용량 개수({JarCount})보다 적음", this);
    }

    private void Start()
    {
        for (int i = 0; i < JarCount && i < jarButtons.Length; i++)
        {
            int idx = i;
            if (jarButtons[i] != null) jarButtons[i].onClick.AddListener(() => OnClickJar(idx));
        }

        if (btnReset != null) btnReset.onClick.AddListener(ResetPuzzle);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));
        if (textSuccessNotice != null) textSuccessNotice.text = clearMessage;
        if (successNotice != null) successNotice.SetActive(false);

        ResetState();
    }

    private int Cap(int i) => Mathf.Max(1, capacities[i]);
    private int Goal(int i) => (goalAmounts != null && i < goalAmounts.Length) ? goalAmounts[i] : -1;
    private int StartAmount(int i) => (startAmounts != null && i < startAmounts.Length) ? Mathf.Clamp(startAmounts[i], 0, Cap(i)) : 0;

    private void OnClickJar(int index)
    {
        if (isCleared || isPouring) return;

        if (selected == -1)
        {
            selected = index;
            SetHighlight(index, true);
            return;
        }

        if (selected != index) StartCoroutine(PourRoutine(selected, index));
        SetHighlight(selected, false);
        selected = -1;
    }

    private IEnumerator PourRoutine(int from, int to)
    {
        int move = Mathf.Min(cur[from], Cap(to) - cur[to]);
        if (move <= 0) yield break; // 빈 항아리에서 붓거나 가득 찬 곳에 부으면 무시

        isPouring = true;
        cur[from] -= move;
        cur[to] += move;
        moveCount++;

        for (int i = 0; i < JarCount; i++)
            fillStart[i] = jarFills[i] != null ? jarFills[i].fillAmount : 0f;

        const float duration = 0.45f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < JarCount; i++)
                if (jarFills[i] != null) jarFills[i].fillAmount = Mathf.Lerp(fillStart[i], (float)cur[i] / Cap(i), t);
            yield return null;
        }

        UpdateVisuals();
        CheckClear();
        isPouring = false;
    }

    private void CheckClear()
    {
        for (int i = 0; i < JarCount; i++)
        {
            int g = Goal(i);
            if (g >= 0 && cur[i] != g) return;
        }

        isCleared = true;
        if (successNotice != null) successNotice.SetActive(true);
        Debug.Log($"[{name}] 클리어! 붓기 {moveCount}번", this);

        if (rewarded) return;
        rewarded = true;
        if (clueIndex >= 0 && !string.IsNullOrEmpty(clueText) && AnubisNotebookManager.Instance != null)
            AnubisNotebookManager.Instance.UnlockClue(clueIndex, clueText);
        if (!string.IsNullOrEmpty(rewardItemId) && ItemInventory.Instance != null)
            ItemInventory.Instance.Grant(rewardItemId);
    }

    public void ResetPuzzle()
    {
        if (isPouring || isCleared) return;
        ResetState();
    }

    // ================= [추가] 변형 =================
    public int VariantCount => variants != null ? variants.Length : 0;
    public int CurrentVariant { get; private set; }

    public void ApplyVariant(int index)
    {
        if (VariantCount == 0) return;
        index = Mathf.Clamp(index, 0, VariantCount - 1);
        Variant v = variants[index];

        // 항아리 UI가 4개라 개수는 그대로 두고 값만 교체
        if (v.capacities == null || v.capacities.Length != JarCount)
        {
            Debug.LogError($"[{name}] 변형 {index}의 항아리 개수가 {JarCount}개가 아님", this);
            return;
        }
        capacities = (int[])v.capacities.Clone();
        startAmounts = v.startAmounts != null ? (int[])v.startAmounts.Clone() : new int[JarCount];
        goalAmounts = v.goalAmounts != null ? (int[])v.goalAmounts.Clone() : new int[JarCount];
        CurrentVariant = index;

        AnubisVariantManager.SetScrollText(scrollId, v.scrollText);
        if (cur != null && !isCleared) ResetState(); // 이미 열어 본 패널이면 바로 새 문제로
    }

    // [추가] 메인 저울 실패 처벌: 클리어 상태와 모래를 처음으로 (사자의 서 단서는 유지)
    public void ResetForRetry()
    {
        StopAllCoroutines();
        isPouring = false;
        isCleared = false;
        rewarded = false;
        if (successNotice != null) successNotice.SetActive(false);
        if (cur == null) return; // 한 번도 열지 않은 패널이면 Start에서 초기화됨
        ResetState();
    }

    private void ResetState()
    {
        for (int i = 0; i < JarCount; i++) cur[i] = StartAmount(i);
        selected = -1;
        moveCount = 0;
        for (int i = 0; i < JarCount; i++) SetHighlight(i, false);
        UpdateVisuals();
    }

    private void SetHighlight(int index, bool on)
    {
        if (jarHighlights != null && index >= 0 && index < jarHighlights.Length && jarHighlights[index] != null)
            jarHighlights[index].SetActive(on);
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < JarCount; i++)
            if (jarFills[i] != null) jarFills[i].fillAmount = (float)cur[i] / Cap(i);

        if (textMoveCount != null) textMoveCount.text = $"붓기 횟수: {moveCount}";
    }

    [ContextMenu("테스트: 정답 상태로 만들기")]
    private void TestSolve()
    {
        for (int i = 0; i < JarCount; i++)
            if (Goal(i) >= 0) cur[i] = Goal(i);
        UpdateVisuals();
        CheckClear();
    }
}
// 정답 풀이
//1	첫째 → 둘째	4, 8, 0, 0
//2 둘째 → 셋째	4, 3, 5, 0
//3	셋째 → 넷째	4, 3, 2, 3
//4	넷째 → 둘째	4, 6, 2, 0
//5	셋째 → 넷째	4, 6, 0, 2
//6	둘째 → 셋째	4, 1, 5, 2
//7	셋째 → 넷째	4, 1, 4, 3
//8	넷째 → 둘째	4, 4, 4, 0 