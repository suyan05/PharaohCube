using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ThothPuzzleUI : MonoBehaviour
{
    [Header("매니저 참조")]
    [SerializeField] private ThothPuzzleManager puzzleManager;

    [Header("서브 퍼즐 UI")]
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Button btnRestorePapyrus;

    [Header("슬롯 텍스트 (4개)")]
    [SerializeField] private TMP_Text[] slotTexts;

    [Header("상형문자 조작 버튼")]
    [SerializeField] private Button btnEye;
    [SerializeField] private Button btnWater;
    [SerializeField] private Button btnBird;
    [SerializeField] private Button btnSnake;
    [SerializeField] private Button btnReset;

    [Header("클리어 연출")]
    [SerializeField] private GameObject clearText;

    private void Start()
    {
        if (btnEye != null) btnEye.onClick.AddListener(() => puzzleManager.InputGlyph("눈"));
        if (btnWater != null) btnWater.onClick.AddListener(() => puzzleManager.InputGlyph("물"));
        if (btnBird != null) btnBird.onClick.AddListener(() => puzzleManager.InputGlyph("새"));
        if (btnSnake != null) btnSnake.onClick.AddListener(() => puzzleManager.InputGlyph("뱀"));
        if (btnReset != null) btnReset.onClick.AddListener(() => puzzleManager.ResetInput());

        if (btnRestorePapyrus != null)
            btnRestorePapyrus.onClick.AddListener(OnRestoreClicked);

        if (clearText != null) clearText.SetActive(false);
        UpdateSlots(new List<string>());
        UpdateHintUI();
    }

    private void OnEnable()
    {
        if (puzzleManager != null)
        {
            puzzleManager.OnInputChanged += UpdateSlots;
            puzzleManager.OnPuzzleCleared += HandleClear;
        }
    }

    private void OnDisable()
    {
        if (puzzleManager != null)
        {
            puzzleManager.OnInputChanged -= UpdateSlots;
            puzzleManager.OnPuzzleCleared -= HandleClear;
        }
    }

    private void OnRestoreClicked()
    {
        puzzleManager.RestorePapyrus();
        UpdateHintUI();
        btnRestorePapyrus.interactable = false;
    }

    private void UpdateHintUI()
    {
        if (hintText == null) return;

        if (puzzleManager != null && puzzleManager.IsPapyrusRestored)
        {
            hintText.text = "<color=#FFE45C>[복원된 파피루스 비문]</color>\n'태양의 <color=red>[눈]</color>이 흐르는 <color=#00FFFF>[물]</color>을 비추고, 창공의 <color=green>[새]</color>가 기어가는 <color=yellow>[뱀]</color>을 낚아채리라.'";
        }
        else
        {
            hintText.text = "<color=#999999>[훼손된 고대 파피루스]</color>\n'문장이 찢겨 나가 올바른 주문 순서를 알 수 없습니다. 조각을 먼저 복원하세요.'";
        }
    }

    private void UpdateSlots(List<string> inputs)
    {
        if (slotTexts == null) return;

        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] == null) continue;

            if (i < inputs.Count)
            {
                slotTexts[i].text = inputs[i];
                slotTexts[i].color = Color.yellow;
            }
            else
            {
                slotTexts[i].text = "빈 슬롯";
                slotTexts[i].color = Color.gray;
            }
        }
    }

    private void HandleClear()
    {
        if (clearText != null) clearText.SetActive(true);
    }
}