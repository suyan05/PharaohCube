using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BastetMainPuzzle : PuzzleBase
{
    [System.Serializable]
    public struct TileData
    {
        public string direction;
        public string eye; 
        public string deco;

        public TileData(string d, string e, string dec)
        {
            direction = d;
            eye = e;
            deco = dec;
        }

        public string GetDisplayText()
        {
            return $"{direction}\n<size=12>{eye} {deco}</size>";
        }

        public bool Equals(TileData other)
        {
            return direction == other.direction && eye == other.eye && deco == other.deco;
        }
    }

    [System.Serializable]
    public class MatrixSlot
    {
        public Button slotButton;
        public TMP_Text slotText;
        public Image slotBackground;
        public bool isBlank;
        public TileData targetData;
        public TileData currentData;
        public TileData[] candidateList;
    }

    [Header("6x5 대형 매트릭스 슬롯 (총 30칸)")]
    [Tooltip("Hierarchy의 Matrix_Slots_Area를 여기에 넣고 컴포넌트 우클릭 메뉴로 일괄 연결할 수 있습니다.")]
    [SerializeField] private Transform matrixSlotsContainer;
    [SerializeField] private MatrixSlot[] matrixSlots = new MatrixSlot[30];

    [Header("하단 후보 카드 버튼 (3개)")]
    [SerializeField] private GameObject candidatePanel;
    [SerializeField] private TMP_Text candidateLabel;
    [SerializeField] private Button[] candidateButtons = new Button[3];
    [SerializeField] private TMP_Text[] candidateTexts = new TMP_Text[3];

    [Header("중앙 및 최종 보상 연출")]
    [SerializeField] private GameObject catStencilRewardPanel;
    [SerializeField] private GameObject worldExitDoor;

    [Header("UI 안내 및 제어")]
    [SerializeField] private GameObject puzzleUIRoot;
    [SerializeField] private TMP_Text textStatusNotice;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private Button btnSubmit;
    [SerializeField] private Button btnReset;
    [SerializeField] private Button btnClose;

    private int selectedBlankIndex = -1;
    private bool isChecking = false;

    private readonly Color colSlotFixed = new Color(0.22f, 0.17f, 0.28f, 1f);
    private readonly Color colSlotBlank = new Color(0.16f, 0.12f, 0.22f, 1f);
    private readonly Color colSlotSelected = new Color(0.7f, 0.4f, 0.95f, 1f);
    private readonly Color colWrong = new Color(0.9f, 0.25f, 0.25f, 1f);
    private readonly Color colGold = new Color(1f, 0.85f, 0.2f, 1f);

    private void Awake()
    {
        for (int i = 0; i < matrixSlots.Length; i++)
        {
            int index = i;
            if (matrixSlots[index] != null && matrixSlots[index].slotButton != null)
            {
                matrixSlots[index].slotButton.onClick.AddListener(() => OnSlotClicked(index));
            }
        }

        for (int i = 0; i < candidateButtons.Length; i++)
        {
            int cIndex = i;
            if (candidateButtons[cIndex] != null)
            {
                candidateButtons[cIndex].onClick.AddListener(() => OnCandidateChosen(cIndex));
            }
        }

        if (btnSubmit != null) btnSubmit.onClick.AddListener(Submit);
        if (btnReset != null) btnReset.onClick.AddListener(ResetBlanks);
        if (btnClose != null) btnClose.onClick.AddListener(ClosePuzzle);
    }

    public override void Open()
    {
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        base.Open();

        if (successNotice != null) successNotice.SetActive(false);
        if (catStencilRewardPanel != null) catStencilRewardPanel.SetActive(false);

        if (!IsCleared)
        {
            Reset();
        }
    }

    private void OnSlotClicked(int index)
    {
        if (IsCleared || isChecking) return;
        if (!matrixSlots[index].isBlank) return;

        selectedBlankIndex = index;
        UpdateSlotVisuals();
        ShowCandidatesForSlot(index);

        int row = index / 6 + 1;
        int col = index % 6 + 1;
        if (textStatusNotice != null)
            textStatusNotice.text = $"{row}행 {col}열 빈칸을 선택했습니다. 하단 후보 3개 중 알맞은 것을 고르십시오.";
    }

    private void ShowCandidatesForSlot(int slotIdx)
    {
        if (candidatePanel != null) candidatePanel.SetActive(true);

        var slot = matrixSlots[slotIdx];
        if (slot.candidateList != null && slot.candidateList.Length == 3)
        {
            for (int i = 0; i < 3; i++)
            {
                if (candidateTexts[i] != null)
                    candidateTexts[i].text = slot.candidateList[i].GetDisplayText();
            }
        }
    }

    private void OnCandidateChosen(int candidateIdx)
    {
        if (IsCleared || isChecking || selectedBlankIndex == -1) return;

        var slot = matrixSlots[selectedBlankIndex];
        if (slot.candidateList != null && candidateIdx < slot.candidateList.Length)
        {
            slot.currentData = slot.candidateList[candidateIdx];
            selectedBlankIndex = -1;

            if (candidatePanel != null) candidatePanel.SetActive(false);
            UpdateSlotVisuals();

            if (textStatusNotice != null)
                textStatusNotice.text = "빈칸에 후보를 배치했습니다. 다른 빈칸을 선택하거나 봉인을 해제하십시오.";
        }
    }

    private void UpdateSlotVisuals()
    {
        for (int i = 0; i < matrixSlots.Length; i++)
        {
            var slot = matrixSlots[i];
            if (slot == null) continue;

            if (!slot.isBlank)
            {
                if (slot.slotText != null) slot.slotText.text = slot.targetData.GetDisplayText();
                if (slot.slotBackground != null) slot.slotBackground.color = colSlotFixed;
            }
            else
            {
                bool isPlaced = !string.IsNullOrEmpty(slot.currentData.direction);
                if (slot.slotText != null)
                {
                    slot.slotText.text = isPlaced ? slot.currentData.GetDisplayText() : "<b><size=22>?</size></b>";
                }

                if (slot.slotBackground != null)
                {
                    slot.slotBackground.color = (i == selectedBlankIndex) ? colSlotSelected : colSlotBlank;
                }
            }
        }
    }

    public override void Submit()
    {
        if (IsCleared || isChecking) return;

        for (int i = 0; i < matrixSlots.Length; i++)
        {
            if (matrixSlots[i].isBlank && string.IsNullOrEmpty(matrixSlots[i].currentData.direction))
            {
                if (textStatusNotice != null)
                    textStatusNotice.text = "<color=#FFA726>8개의 빈칸(?)을 모두 채운 후 봉인을 해제하십시오.</color>";
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
        for (int i = 0; i < matrixSlots.Length; i++)
        {
            if (matrixSlots[i].isBlank)
            {
                if (!matrixSlots[i].currentData.Equals(matrixSlots[i].targetData))
                    return false;
            }
        }
        return true;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal();

        for (int i = 0; i < matrixSlots.Length; i++)
        {
            if (matrixSlots[i].slotBackground != null)
                matrixSlots[i].slotBackground.color = colGold;
            if (matrixSlots[i].slotText != null)
                matrixSlots[i].slotText.color = new Color(0.2f, 0.15f, 0.05f, 1f);
        }

        if (candidatePanel != null) candidatePanel.SetActive(false);

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FFD700>바스테트의 신성한 반복이 완성되었습니다! 고양이 형판을 획득합니다.</color>";

        if (successNotice != null) successNotice.SetActive(true);
        if (catStencilRewardPanel != null) catStencilRewardPanel.SetActive(true);

        if (worldExitDoor != null)
        {
            worldExitDoor.SetActive(false);
        }
    }

    private IEnumerator FailRoutine()
    {
        isChecking = true;

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FF4444>신성한 조화가 어긋났습니다! 틀린 빈칸이 붉게 반응합니다.</color>";

        for (int i = 0; i < matrixSlots.Length; i++)
        {
            if (matrixSlots[i].isBlank && !matrixSlots[i].currentData.Equals(matrixSlots[i].targetData))
            {
                if (matrixSlots[i].slotBackground != null)
                    matrixSlots[i].slotBackground.color = colWrong;
            }
        }

        yield return new WaitForSeconds(0.8f);

        UpdateSlotVisuals();
        isChecking = false;
    }

    private void ResetBlanks()
    {
        if (IsCleared || isChecking) return;

        for (int i = 0; i < matrixSlots.Length; i++)
        {
            if (matrixSlots[i].isBlank)
            {
                matrixSlots[i].currentData = new TileData("", "", "");
            }
        }

        selectedBlankIndex = -1;
        if (candidatePanel != null) candidatePanel.SetActive(false);
        UpdateSlotVisuals();

        if (textStatusNotice != null)
            textStatusNotice.text = "빈칸이 초기화되었습니다.";
    }

    public override void Reset()
    {
        base.Reset();

        TileData[,] fullGrid = new TileData[5, 6];

        string[] dirsLeft = { "▲", "▶", "▼", "▲", "▶" };
        string[] eyesLeft = { "[O]", "[-]", "[O]", "[-]", "[O]" };
        string[] decosLeft = { "/", "//", "///", "/", "//" };

        for (int r = 0; r < 5; r++)
        {
            fullGrid[r, 0] = new TileData(dirsLeft[r], eyesLeft[r], decosLeft[r]);
            fullGrid[r, 1] = new TileData(RotateDir(dirsLeft[r]), eyesLeft[(r + 1) % 5], NextDeco(decosLeft[r]));
            fullGrid[r, 2] = new TileData(RotateDir(RotateDir(dirsLeft[r])), eyesLeft[(r + 2) % 5], NextDeco(NextDeco(decosLeft[r])));

            fullGrid[r, 3] = MirrorTile(fullGrid[r, 2]);
            fullGrid[r, 4] = MirrorTile(fullGrid[r, 1]);
            fullGrid[r, 5] = MirrorTile(fullGrid[r, 0]);
        }

        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 6; c++)
            {
                int idx = r * 6 + c;
                if (idx < matrixSlots.Length && matrixSlots[idx] != null)
                {
                    matrixSlots[idx].isBlank = false;
                    matrixSlots[idx].targetData = fullGrid[r, c];
                    matrixSlots[idx].currentData = new TileData("", "", "");
                }
            }
        }

        int[] blankIndices = { 2, 5, 8, 13, 16, 21, 26, 28 };

        for (int i = 0; i < blankIndices.Length; i++)
        {
            int bIdx = blankIndices[i];
            if (bIdx >= matrixSlots.Length || matrixSlots[bIdx] == null) continue;

            matrixSlots[bIdx].isBlank = true;

            TileData correct = matrixSlots[bIdx].targetData;
            TileData fake1 = new TileData(MirrorDir(correct.direction), correct.eye, correct.deco);
            TileData fake2 = new TileData(correct.direction, (correct.eye == "[O]") ? "[-]" : "[O]", NextDeco(correct.deco));

            TileData[] candidates = new TileData[3];
            if (i % 3 == 0) { candidates[0] = correct; candidates[1] = fake1; candidates[2] = fake2; }
            else if (i % 3 == 1) { candidates[0] = fake1; candidates[1] = correct; candidates[2] = fake2; }
            else { candidates[0] = fake1; candidates[1] = fake2; candidates[2] = correct; }

            matrixSlots[bIdx].candidateList = candidates;
        }

        selectedBlankIndex = -1;
        if (candidatePanel != null) candidatePanel.SetActive(false);
        UpdateSlotVisuals();

        if (textStatusNotice != null)
            textStatusNotice.text = "행/열 순환과 좌우 거울 대칭을 분석하여 8개의 빈칸(?)을 채우십시오.";
    }

    private string RotateDir(string dir)
    {
        if (dir == "▲") return "▶";
        if (dir == "▶") return "▼";
        if (dir == "▼") return "◀";
        return "▲";
    }

    private string MirrorDir(string dir)
    {
        if (dir == "▶") return "◀";
        if (dir == "◀") return "▶";
        return dir;
    }

    private string NextDeco(string deco)
    {
        if (deco == "/") return "//";
        if (deco == "//") return "///";
        return "/";
    }

    private TileData MirrorTile(TileData orig)
    {
        return new TileData(MirrorDir(orig.direction), orig.eye, orig.deco);
    }

    public void ClosePuzzle()
    {
        if (puzzleUIRoot != null) puzzleUIRoot.SetActive(false);
        else gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    [ContextMenu(" 30개 매트릭스 슬롯 자동 연결")]
    public void AutoAssignMatrixSlots()
    {
        if (matrixSlotsContainer == null)
        {
            Debug.LogError("[오류] Matrix Slots Container 슬롯에 'Matrix_Slots_Area'를 먼저 드래그해 넣으세요!");
            return;
        }

        int count = matrixSlotsContainer.childCount;
        matrixSlots = new MatrixSlot[count];

        for (int i = 0; i < count; i++)
        {
            Transform child = matrixSlotsContainer.GetChild(i);
            matrixSlots[i] = new MatrixSlot
            {
                slotButton = child.GetComponent<Button>(),
                slotBackground = child.GetComponent<Image>(),
                slotText = child.GetComponentInChildren<TMP_Text>()
            };
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"<color=#00FF00>[성공] 총 {count}개의 슬롯이 자동으로 바인딩되었습니다!</color>");
    }
#endif
}