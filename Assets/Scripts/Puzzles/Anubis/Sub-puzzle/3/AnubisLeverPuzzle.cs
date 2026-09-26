using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnubisLeverPuzzle : MonoBehaviour
{
    [Header("시각 연출 UI")]
    [SerializeField] private RectTransform leverBar;            // 기울어질 지렛대 막대
    [SerializeField] private TMP_Text textDistanceDisplay;      // 걸이 위치 표시 (1~4칸)
    [SerializeField] private TMP_Text textWeightCountDisplay;   // 추 개수 표시 (1~3개)
    [SerializeField] private TMP_Text textHintNotice;           // 상단 안내 (수식 은폐)
    [SerializeField] private TMP_Text textFeedbackNotice;       // 판정 피드백 (기울어짐 경고)

    [Header("조작 버튼")]
    [SerializeField] private Button btnDistMinus;               // 거리 감소 (<)
    [SerializeField] private Button btnDistPlus;                // 거리 증가 (>)
    [SerializeField] private Button btnCountMinus;              // 추 개수 감소 (-)
    [SerializeField] private Button btnCountPlus;               // 추 개수 증가 (+)
    [SerializeField] private Button btnCheckBalance;            // 고정핀 해제 (판정 레버)
    [SerializeField] private Button btnClose;                   // 나가기 버튼

    [Header("성공 패널")]
    [SerializeField] private GameObject successNotice;

    private readonly float leftTorque = 4f;
    private readonly float singleWeightMass = 2f;

    private int currentDistance = 1;
    private int currentWeightCount = 1;

    private bool isCleared = false;
    private bool isTesting = false;
    private Coroutine feedbackCoroutine;

    private void Start()
    {
        if (btnDistMinus != null) btnDistMinus.onClick.AddListener(() => ChangeDistance(-1));
        if (btnDistPlus != null) btnDistPlus.onClick.AddListener(() => ChangeDistance(1));
        if (btnCountMinus != null) btnCountMinus.onClick.AddListener(() => ChangeWeightCount(-1));
        if (btnCountPlus != null) btnCountPlus.onClick.AddListener(() => ChangeWeightCount(1));
        if (btnCheckBalance != null) btnCheckBalance.onClick.AddListener(CheckLeverBalance);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        if (successNotice != null) successNotice.SetActive(false);
        if (textFeedbackNotice != null) textFeedbackNotice.text = "";

        if (textHintNotice != null)
        {
            textHintNotice.text = "<b>[아누비스의 지렛대 천칭]</b>\n흑요석 추의 개수와 걸이 위치를 조절하여 좌측 심장과 완벽한 회전 평형을 이루십시오.";
        }

        if (leverBar != null) leverBar.localRotation = Quaternion.identity;

        UpdateUI();
    }

    private void OnEnable()
    {
        ClearFeedback();
        if (leverBar != null && !isCleared) leverBar.localRotation = Quaternion.identity;
    }

    private void ChangeDistance(int delta)
    {
        if (isCleared || isTesting) return;
        ClearFeedback();
        currentDistance = Mathf.Clamp(currentDistance + delta, 1, 4);
        UpdateUI();
    }

    private void ChangeWeightCount(int delta)
    {
        if (isCleared || isTesting) return;
        ClearFeedback();
        currentWeightCount = Mathf.Clamp(currentWeightCount + delta, 1, 3);
        UpdateUI();
    }

    private void CheckLeverBalance()
    {
        if (isCleared || isTesting) return;

        float rightTorque = (singleWeightMass * currentWeightCount) * currentDistance;
        float diff = rightTorque - leftTorque;

        // 정답 조건: 토크가 4로 일치할 때
        // (1) 추 2개 x 1칸 = 4
        // (2) 추 1개 x 2칸 = 4
        if (Mathf.Approximately(leftTorque, rightTorque))
        {
            isCleared = true;
            ClearFeedback();

            if (leverBar != null) leverBar.localRotation = Quaternion.identity;

            if (successNotice != null) successNotice.SetActive(true);

            if (AnubisNotebookManager.Instance != null)
            {
                AnubisNotebookManager.Instance.UnlockClue(2,
                    "망자의 심장(H)은 밤의 흑요석 추(D) 두 개의 무게와 정확히 같도다. (H = 2D)");
            }
        }
        else
        {
            float tiltAngle = Mathf.Clamp(-diff * 5f, -18f, 18f);
            if (leverBar != null) leverBar.localRotation = Quaternion.Euler(0f, 0f, tiltAngle);

            string failMsg = (diff < 0)
                ? "<color=#FF4444>불균형: 좌측 심장의 무게가 더 무거워 지렛대가 왼쪽으로 곤두박질칩니다!</color>"
                : "<color=#FF4444>불균형: 우측 흑요석의 회전력이 과도하여 지렛대가 오른쪽으로 처박힙니다!</color>";

            ShowFeedback(failMsg);
        }
    }

    private void ShowFeedback(string msg)
    {
        ClearFeedback();
        feedbackCoroutine = StartCoroutine(FeedbackRoutine(msg));
    }

    private IEnumerator FeedbackRoutine(string msg)
    {
        isTesting = true;
        if (textFeedbackNotice != null) textFeedbackNotice.text = msg;

        yield return new WaitForSeconds(2.0f);

        if (textFeedbackNotice != null) textFeedbackNotice.text = "";
        if (leverBar != null && !isCleared)
            leverBar.localRotation = Quaternion.identity;

        isTesting = false;
    }

    private void ClearFeedback()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }
        if (textFeedbackNotice != null) textFeedbackNotice.text = "";
        isTesting = false;
    }

    private void UpdateUI()
    {
        if (textDistanceDisplay != null)
            textDistanceDisplay.text = $"걸이 위치\n<b>{currentDistance} 칸</b>";

        if (textWeightCountDisplay != null)
            textWeightCountDisplay.text = $"흑요석 추\n<b>{currentWeightCount} 개</b>";
    }
}