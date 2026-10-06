using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BastetClawPatternPuzzle : PuzzleBase
{
    [System.Serializable]
    public class PatternSlot
    {
        public Button slotButton;
        public TMP_Text slotText;
        public Image slotBackground;
        public bool isBlank;
        public int fixedValue = 0;
        public int targetValue = 0;
        public int currentValue = 0; 
    }

    [Header("4x3 벽화 발톱 슬롯 (총 12칸: 4열 x 3행)")]
    [SerializeField] private PatternSlot[] patternSlots = new PatternSlot[12];

    [Header("하단 후보 문양 버튼 (1: /, 2: //, 3: ///)")]
    [SerializeField] private Button[] candidateButtons = new Button[3];

    [Header("UI 안내 및 제어")]
    [SerializeField] private GameObject puzzleUIRoot;
    [SerializeField] private TMP_Text textStatusNotice;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private Button btnSubmit;
    [SerializeField] private Button btnReset;
    [SerializeField] private Button btnClose;

    // 슬래시(/) 기반 발톱 긁힘 문양 표기
    private readonly string[] clawSymbols = { "?", "/", "//", "///" };

    private int selectedBlankSlotIndex = -1;
    private bool isChecking = false;

    // 테마 색상
    private readonly Color colSlotFixed = new Color(0.25f, 0.2f, 0.32f, 1f);     // 고정 슬롯
    private readonly Color colSlotBlank = new Color(0.18f, 0.14f, 0.24f, 1f);     // 빈칸 기본
    private readonly Color colSlotSelected = new Color(0.7f, 0.4f, 0.95f, 1f);   // 선택된 빈칸
    private readonly Color colWrong = new Color(0.9f, 0.25f, 0.25f, 1f);         // 오답 붉은색
    private readonly Color colGold = new Color(1f, 0.85f, 0.2f, 1f);             // 정답 황금빛

    private void Awake()
    {
        for (int i = 0; i < patternSlots.Length; i++)
        {
            int index = i;
            if (patternSlots[index].slotButton != null)
            {
                patternSlots[index].slotButton.onClick.AddListener(() => OnSlotClicked(index));
            }
        }

        for (int i = 0; i < candidateButtons.Length; i++)
        {
            int candidateValue = i + 1;
            if (candidateButtons[i] != null)
            {
                candidateButtons[i].onClick.AddListener(() => OnCandidateClicked(candidateValue));
            }
        }

        if (btnSubmit != null) btnSubmit.onClick.AddListener(Submit);
        if (btnReset != null) btnReset.onClick.AddListener(ResetBlanks);
        if (btnClose != null) btnClose.onClick.AddListener(ClosePuzzle);
    }

    public override void Open()
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        base.Open();

        if (successNotice != null) successNotice.SetActive(false);

        if (!IsCleared)
        {
            Reset();
        }
    }

    private void OnSlotClicked(int index)
    {
        if (IsCleared || isChecking) return;
        if (!patternSlots[index].isBlank) return;

        selectedBlankSlotIndex = index;
        UpdateSlotVisuals();

        if (textStatusNotice != null)
            textStatusNotice.text = $"{index + 1}번 빈칸을 선택했습니다. 하단 후보에서 알맞은 문양을 고르십시오.";
    }

    private void OnCandidateClicked(int value)
    {
        if (IsCleared || isChecking) return;

        if (selectedBlankSlotIndex == -1)
        {
            for (int i = 0; i < patternSlots.Length; i++)
            {
                if (patternSlots[i].isBlank && patternSlots[i].currentValue == 0)
                {
                    selectedBlankSlotIndex = i;
                    break;
                }
            }
        }

        if (selectedBlankSlotIndex != -1)
        {
            patternSlots[selectedBlankSlotIndex].currentValue = value;
            selectedBlankSlotIndex = -1;
            UpdateSlotVisuals();
        }
    }

    private void UpdateSlotVisuals()
    {
        for (int i = 0; i < patternSlots.Length; i++)
        {
            var slot = patternSlots[i];

            if (!slot.isBlank)
            {
                if (slot.slotText != null) slot.slotText.text = clawSymbols[slot.fixedValue];
                if (slot.slotBackground != null) slot.slotBackground.color = colSlotFixed;
            }
            else
            {
                if (slot.slotText != null)
                {
                    slot.slotText.text = (slot.currentValue > 0) ? clawSymbols[slot.currentValue] : "?";
                }

                if (slot.slotBackground != null)
                {
                    slot.slotBackground.color = (i == selectedBlankSlotIndex) ? colSlotSelected : colSlotBlank;
                }
            }
        }
    }

    public override void Submit()
    {
        if (IsCleared || isChecking) return;

        for (int i = 0; i < patternSlots.Length; i++)
        {
            if (patternSlots[i].isBlank && patternSlots[i].currentValue == 0)
            {
                if (textStatusNotice != null)
                    textStatusNotice.text = "<color=#FFA726>5개의 빈칸(?)을 모두 채운 후 확인하십시오.</color>";
                return;
            }
        }

        if (Validate())
        {
            OnSuccessInternal();
        }
        else
        {
            StartCoroutine(FailRoutine());
        }
    }

    protected override bool Validate()
    {
        for (int i = 0; i < patternSlots.Length; i++)
        {
            if (patternSlots[i].isBlank)
            {
                if (patternSlots[i].currentValue != patternSlots[i].targetValue)
                    return false;
            }
        }
        return true;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal();

        for (int i = 0; i < patternSlots.Length; i++)
        {
            if (patternSlots[i].slotBackground != null)
                patternSlots[i].slotBackground.color = colGold;
            if (patternSlots[i].slotText != null)
                patternSlots[i].slotText.color = new Color(0.2f, 0.15f, 0.05f, 1f);
        }

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FFD700>4x3 벽화의 좌우 대칭과 세로 순환이 완성되었습니다! (메인 반복 규칙 획득)</color>";

        if (successNotice != null) successNotice.SetActive(true);
    }

    private IEnumerator FailRoutine()
    {
        isChecking = true;

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FF4444>가로 대칭 또는 세로 순환 규칙이 어긋난 빈칸이 있습니다!</color>";

        for (int i = 0; i < patternSlots.Length; i++)
        {
            if (patternSlots[i].isBlank && patternSlots[i].currentValue != patternSlots[i].targetValue)
            {
                if (patternSlots[i].slotBackground != null)
                    patternSlots[i].slotBackground.color = colWrong;
            }
        }

        yield return new WaitForSeconds(0.7f);

        UpdateSlotVisuals();

        if (textStatusNotice != null)
            textStatusNotice.text = "행별 좌우 대칭과 열별 변화 규칙을 다시 분석하십시오.";

        isChecking = false;
    }

    private void ResetBlanks()
    {
        if (IsCleared || isChecking) return;

        for (int i = 0; i < patternSlots.Length; i++)
        {
            if (patternSlots[i].isBlank)
            {
                patternSlots[i].currentValue = 0;
            }
        }

        selectedBlankSlotIndex = -1;
        UpdateSlotVisuals();

        if (textStatusNotice != null)
            textStatusNotice.text = "빈칸이 초기화되었습니다.";
    }

    public override void Reset()
    {
        base.Reset();

        int[] fullGridPattern = {
            1, 2, 2, 1,
            2, 3, 3, 2,
            3, 1, 1, 3
        };

        HashSet<int> blankIndices = new HashSet<int> { 1, 4, 6, 9, 11 };

        for (int i = 0; i < patternSlots.Length; i++)
        {
            if (blankIndices.Contains(i))
            {
                patternSlots[i].isBlank = true;
                patternSlots[i].targetValue = fullGridPattern[i];
                patternSlots[i].currentValue = 0;
            }
            else
            {
                patternSlots[i].isBlank = false;
                patternSlots[i].fixedValue = fullGridPattern[i];
            }
        }

        selectedBlankSlotIndex = -1;
        UpdateSlotVisuals();

        if (textStatusNotice != null)
            textStatusNotice.text = "가로 대칭과 세로 순환 규칙을 분석하여 5개의 빈칸(?)을 채우십시오.";
    }

    public void ClosePuzzle()
    {
        if (puzzleUIRoot != null)
            puzzleUIRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}