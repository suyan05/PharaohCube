using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 도르래와 황금 기어비 천칭 - 실전 (서브퍼즐 2)
// 기어 3개(첫 → 가운데 → 마지막) + 도르래 2개, 숫자와 배율 계산식은 숨김
// 배율 = (마지막 톱니 / 첫 톱니) x 도르래1 x 도르래2  ※ 가운데 바퀴는 방향만 바꿈
public class GearPuzzleReal : MonoBehaviour
{
    [Header("난이도")]
    [Tooltip("톱니 수 선택지 (작은 / 중간 / 큰)")]
    [SerializeField] private int[] teethOptions = { 12, 24, 36 };
    [Tooltip("화면에 보이는 바퀴 이름 (teethOptions와 같은 순서)")]
    [SerializeField] private string[] sizeNames = { "작은 바퀴", "중간 바퀴", "큰 바퀴" };
    [Tooltip("목표 배율 (1근 추로 들어 올릴 무게)")]
    [SerializeField] private float targetAdvantage = 6f;
    [Tooltip("시작 시 기어 3개의 선택 번호 (0 = 작은 바퀴)")]
    [SerializeField] private int[] startGearIndex = { 0, 0, 0 };

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

    [ContextMenu("테스트: 정답 상태로 맞추기 (작은 → 큰, 도르래 1개)")]
    private void TestSetAnswer()
    {
        gearIndex[0] = 0;
        gearIndex[2] = teethOptions.Length - 1;
        pulleyMoving[0] = true;
        pulleyMoving[1] = false;
        UpdateVisuals();
        Debug.Log($"[{name}] 현재 배율 {CurrentAdvantage()} (크랭크를 눌러 확인)", this);
    }
}