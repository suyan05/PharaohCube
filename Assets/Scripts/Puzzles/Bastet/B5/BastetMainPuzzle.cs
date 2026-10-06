using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BastetMainPuzzle : PuzzleBase
{
    [System.Serializable]
    public class SacredDial
    {
        public string dialName;
        public Button btnCycle;
        public TMP_Text textValue;
        public Image dialBackground;
        public string[] options;
        public int targetIndex;
        public int currentIndex = 0; 
    }

    [Header("4대 신성 다이얼 (1:발자국, 2:눈, 3:대칭, 4:발톱)")]
    [SerializeField] private SacredDial[] dials = new SacredDial[4];

    [Header("중앙 고양이 형판 봉인 구역")]
    [SerializeField] private Image centerBastetEmblem;
    [SerializeField] private GameObject catStencilRewardPanel;

    [Header("UI 안내 및 제어")]
    [SerializeField] private GameObject puzzleUIRoot;
    [SerializeField] private TMP_Text textStatusNotice;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private Button btnUnlockSeal;
    [SerializeField] private Button btnClose;

    [Header("월드 출구 문 (클리어 시 개방)")]
    [SerializeField] private GameObject worldExitDoor;

    private bool isChecking = false;

    private readonly Color colDialNormal = new Color(0.24f, 0.18f, 0.32f, 1f);
    private readonly Color colDialWrong = new Color(0.9f, 0.25f, 0.25f, 1f);
    private readonly Color colGold = new Color(1f, 0.85f, 0.2f, 1f);

    private void Awake()
    {
        for (int i = 0; i < dials.Length; i++)
        {
            int index = i;
            if (dials[index].btnCycle != null)
            {
                dials[index].btnCycle.onClick.AddListener(() => OnDialClicked(index));
            }
        }

        if (btnUnlockSeal != null) btnUnlockSeal.onClick.AddListener(Submit);
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
        if (catStencilRewardPanel != null) catStencilRewardPanel.SetActive(false);

        if (!IsCleared)
        {
            Reset();
        }
    }

    private void OnDialClicked(int dialIndex)
    {
        if (IsCleared || isChecking) return;

        var dial = dials[dialIndex];
        if (dial.options != null && dial.options.Length > 0)
        {
            dial.currentIndex = (dial.currentIndex + 1) % dial.options.Length;
            UpdateDialVisual(dialIndex);
        }
    }

    private void UpdateDialVisual(int dialIndex)
    {
        var dial = dials[dialIndex];
        if (dial.textValue != null && dial.options != null && dial.options.Length > dial.currentIndex)
        {
            dial.textValue.text = dial.options[dial.currentIndex];
        }

        if (dial.dialBackground != null)
        {
            dial.dialBackground.color = colDialNormal;
        }
    }

    public override void Submit()
    {
        if (IsCleared || isChecking) return;

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
        for (int i = 0; i < dials.Length; i++)
        {
            if (dials[i].currentIndex != dials[i].targetIndex)
                return false;
        }
        return true;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal();

        for (int i = 0; i < dials.Length; i++)
        {
            if (dials[i].dialBackground != null)
                dials[i].dialBackground.color = colGold;
            if (dials[i].textValue != null)
                dials[i].textValue.color = new Color(0.2f, 0.15f, 0.05f, 1f);
        }

        if (centerBastetEmblem != null)
            centerBastetEmblem.color = colGold;

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FFD700>바스테트의 봉인이 풀렸습니다! 고양이 형판을 획득했습니다.</color>";

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
            textStatusNotice.text = "<color=#FF4444>신성한 조화가 이루어지지 않았습니다. 맞지 않는 다이얼이 공명합니다.</color>";

        for (int i = 0; i < dials.Length; i++)
        {
            if (dials[i].currentIndex != dials[i].targetIndex && dials[i].dialBackground != null)
            {
                dials[i].dialBackground.color = colDialWrong;
            }
        }

        yield return new WaitForSeconds(0.7f);

        for (int i = 0; i < dials.Length; i++)
        {
            UpdateDialVisual(i);
        }

        if (textStatusNotice != null)
            textStatusNotice.text = "네 제단에서 터득한 법칙을 결합하여 봉인을 해제하십시오.";

        isChecking = false;
    }

    public override void Reset()
    {
        base.Reset();

        dials[0].options = new string[] { "북 (North)", "동 (East)", "남 (South)", "서 (West)" };
        dials[0].targetIndex = 1;
        dials[0].currentIndex = 0;

        dials[1].options = new string[] { "상 (▲)", "우 (▶)", "하 (▼)", "좌 (◀)" };
        dials[1].targetIndex = 0;
        dials[1].currentIndex = 2;

        dials[2].options = new string[] { "평행 (Parallel)", "수렴 (Symmetry)", "발산 (Diverge)", "직각 (Right)" };
        dials[2].targetIndex = 1;
        dials[2].currentIndex = 0;

        dials[3].options = new string[] { "/ (1줄)", "// (2줄)", "/// (3줄)" };
        dials[3].targetIndex = 2;
        dials[3].currentIndex = 0;

        for (int i = 0; i < dials.Length; i++)
        {
            UpdateDialVisual(i);
        }

        if (centerBastetEmblem != null)
            centerBastetEmblem.color = new Color(0.4f, 0.35f, 0.5f, 1f);

        if (textStatusNotice != null)
            textStatusNotice.text = "네 제단에서 터득한 법칙을 결합하여 봉인을 해제하십시오.";
    }

    public void ClosePuzzle()
    {
        if (puzzleUIRoot != null)
            puzzleUIRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}