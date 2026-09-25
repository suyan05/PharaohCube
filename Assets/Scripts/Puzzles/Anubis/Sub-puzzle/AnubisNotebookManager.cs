using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnubisNotebookManager : MonoBehaviour
{
    public static AnubisNotebookManager Instance;

    [Header("수첩 UI 연결")]
    [SerializeField] private GameObject notebookPopup; // 수첩 팝업 패널
    [SerializeField] private Button btnToggleNotebook; // 수첩 열기/닫기 아이콘 버튼
    [SerializeField] private Button btnCloseNotebook;  // 내부 닫기 버튼
    [SerializeField] private TMP_Text[] clueSlotTexts; // 7개 단서 텍스트 UI (인덱스 0~6)

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