using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 도르래와 황금 기어비 천칭 - 실전 (서브퍼즐 2)
// 기어 3개(첫 → 가운데 → 마지막) + 도르래 2개, 숫자와 배율 계산식은 숨김
// 배율 = (마지막 톱니 / 첫 톱니) x 도르래1 x 도르래2  ※ 가운데 바퀴는 방향만 바꿈
public class GearPuzzleReal : MonoBehaviour, IAnubisRetryable, IAnubisVariant
{
    // [추가] 변형 1세트 = 목표 배율 + 그에 맞는 양피지 글
    [System.Serializable]
    public class Variant
    {
        public float targetAdvantage = 6f;
        [Tooltip("이 변형의 2번 방 양피지 글. 비우면 양피지 에셋에 적힌 원래 글")]
        [TextArea(4, 12)] public string scrollText;
    }

    [Header("난이도")]
    [Tooltip("톱니 수 선택지 (작은 / 중간 / 큰)")]
    [SerializeField] private int[] teethOptions = { 12, 24, 36 };
    [Tooltip("화면에 보이는 바퀴 이름 (teethOptions와 같은 순서)")]
    [SerializeField] private string[] sizeNames = { "작은 바퀴", "중간 바퀴", "큰 바퀴" };
    [Tooltip("목표 배율 (1근 추로 들어 올릴 무게)")]
    [SerializeField] private float targetAdvantage = 6f;
    [Tooltip("시작 시 기어 3개의 선택 번호 (0 = 작은 바퀴)")]
    [SerializeField] private int[] startGearIndex = { 0, 0, 0 };

    [Header("변형 세트 [추가] (새 게임마다 무작위, 메인 저울 실패 시 다음 변형)")]
    [Tooltip("이 퍼즐의 푸는 방법이 적힌 양피지 ID")]
    [SerializeField] private string scrollId = "SCROLL_ANUBIS_2";
    [SerializeField]
    private Variant[] variants =
    {
        // 0: 기본 6배 (작은 -> 큰 3배 x 움직 도르래 1개)
        new Variant { targetAdvantage = 6f, scrollText = "" },
        // 1: 4배 (작은 -> 중간 2배 x 움직 도르래 1개, 또는 같은 크기 x 움직 도르래 2개)
        new Variant { targetAdvantage = 4f, scrollText = "태엽의 힘은 오직 첫 바퀴와 마지막 바퀴가 정한다.\n마지막 바퀴의 이가 첫 바퀴보다 많을수록,\n힘은 그 비율만큼 커진다.\n가운데 바퀴는 도는 방향만 바꿀 뿐,\n힘을 더하지도 빼지도 않는다.\n\n작은 바퀴는 열두 개의 이,\n중간 바퀴는 스물네 개, 큰 바퀴는 서른여섯 개의 이를 가졌다.\n\n고정 도르래는 방향만 바꾸고,\n움직 도르래는 하나마다 힘을 두 배로 만든다.\n\n한 근의 추로 네 근의 석관을 들어 올려라.\n힘이 모자라도, 넘쳐도 줄은 버티지 못한다." },
        // 2: 8배 (작은 -> 중간 2배 x 움직 도르래 2개)
        new Variant { targetAdvantage = 8f, scrollText = "태엽의 힘은 오직 첫 바퀴와 마지막 바퀴가 정한다.\n마지막 바퀴의 이가 첫 바퀴보다 많을수록,\n힘은 그 비율만큼 커진다.\n가운데 바퀴는 도는 방향만 바꿀 뿐,\n힘을 더하지도 빼지도 않는다.\n\n작은 바퀴는 열두 개의 이,\n중간 바퀴는 스물네 개, 큰 바퀴는 서른여섯 개의 이를 가졌다.\n\n고정 도르래는 방향만 바꾸고,\n움직 도르래는 하나마다 힘을 두 배로 만든다.\n\n한 근의 추로 여덟 근의 석관을 들어 올려라.\n힘이 모자라도, 넘쳐도 줄은 버티지 못한다." }
    };

    [Header("클리어 처리")]
    [Tooltip("사자의 서 단서 슬롯 (-1이면 없음)")]
    [SerializeField] private int clueIndex = 1;
    [TextArea(2, 4)]
    [SerializeField] private string clueText = "청동의 그릇(B)과 흙(C)의 무게 합은, 밤의 흑요석(D)과 황금(A)의 합보다 가장 작은 무게 단위(1kg)만큼 무겁도다. ((B + C) - (D + A) = 1)";
    [Tooltip("클리어 보상 아이템 ID (비우면 지급 안 함)")]
    [SerializeField] private string rewardItemId = AnubisItemIds.ClayPot;
    [TextArea(1, 3)]
    [SerializeField] private string clearMessage = "석관이 천천히 떠올랐습니다!\n사자의 서에 두 번째 비문이 기록되었습니다.";

    [Header("기어 3개 (첫 / 가운데 / 마지막 순서)")]
    [SerializeField] private RectTransform[] gearTransforms = new RectTransform[3];
    [SerializeField] private TMP_Text[] gearLabels = new TMP_Text[3];
    [SerializeField] private Button[] gearButtons = new Button[3];

    [Header("도르래 2개")]
    [SerializeField] private Button[] pulleyButtons = new Button[2];
    [SerializeField] private Image[] pulleyImages = new Image[2];
    [SerializeField] private TMP_Text[] pulleyLabels = new TMP_Text[2];

    [Header("석관 / 시스템")]
    [SerializeField] private RectTransform relicPlate;
    [SerializeField] private Button btnCrank;
    [SerializeField] private Button btnClose;
    [SerializeField] private TMP_Text textFeedback;
    [SerializeField] private TMP_Text textAttempts;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private TMP_Text textSuccessNotice;

    private static readonly Color PulleyFixed = new Color(0.6f, 0.6f, 0.6f, 1f);
    private static readonly Color PulleyMoving = new Color(1f, 0.85f, 0.3f, 1f);
    private const float MaxLift = 70f; // 정답일 때 석관이 올라가는 높이

    private readonly int[] gearIndex = new int[3];
    private readonly bool[] pulleyMoving = new bool[2];
    private Vector2 relicStartPos;
    private int attempts;
    private bool isOperating;
    private bool isCleared;
    private bool rewarded;

    private void Awake()
    {
        if (relicPlate != null) relicStartPos = relicPlate.anchoredPosition;
    }

    private void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            if (gearButtons[i] != null) gearButtons[i].onClick.AddListener(() => CycleGear(idx));
            gearIndex[i] = (startGearIndex != null && i < startGearIndex.Length) ? Mathf.Clamp(startGearIndex[i], 0, teethOptions.Length - 1) : 0;
        }
        for (int i = 0; i < 2; i++)
        {
            int idx = i;
            if (pulleyButtons[i] != null) pulleyButtons[i].onClick.AddListener(() => TogglePulley(idx));
        }

        if (btnCrank != null) btnCrank.onClick.AddListener(OperateCrank);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));
        if (textSuccessNotice != null) textSuccessNotice.text = clearMessage;
        if (successNotice != null) successNotice.SetActive(false);
        if (textFeedback != null) textFeedback.text = "";

        UpdateVisuals();
    }

    private void OnEnable()
    {
        if (relicPlate != null && !isCleared) relicPlate.anchoredPosition = relicStartPos;
    }

    // 현재 배율 (가운데 바퀴는 계산에 들어가지 않음)
    private float CurrentAdvantage()
    {
        float gearRatio = (float)teethOptions[gearIndex[2]] / teethOptions[gearIndex[0]];
        float pulley = (pulleyMoving[0] ? 2f : 1f) * (pulleyMoving[1] ? 2f : 1f);
        return gearRatio * pulley;
    }

    private void CycleGear(int slot)
    {
        if (isCleared || isOperating) return;
        gearIndex[slot] = (gearIndex[slot] + 1) % teethOptions.Length;
        ClearFeedback();
        UpdateVisuals();
    }

    private void TogglePulley(int slot)
    {
        if (isCleared || isOperating) return;
        pulleyMoving[slot] = !pulleyMoving[slot];
        ClearFeedback();
        UpdateVisuals();
    }

    private void OperateCrank()
    {
        if (isCleared || isOperating) return;
        StartCoroutine(CrankRoutine());
    }

    private IEnumerator CrankRoutine()
    {
        isOperating = true;
        attempts++;
        ClearFeedback();

        float adv = CurrentAdvantage();
        bool solved = Mathf.Abs(adv - targetAdvantage) < 0.01f;

        float ratio = Mathf.Clamp01(adv / targetAdvantage);
        float lift = solved ? MaxLift : MaxLift * 0.6f * ratio;

        int tFirst = teethOptions[gearIndex[0]];
        int tMid = teethOptions[gearIndex[1]];
        int tLast = teethOptions[gearIndex[2]];

        float firstRot = -360f;
        float midRot = 360f * tFirst / tMid;
        float lastRot = -360f * tFirst / tLast;

        const float duration = 1.4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            float step = dt / duration;
            if (gearTransforms[0] != null) gearTransforms[0].Rotate(0, 0, firstRot * step);
            if (gearTransforms[1] != null) gearTransforms[1].Rotate(0, 0, midRot * step);
            if (gearTransforms[2] != null) gearTransforms[2].Rotate(0, 0, lastRot * step);

            if (relicPlate != null)
            {
                // 힘이 넘치면 위로 튀었다가 떨어지는 느낌 
                float t = elapsed / duration;
                float y = (solved || adv < targetAdvantage) ? lift * Mathf.Clamp01(t * 1.5f) : lift * Mathf.Sin(t * Mathf.PI);
                relicPlate.anchoredPosition = relicStartPos + new Vector2(0, y);
            }
            yield return null;
        }

        if (solved)
        {
            isCleared = true;
            if (successNotice != null) successNotice.SetActive(true);
            GiveReward();
        }
        else
        {
            if (relicPlate != null) relicPlate.anchoredPosition = relicStartPos;
            SetFeedback(adv < targetAdvantage
                ? "<color=#FF6655>석관이 조금 들리다 내려앉습니다. 힘이 모자랍니다.</color>"
                : "<color=#FF6655>줄이 끊어질 듯 팽팽해지다 풀립니다. 힘이 넘칩니다.</color>");
        }

        UpdateVisuals();
        isOperating = false;
    }

    private void GiveReward()
    {
        if (rewarded) return;
        rewarded = true;
        if (clueIndex >= 0 && !string.IsNullOrEmpty(clueText) && AnubisNotebookManager.Instance != null)
            AnubisNotebookManager.Instance.UnlockClue(clueIndex, clueText);
        if (!string.IsNullOrEmpty(rewardItemId) && ItemInventory.Instance != null)
            ItemInventory.Instance.Grant(rewardItemId);
        Debug.Log($"[{name}] 클리어! 시도 {attempts}번", this);
    }

    // ================= [추가] 변형 =================
    public int VariantCount => variants != null ? variants.Length : 0;
    public int CurrentVariant { get; private set; }

    public void ApplyVariant(int index)
    {
        if (VariantCount == 0) return;
        index = Mathf.Clamp(index, 0, VariantCount - 1);
        targetAdvantage = variants[index].targetAdvantage;
        CurrentVariant = index;
        AnubisVariantManager.SetScrollText(scrollId, variants[index].scrollText);
    }

    // [추가] 메인 저울 실패 처벌: 기어, 도르래, 석관을 처음으로 (사자의 서 단서는 유지)
    public void ResetForRetry()
    {
        StopAllCoroutines();
        isOperating = false;
        isCleared = false;
        rewarded = false;
        attempts = 0;

        for (int i = 0; i < 3; i++)
        {
            gearIndex[i] = (startGearIndex != null && i < startGearIndex.Length) ? Mathf.Clamp(startGearIndex[i], 0, teethOptions.Length - 1) : 0;
            if (gearTransforms[i] != null) gearTransforms[i].localRotation = Quaternion.identity;
        }
        for (int i = 0; i < 2; i++) pulleyMoving[i] = false;

        if (relicPlate != null) relicPlate.anchoredPosition = relicStartPos;
        if (successNotice != null) successNotice.SetActive(false);
        ClearFeedback();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < 3; i++)
        {
            int teeth = teethOptions[gearIndex[i]];
            // 톱니 수에 비례한 크기 (12 = 0.75, 24 = 1.0, 36 = 1.25)
            if (gearTransforms[i] != null) gearTransforms[i].localScale = Vector3.one * (0.5f + teeth / 48f);
            if (gearLabels[i] != null && gearIndex[i] < sizeNames.Length) gearLabels[i].text = sizeNames[gearIndex[i]];
        }

        for (int i = 0; i < 2; i++)
        {
            if (pulleyImages[i] != null) pulleyImages[i].color = pulleyMoving[i] ? PulleyMoving : PulleyFixed;
            if (pulleyLabels[i] != null) pulleyLabels[i].text = pulleyMoving[i] ? "움직 도르래" : "고정 도르래";
        }

        if (textAttempts != null) textAttempts.text = $"시도 횟수: {attempts}";
    }

    private void SetFeedback(string msg) { if (textFeedback != null) textFeedback.text = msg; }
    private void ClearFeedback() { if (textFeedback != null) textFeedback.text = ""; }

    // [추가] 첫 / 마지막 바퀴와 도르래 조합 중 목표 배율이 되는 것을 찾아서 적용
    private bool FindAnswer()
    {
        for (int a = 0; a < teethOptions.Length; a++)
            for (int c = 0; c < teethOptions.Length; c++)
                for (int p = 0; p < 4; p++)
                {
                    gearIndex[0] = a;
                    gearIndex[2] = c;
                    pulleyMoving[0] = (p & 1) != 0;
                    pulleyMoving[1] = (p & 2) != 0;
                    if (Mathf.Abs(CurrentAdvantage() - targetAdvantage) < 0.01f) return true;
                }
        return false;
    }

    [ContextMenu("테스트: 정답 상태로 맞추기 (지금 변형의 목표 배율)")]
    private void TestSetAnswer()
    {
        // [수정] 변형마다 목표가 달라서 가능한 조합을 찾아서 맞춤
        if (!FindAnswer()) Debug.LogWarning($"[{name}] 목표 배율 {targetAdvantage}을 만들 수 있는 조합이 없음", this);
        UpdateVisuals();
        Debug.Log($"[{name}] 현재 배율 {CurrentAdvantage()} (크랭크를 눌러 확인)", this);
    }
}