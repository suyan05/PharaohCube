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

    [Header("단지 UI 텍스트 (TMP)")]
    [SerializeField] private TMP_Text textJar8L;
    [SerializeField] private TMP_Text textJar5L;
    [SerializeField] private TMP_Text textJar3L;

    [Header("조작 버튼")]
    [SerializeField] private Button btnJar8L;
    [SerializeField] private Button btnJar5L;
    [SerializeField] private Button btnJar3L;
    [SerializeField] private Button btnReset;
    [SerializeField] private Button btnClose;

    [Header("성공 알림 패널 및 텍스트 (크래시 방지)")]
    [SerializeField] private GameObject successNotice;
    [SerializeField] private TMP_Text textSuccessNotice;

    private int selectedJar = -1; // -1: 미선택, 0: 8L, 1: 5L, 2: 3L
    private bool isCleared = false;

    private void Start()
    {
        if (textSuccessNotice != null)
        {
            textSuccessNotice.text = "제단 해제 성공! 사자의 서에 첫 번째 비문이 해독되었습니다.";
        }

        if (btnJar8L != null) btnJar8L.onClick.AddListener(() => OnClickJar(0));
        if (btnJar5L != null) btnJar5L.onClick.AddListener(() => OnClickJar(1));
        if (btnJar3L != null) btnJar3L.onClick.AddListener(() => OnClickJar(2));
        if (btnReset != null) btnReset.onClick.AddListener(ResetDecanter);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        if (successNotice != null) successNotice.SetActive(false);

        UpdateUI();
    }

    public void OnClickJar(int index)
    {
        if (isCleared) return;

        if (selectedJar == -1)
        {
            selectedJar = index;
            HighlightJar(index, true);
        }
        else
        {
            if (selectedJar != index)
            {
                PourSand(selectedJar, index);
            }
            HighlightJar(selectedJar, false);
            selectedJar = -1; 
        }

        UpdateUI();
        CheckClearCondition();
    }

    private void PourSand(int from, int to)
    {
        int move = 0;
        if (from == 0 && to == 1) { move = Mathf.Min(curA, maxB - curB); curA -= move; curB += move; }
        else if (from == 0 && to == 2) { move = Mathf.Min(curA, maxC - curC); curA -= move; curC += move; }
        else if (from == 1 && to == 0) { move = Mathf.Min(curB, maxA - curA); curB -= move; curA += move; }
        else if (from == 1 && to == 2) { move = Mathf.Min(curB, maxC - curC); curB -= move; curC += move; }
        else if (from == 2 && to == 0) { move = Mathf.Min(curC, maxA - curA); curC -= move; curA += move; }
        else if (from == 2 && to == 1) { move = Mathf.Min(curC, maxB - curB); curC -= move; curB += move; }
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
        curA = 8;
        curB = 0;
        curC = 0;
        selectedJar = -1;
        isCleared = false;
        if (successNotice != null) successNotice.SetActive(false);
        UpdateUI();
    }

    private void HighlightJar(int index, bool active)
    {
        // 필요 시 단지 강조 연출
    }

    private void UpdateUI()
    {
        if (textJar8L != null) textJar8L.text = $"사자 단지 (8L)\n<b>{curA} / {maxA} L</b>";
        if (textJar5L != null) textJar5L.text = $"자칼 단지 (5L)\n<b>{curB} / {maxB} L</b>";
        if (textJar3L != null) textJar3L.text = $"원숭이 단지 (3L)\n<b>{curC} / {maxC} L</b>";
    }
}