using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 아누비스의 지렛대 천칭 (서브퍼즐 3). 토크(무게 x 거리) 600 맞추기
// 패널 루트에 부착
public class AnubisLeverPuzzle : MonoBehaviour
{
    [Header("시각 그래픽 요소")]
    [SerializeField] private RectTransform leverBar;
    [SerializeField] private RectTransform rightHookAnchor;
    [SerializeField] private GameObject[] obsidianWeights;

    [Header("UI 텍스트")]
    [SerializeField] private TMP_Text textDistanceDisplay;
    [SerializeField] private TMP_Text textWeightCountDisplay;
    [SerializeField] private TMP_Text textHintNotice;
    [SerializeField] private TMP_Text textFeedbackNotice;

    [Header("조작 버튼")]
    [SerializeField] private Button btnDistMinus;
    [SerializeField] private Button btnDistPlus;
    [SerializeField] private Button btnCountMinus;
    [SerializeField] private Button btnCountPlus;
    [SerializeField] private Button btnCheckBalance;
    [SerializeField] private Button btnClose;

    [Header("성공 패널")]
    [SerializeField] private GameObject successNotice;

    private readonly float leftTorque = 4f * 150f;
    private readonly float singleWeightMass = 2f;

    private int currentDistance = 1;
    private int currentWeightCount = 1;

    private readonly float[] distanceXPositions = { 75f, 150f, 225f, 300f };

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

        UpdateVisuals();
    }

    private void OnEnable()
    {
        ClearFeedback();
        if (leverBar != null && !isCleared)
            leverBar.localRotation = Quaternion.identity;
    }

    private void ChangeDistance(int delta)
    {
        if (isCleared || isTesting) return;
        ClearFeedback();

        currentDistance = Mathf.Clamp(currentDistance + delta, 1, 4);
        UpdateVisuals();
    }

    private void ChangeWeightCount(int delta)
    {
        if (isCleared || isTesting) return;
        ClearFeedback();

        currentWeightCount = Mathf.Clamp(currentWeightCount + delta, 1, 3);
        UpdateVisuals();
    }

    private void CheckLeverBalance()
    {
        if (isCleared || isTesting) return;

        float currentDistX = distanceXPositions[currentDistance - 1];
        float rightTorque = (singleWeightMass * currentWeightCount) * currentDistX;
        float diff = rightTorque - leftTorque;

        // 정답 조건: 토크가 600으로 일치 (추 2개 x 2칸(150px))
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

            if (ItemInventory.Instance != null)
                ItemInventory.Instance.Grant(AnubisItemIds.Obsidian);
        }
        else
        {
            float targetAngle = Mathf.Clamp(-diff * 0.03f, -15f, 15f);
            StartCoroutine(TiltAnimationRoutine(targetAngle));

            string failMsg = (diff < 0)
                ? "<color=#FF4444>불균형: 좌측 심장의 무게가 더 무거워 지렛대가 왼쪽으로 기울어집니다!</color>"
                : "<color=#FF4444>불균형: 우측 흑요석의 회전력이 과도하여 지렛대가 오른쪽으로 처박힙니다!</color>";

            ShowFeedback(failMsg);
        }
    }

    private IEnumerator TiltAnimationRoutine(float targetAngle)
    {
        if (leverBar == null) yield break;

        float duration = 0.35f;
        float elapsed = 0f;
        Quaternion startRot = leverBar.localRotation;
        Quaternion endRot = Quaternion.Euler(0, 0, targetAngle);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            leverBar.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            yield return null;
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
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Quaternion startRot = leverBar.localRotation;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                leverBar.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, elapsed / duration);
                yield return null;
            }
            leverBar.localRotation = Quaternion.identity;
        }

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

    private void UpdateVisuals()
    {
        if (rightHookAnchor != null)
        {
            float targetX = distanceXPositions[currentDistance - 1];
            rightHookAnchor.anchoredPosition = new Vector2(targetX, 0f);
        }

        if (obsidianWeights != null)
        {
            for (int i = 0; i < obsidianWeights.Length; i++)
            {
                if (obsidianWeights[i] != null)
                    obsidianWeights[i].SetActive(i < currentWeightCount);
            }
        }

        if (textDistanceDisplay != null)
            textDistanceDisplay.text = $"걸이 위치\n<b>{currentDistance} 칸</b>";

        if (textWeightCountDisplay != null)
            textWeightCountDisplay.text = $"흑요석 추\n<b>{currentWeightCount} 개</b>";
    }
}