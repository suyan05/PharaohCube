using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LionSandDecanter : MonoBehaviour
{
    private readonly int maxA = 8;
    private readonly int maxB = 5;
    private readonly int maxC = 3;

    private int curA = 8;
    private int curB = 0;
    private int curC = 0;

    [Header("단지 버튼 (터치 영역)")]
    [SerializeField] private Button btnJar8L;
    [SerializeField] private Button btnJar5L;
    [SerializeField] private Button btnJar3L;

    [Header("모래 그래픽 게이지 (Image Type: Filled)")]
    [SerializeField] private Image fillSand8L;
    [SerializeField] private Image fillSand5L;
    [SerializeField] private Image fillSand3L;

    [Header("선택 시 강조 테두리 (Glow/Outline)")]
    [SerializeField] private GameObject highlight8L;
    [SerializeField] private GameObject highlight5L;
    [SerializeField] private GameObject highlight3L;

    [Header("용량 표시 텍스트")]
    [SerializeField] private TMP_Text textJar8L;
    [SerializeField] private TMP_Text textJar5L;
    [SerializeField] private TMP_Text textJar3L;

    [Header("시스템 버튼 & 알림창")]
    [SerializeField] private Button btnReset;
    [SerializeField] private Button btnClose;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private TMP_Text textSuccessNotice;

    private int selectedJar = -1;                                   // -1: 미선택, 0: 8L, 1: 5L, 2: 3L
    private bool isCleared = false;
    private bool isPouring = false;                                 // 모래가 쏟아지는 연출 중 조작 차단

    private void Start()
    {
        if (textSuccessNotice != null)
            textSuccessNotice.text = "제단 해제 성공! 사자의 서에 첫 번째 비문이 해독되었습니다.";

        if (btnJar8L != null) btnJar8L.onClick.AddListener(() => OnClickJar(0));
        if (btnJar5L != null) btnJar5L.onClick.AddListener(() => OnClickJar(1));
        if (btnJar3L != null) btnJar3L.onClick.AddListener(() => OnClickJar(2));
        if (btnReset != null) btnReset.onClick.AddListener(ResetDecanter);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        if (successNotice != null) successNotice.SetActive(false);

        ResetHighlights();
        UpdateVisualsInstant();
    }

    public void OnClickJar(int index)
    {
        if (isCleared || isPouring) return;

        if (selectedJar == -1)
        {
            selectedJar = index;
            SetHighlight(index, true);
        }
        else
        {
            if (selectedJar != index)
            {
                StartCoroutine(PourSandRoutine(selectedJar, index));
            }
            SetHighlight(selectedJar, false);
            selectedJar = -1;
        }
    }

    private IEnumerator PourSandRoutine(int from, int to)
    {
        isPouring = true;

        int move = 0;
        if (from == 0 && to == 1) { move = Mathf.Min(curA, maxB - curB); curA -= move; curB += move; }
        else if (from == 0 && to == 2) { move = Mathf.Min(curA, maxC - curC); curA -= move; curC += move; }
        else if (from == 1 && to == 0) { move = Mathf.Min(curB, maxA - curA); curB -= move; curA += move; }
        else if (from == 1 && to == 2) { move = Mathf.Min(curB, maxC - curC); curB -= move; curC += move; }
        else if (from == 2 && to == 0) { move = Mathf.Min(curC, maxA - curA); curC -= move; curA += move; }
        else if (from == 2 && to == 1) { move = Mathf.Min(curC, maxB - curB); curC -= move; curB += move; }

        float elapsed = 0f;
        float duration = 0.35f;

        float startA = fillSand8L != null ? fillSand8L.fillAmount : 0;
        float startB = fillSand5L != null ? fillSand5L.fillAmount : 0;
        float startC = fillSand3L != null ? fillSand3L.fillAmount : 0;

        float targetA = (float)curA / maxA;
        float targetB = (float)curB / maxB;
        float targetC = (float)curC / maxC;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (fillSand8L != null) fillSand8L.fillAmount = Mathf.Lerp(startA, targetA, t);
            if (fillSand5L != null) fillSand5L.fillAmount = Mathf.Lerp(startB, targetB, t);
            if (fillSand3L != null) fillSand3L.fillAmount = Mathf.Lerp(startC, targetC, t);

            yield return null;
        }

        UpdateVisualsInstant();
        CheckClearCondition();
        isPouring = false;
    }

    private void CheckClearCondition()
    {
        if (curA == 4 && curB == 4)
        {
            isCleared = true;
            if (successNotice != null) successNotice.SetActive(true);

            if (AnubisNotebookManager.Instance != null)
            {
                AnubisNotebookManager.Instance.UnlockClue(0,
                    "가장 비천한 흙의 단지(C)에 태양의 황금(A)을 더해야만 심장(H)의 무게를 얻으리라. (C + A = H)");
            }
        }
    }

    public void ResetDecanter()
    {
        if (isPouring) return;

        curA = 8;
        curB = 0;
        curC = 0;
        selectedJar = -1;
        isCleared = false;
        if (successNotice != null) successNotice.SetActive(false);

        ResetHighlights();
        UpdateVisualsInstant();
    }

    private void SetHighlight(int index, bool active)
    {
        if (index == 0 && highlight8L != null) highlight8L.SetActive(active);
        if (index == 1 && highlight5L != null) highlight5L.SetActive(active);
        if (index == 2 && highlight3L != null) highlight3L.SetActive(active);
    }

    private void ResetHighlights()
    {
        if (highlight8L != null) highlight8L.SetActive(false);
        if (highlight5L != null) highlight5L.SetActive(false);
        if (highlight3L != null) highlight3L.SetActive(false);
    }

    private void UpdateVisualsInstant()
    {
        if (fillSand8L != null) fillSand8L.fillAmount = (float)curA / maxA;
        if (fillSand5L != null) fillSand5L.fillAmount = (float)curB / maxB;
        if (fillSand3L != null) fillSand3L.fillAmount = (float)curC / maxC;

        if (textJar8L != null) textJar8L.text = $"<b>{curA}</b> / {maxA} L";
        if (textJar5L != null) textJar5L.text = $"<b>{curB}</b> / {maxB} L";
        if (textJar3L != null) textJar3L.text = $"<b>{curC}</b> / {maxC} L";
    }
}