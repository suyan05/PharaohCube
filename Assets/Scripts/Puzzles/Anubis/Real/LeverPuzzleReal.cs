using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 아누비스의 지렛대 천칭 - 실전 (서브퍼즐 3)
// 5칸 눈금, 심장은 왼쪽 3번째 칸 고정, 무게는 화면에 숨김
// 평형 조건: 심장무게 x 심장칸 = 추무게 x 추개수 x 걸이칸
public class LeverPuzzleReal : MonoBehaviour
{
    [Header("난이도 (무게는 화면에 표시되지 않음)")]
    [Tooltip("눈금 칸 수 (한쪽)")]
    [Range(3, 6)]
    [SerializeField] private int slotCount = 5;
    [Tooltip("눈금 1칸의 간격 (px)")]
    [SerializeField] private float slotSpacing = 70f;
    [Tooltip("심장이 걸린 칸 (왼쪽)")]
    [Range(1, 6)]
    [SerializeField] private int heartSlot = 3;
    [Tooltip("심장 무게 (kg, 숨김)")]
    [SerializeField] private float heartMass = 6f;
    [Tooltip("흑요석 추 1개 무게 (kg, 숨김)")]
    [SerializeField] private float weightMass = 3f;
    [Tooltip("걸 수 있는 추 최대 개수")]
    [Range(1, 3)]
    [SerializeField] private int maxWeights = 3;

    [Header("클리어 처리")]
    [Tooltip("사자의 서 단서 슬롯 (-1이면 없음)")]
    [SerializeField] private int clueIndex = 2;
    [TextArea(2, 4)]
    [SerializeField] private string clueText = "망자의 심장(H)은 밤의 흑요석 추(D) 두 개의 무게와 정확히 같도다. (H = 2D)";
    [Tooltip("클리어 보상 아이템 ID (비우면 지급 안 함)")]
    [SerializeField] private string rewardItemId = AnubisItemIds.Obsidian;
    [TextArea(1, 3)]
    [SerializeField] private string clearMessage = "지렛대가 완벽한 수평을 이루었습니다!\n사자의 서에 세 번째 비문이 기록되었습니다.";

    [Header("지렛대 그래픽")]
    [SerializeField] private RectTransform leverBar;
    [SerializeField] private RectTransform heartObject;   // 지렛대 자식 (왼쪽)
    [SerializeField] private RectTransform rightHook;     // 지렛대 자식 (오른쪽, 움직임)
    [SerializeField] private GameObject[] weights = new GameObject[3];

    [Header("조작 UI")]
    [SerializeField] private Button btnDistMinus;
    [SerializeField] private Button btnDistPlus;
    [SerializeField] private Button btnCountMinus;
    [SerializeField] private Button btnCountPlus;
    [SerializeField] private Button btnCheck;
    [SerializeField] private Button btnClose;
    [SerializeField] private TMP_Text textDistance;
    [SerializeField] private TMP_Text textCount;
    [SerializeField] private TMP_Text textFeedback;
    [SerializeField] private TMP_Text textAttempts;

    [Header("성공 패널")]
    [SerializeField] private GameObject successNotice;
    [SerializeField] private TMP_Text textSuccessNotice;

    private int distance = 1;   // 오른쪽 걸이 칸
    private int count = 1;      // 추 개수
    private int attempts;
    private bool isCleared;
    private bool isTesting;
    private bool rewarded;
    private Coroutine tiltRoutine;

    private float LeftTorque => heartMass * heartSlot;
    private float RightTorque => weightMass * count * distance;

    private void Start()
    {
        if (btnDistMinus != null) btnDistMinus.onClick.AddListener(() => ChangeDistance(-1));
        if (btnDistPlus != null) btnDistPlus.onClick.AddListener(() => ChangeDistance(1));
        if (btnCountMinus != null) btnCountMinus.onClick.AddListener(() => ChangeCount(-1));
        if (btnCountPlus != null) btnCountPlus.onClick.AddListener(() => ChangeCount(1));
        if (btnCheck != null) btnCheck.onClick.AddListener(CheckBalance);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        if (textSuccessNotice != null) textSuccessNotice.text = clearMessage;
        if (successNotice != null) successNotice.SetActive(false);
        if (textFeedback != null) textFeedback.text = "";

        if (heartObject != null) heartObject.anchoredPosition = new Vector2(-heartSlot * slotSpacing, heartObject.anchoredPosition.y);

        UpdateVisuals();
    }

    private void OnEnable()
    {
        if (leverBar != null && !isCleared) leverBar.localRotation = Quaternion.identity;
        isTesting = false;
    }

    private void ChangeDistance(int delta)
    {
        if (isCleared || isTesting) return;
        distance = Mathf.Clamp(distance + delta, 1, slotCount);
        ClearFeedback();
        UpdateVisuals();
    }

    private void ChangeCount(int delta)
    {
        if (isCleared || isTesting) return;
        count = Mathf.Clamp(count + delta, 1, Mathf.Min(maxWeights, weights.Length));
        ClearFeedback();
        UpdateVisuals();
    }

    private void CheckBalance()
    {
        if (isCleared || isTesting) return;
        attempts++;

        float diff = RightTorque - LeftTorque; 
        if (Mathf.Abs(diff) < 0.01f)
        {
            isCleared = true;
            if (leverBar != null) leverBar.localRotation = Quaternion.identity;
            if (successNotice != null) successNotice.SetActive(true);
            ClearFeedback();
            GiveReward();
        }
        else
        {
            float angle = Mathf.Clamp(-diff * 1.2f, -15f, 15f);
            if (tiltRoutine != null) StopCoroutine(tiltRoutine);
            tiltRoutine = StartCoroutine(TiltRoutine(angle));

            SetFeedback(diff < 0
                ? "<color=#FF6655>지렛대가 심장 쪽(왼쪽)으로 기울어집니다.</color>"
                : "<color=#FF6655>지렛대가 흑요석 쪽(오른쪽)으로 기울어집니다.</color>");
        }

        UpdateVisuals();
    }

    private IEnumerator TiltRoutine(float angle)
    {
        isTesting = true;
        yield return Rotate(Quaternion.Euler(0, 0, angle), 0.35f);
        yield return new WaitForSeconds(1.2f);
        yield return Rotate(Quaternion.identity, 0.3f);
        isTesting = false;
        tiltRoutine = null;
    }

    private IEnumerator Rotate(Quaternion target, float duration)
    {
        if (leverBar == null) yield break;
        Quaternion start = leverBar.localRotation;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            leverBar.localRotation = Quaternion.Slerp(start, target, t / duration);
            yield return null;
        }
        leverBar.localRotation = target;
    }

    private void GiveReward()
    {
        if (rewarded) return;
        rewarded = true;
        if (clueIndex >= 0 && !string.IsNullOrEmpty(clueText) && AnubisNotebookManager.Instance != null)
            AnubisNotebookManager.Instance.UnlockClue(clueIndex, clueText);
        if (!string.IsNullOrEmpty(rewardItemId) && ItemInventory.Instance != null)
            ItemInventory.Instance.Grant(rewardItemId);
        Debug.Log($"[{name}] 클리어! 추 {count}개 x {distance}칸, 시도 {attempts}번", this);
    }

    private void UpdateVisuals()
    {
        if (rightHook != null) rightHook.anchoredPosition = new Vector2(distance * slotSpacing, 0f);

        for (int i = 0; i < weights.Length; i++)
            if (weights[i] != null) weights[i].SetActive(i < count);

        if (textDistance != null) textDistance.text = $"걸이 위치\n<b>{distance} 칸</b>";
        if (textCount != null) textCount.text = $"흑요석 추\n<b>{count} 개</b>";
        if (textAttempts != null) textAttempts.text = $"시도 횟수: {attempts}";
    }

    private void SetFeedback(string msg) { if (textFeedback != null) textFeedback.text = msg; }
    private void ClearFeedback() { if (textFeedback != null) textFeedback.text = ""; }

    [ContextMenu("테스트: 정답 상태로 맞추기 (추 2개, 3칸)")]
    private void TestSetAnswer()
    {
        count = Mathf.Min(2, maxWeights);
        distance = Mathf.Min(3, slotCount);
        UpdateVisuals();
        Debug.Log($"[{name}] 왼쪽 {LeftTorque} / 오른쪽 {RightTorque} (평형 확인을 눌러 확인)", this);
    }
}