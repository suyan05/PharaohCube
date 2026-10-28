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
    // [수정] 난이도를 Inspector에서 조절 (기본값은 기존과 같음)
    [Header("[추가] 난이도 (칸 번호 0 = 왼쪽 위, 4열 기준 오른쪽으로 1씩, 아래로 4씩)")]
    [Tooltip("전체 무늬 12칸 (1 = /, 2 = //, 3 = ///). 규칙: 가로 좌우 대칭, 세로로 1→2→3→1 순환")]
    [SerializeField] private int[] fullGridPattern =
    {
        1, 2, 2, 1,
        2, 3, 3, 2,
        3, 1, 1, 3
    };
    [Tooltip("빈칸(?) 번호. 많을수록 어려움")]
    [SerializeField] private int[] blankIndices = { 1, 4, 6, 9, 11 };

    private readonly string[] clawSymbols = { "?", "/", "//", "///" };

    // [추가] 발톱 자국 그림 (0 = 빈칸, 1~3 = 발톱 1~3줄). 4장 모두 있으면 글자 대신 그림으로 보여 줌
    [Header("[추가] 발톱 자국 그림 (Tools/Bastet/★ 오브젝트·퍼즐 그림 입히기가 넣음)")]
    [SerializeField] private Sprite[] clawSprites = new Sprite[4];
    [SerializeField] private Color artFixed = Color.white;
    [SerializeField] private Color artBlank = new Color(0.92f, 0.9f, 0.85f, 1f);
    [SerializeField] private Color artSelected = new Color(1f, 0.82f, 0.4f, 1f);
    [SerializeField] private Color artWrong = new Color(1f, 0.5f, 0.45f, 1f);
    [SerializeField] private Color artGold = new Color(1f, 0.92f, 0.55f, 1f);

    [Tooltip("그림 모드일 때 안내·후보 글자 색 (파피루스 위 갈색)")]
    [SerializeField] private Color artInk = new Color(0.42f, 0.25f, 0.07f, 1f);

    private bool UseArt => clawSprites != null && clawSprites.Length >= 4 && clawSprites[0] != null && clawSprites[1] != null && clawSprites[2] != null && clawSprites[3] != null;

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
        ApplyArtLook(); // [추가] 그림이 연결돼 있으면 후보 버튼·글자도 그림에 맞춤 (메뉴를 안 눌러도)

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
        isChecking = false; // [수정] 틀림 연출 도중 창을 닫으면 잠긴 채로 남던 문제

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
            BastetSfx.Press(); // [추가] 발톱 자국 새기는 소리
        }
    }

    private void UpdateSlotVisuals()
    {
        for (int i = 0; i < patternSlots.Length; i++)
        {
            var slot = patternSlots[i];

            // [추가] 그림 모드: 칸 그림 = 발톱 자국, 글자는 빈칸 '?'만
            if (UseArt)
            {
                int v = slot.isBlank ? slot.currentValue : slot.fixedValue;
                if (slot.slotBackground != null)
                {
                    slot.slotBackground.sprite = clawSprites[Mathf.Clamp(v, 0, 3)];
                    slot.slotBackground.color = !slot.isBlank ? artFixed : (i == selectedBlankSlotIndex ? artSelected : artBlank);
                }
                if (slot.slotText != null) slot.slotText.text = (slot.isBlank && v == 0) ? "?" : "";
                continue;
            }

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
                patternSlots[i].slotBackground.color = UseArt ? artGold : colGold; // [수정]
            if (patternSlots[i].slotText != null)
                patternSlots[i].slotText.color = new Color(0.2f, 0.15f, 0.05f, 1f);
        }

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FFD700>4x3 벽화의 좌우 대칭과 세로 순환이 완성되었습니다! (메인 반복 규칙 획득)</color>";

        if (successNotice != null) successNotice.SetActive(true);
        BastetSfx.RoundClear(); // [추가]
    }

    private IEnumerator FailRoutine()
    {
        isChecking = true;
        BastetSfx.Wrong(); // [추가]

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FF4444>가로 대칭 또는 세로 순환 규칙이 어긋난 빈칸이 있습니다!</color>";

        for (int i = 0; i < patternSlots.Length; i++)
        {
            if (patternSlots[i].isBlank && patternSlots[i].currentValue != patternSlots[i].targetValue)
            {
                if (patternSlots[i].slotBackground != null)
                    patternSlots[i].slotBackground.color = UseArt ? artWrong : colWrong; // [수정]
            }
        }

        yield return new WaitForSecondsRealtime(0.7f); // [수정]

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
        if (!Application.isPlaying) return; // [추가] 에디터 Reset 가드
        base.Reset();

        for (int i = 0; i < patternSlots.Length && i < fullGridPattern.Length; i++)
        {
            if (System.Array.IndexOf(blankIndices, i) >= 0)
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
            textStatusNotice.text = $"가로 대칭과 세로 순환 규칙을 분석하여 {blankIndices.Length}개의 빈칸(?)을 채우십시오.";
    }

    // [추가] 그림 모드 꾸미기: 후보 버튼 = 발톱 1~3줄 그림, 칸 색 = 흰색(그림 색 그대로), 글자 = 갈색
    private void ApplyArtLook()
    {
        if (!UseArt) return;
        for (int i = 0; i < candidateButtons.Length && i < 3; i++)
        {
            Button b = candidateButtons[i];
            if (b == null) continue;
            Image img = b.targetGraphic as Image ?? b.GetComponent<Image>();
            if (img != null) { img.sprite = clawSprites[i + 1]; img.type = Image.Type.Simple; img.color = Color.white; }
            foreach (TMP_Text t in b.GetComponentsInChildren<TMP_Text>(true)) t.text = ""; // '/' 글자 대신 그림
        }
        for (int i = 0; i < patternSlots.Length; i++)
        {
            PatternSlot slot = patternSlots[i];
            if (slot.slotBackground != null) slot.slotBackground.type = Image.Type.Simple;
            if (slot.slotText != null) slot.slotText.color = new Color(0.23f, 0.14f, 0.06f, 1f);
        }
        if (textStatusNotice != null) textStatusNotice.color = artInk;
    }

#if UNITY_EDITOR
    // [추가] 그림 4장이 비어 있으면 에디터에서 자동으로 찾아 넣음 (씬을 열거나 스크립트가 바뀔 때) -> Ctrl+S로 저장
    private void OnValidate()
    {
        if (UseArt) return;
        if (clawSprites == null || clawSprites.Length < 4) clawSprites = new Sprite[4];
        bool changed = false;
        for (int i = 0; i < 4; i++)
        {
            if (clawSprites[i] != null) continue;
            clawSprites[i] = FindSprite($"Bastet_UI_Claw_{i}"); // [수정] 폴더와 상관없이 이름으로 찾음
            changed |= clawSprites[i] != null;
        }
        if (!changed) return;
        Debug.Log($"[{name}] 발톱 자국 그림 4장 자동 연결 (Ctrl+S로 씬 저장)", this);
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) UnityEditor.EditorUtility.SetDirty(this); }; // 저장 대상으로 표시
    }

    private static Sprite FindSprite(string file)
    {
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets(file + " t:Texture2D"))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == file) return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }
#endif

    public void ClosePuzzle()
    {
        if (puzzleUIRoot != null)
            puzzleUIRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    // [추가] 창이 꺼지면 틀림 연출이 멈추므로 잠금도 풀어 둠
    private void OnDisable() => isChecking = false;
}
