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

    private readonly string[] clueData = new string[7];

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
            if (clueSlotTexts[i] != null)
                clueSlotTexts[i].text = $"<b>[비문 {i + 1}]</b> 아직 해독되지 않은 고대 상형문자입니다.";
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
        if (clueIndex < 0 || clueIndex >= clueSlotTexts.Length) return;

        clueData[clueIndex] = clueText;

        if (clueSlotTexts[clueIndex] != null)
        {
            clueSlotTexts[clueIndex].text = $"<b><color=#FFD700>[비문 {clueIndex + 1} 해독 완료]</color></b>\n\"{clueText}\"";
        }

        Debug.Log($"<color=cyan>[사자의 서] 단서 {clueIndex + 1} 해금: {clueText}</color>");
    }
}