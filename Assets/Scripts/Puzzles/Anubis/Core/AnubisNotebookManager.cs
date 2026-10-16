using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AnubisNotebookManager : MonoBehaviour
{
    public static AnubisNotebookManager Instance;

    [Header("수첩 UI 연결")]
    [Tooltip("수첩 팝업 패널 (루트)")]
    [SerializeField] private GameObject notebookPopup;
    [Tooltip("(선택) 상단 아이콘 버튼. 제거했다면 비워둠")]
    [SerializeField] private Button btnToggleNotebook;
    [Tooltip("(선택) 수첩 내부 닫기 버튼")]
    [SerializeField] private Button btnCloseNotebook;

    [Header("단서 텍스트 (인덱스 0~6)")]
    [SerializeField] private TMP_Text[] clueSlotTexts;

    [Header("단서 개수 [추가]")]
    [Tooltip("실제로 쓰는 비문 수 (서브 퍼즐 4개 = 4). 넘는 칸은 수첩에서 숨김")]
    [Range(1, 7)]
    [SerializeField] private int clueCount = 4;

    private readonly string[] clueData = new string[7];

    // [추가] 비문이 해독될 때 (메인 저울 화면의 사자의 서 판이 받음)
    public event Action<int, string> OnClueUnlocked;

    // [추가] 실제로 쓰는 비문 수 (4)
    public int ClueCount => Mathf.Min(clueCount, clueData.Length);

    // [추가] 해독된 비문 글. 아직이면 null
    public string GetClue(int index) =>
        (index >= 0 && index < clueData.Length) ? clueData[index] : null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (notebookPopup != null)
            notebookPopup.SetActive(false);

        if (btnToggleNotebook != null)
            btnToggleNotebook.onClick.AddListener(ToggleNotebook);

        if (btnCloseNotebook != null)
            btnCloseNotebook.onClick.AddListener(ToggleNotebook);

        for (int i = 0; i < clueSlotTexts.Length; i++)
        {
            if (clueSlotTexts[i] == null) continue;

            // [수정] 서브 퍼즐 수(clueCount)를 넘는 칸은 숨김 (기존 7칸 -> 4칸)
            bool used = i < clueCount;
            clueSlotTexts[i].gameObject.SetActive(used);
            if (used) clueSlotTexts[i].text = $"<b>[비문 {i + 1}]</b> 아직 해독되지 않은 고대 상형문자입니다.";
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.mKey.wasPressedThisFrame)
            ToggleNotebook();
    }

    public void ToggleNotebook()
    {
        if (notebookPopup != null)
            notebookPopup.SetActive(!notebookPopup.activeSelf);
    }

    public void UnlockClue(int clueIndex, string clueText)
    {
        if (clueIndex < 0 || clueIndex >= clueSlotTexts.Length || clueIndex >= clueCount) return;

        clueData[clueIndex] = clueText;

        if (clueSlotTexts[clueIndex] != null)
        {
            clueSlotTexts[clueIndex].text = $"<b><color=#FFD700>[비문 {clueIndex + 1} 해독 완료]</color></b>\n\"{clueText}\"";
        }

        Debug.Log($"<color=cyan>[사자의 서] 단서 {clueIndex + 1} 해금: {clueText}</color>");
        OnClueUnlocked?.Invoke(clueIndex, clueText); // [추가]
    }
}