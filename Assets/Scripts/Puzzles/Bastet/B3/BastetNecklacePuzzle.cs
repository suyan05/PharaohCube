using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BastetNecklacePuzzle : PuzzleBase
{
    [System.Serializable]
    public class NecklacePiece
    {
        public string pieceName = "문양 조각";
        public Button button;
        public RectTransform transform;
        public Image pieceImage;
        [Tooltip("현재 각도 (0: 0도 ▲, 1: 90도 ▶, 2: 180도 ▼, 3: 270도 ◀)")]
        public int currentRotStep = 0;
    }

    [Header("8대 목걸이 문양 조각 (0~3: 좌측 1~4번, 4~7: 우측 5~8번)")]
    [SerializeField] private NecklacePiece[] pieces = new NecklacePiece[8];

    [Header("중앙 대칭선 및 눈 보석")]
    [SerializeField] private Image centerLine;
    [SerializeField] private Image centerEyeGem;

    [Header("UI 안내 및 제어")]
    [SerializeField] private GameObject puzzleUIRoot;
    [SerializeField] private TMP_Text textStatusNotice;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private Button btnCheckSymmetry;
    [SerializeField] private Button btnClose;

    private readonly int[][] chainMatrix = new int[][]
    {
        new int[] { 0, 1, 3 },          // 1번: 1, 2, 4
        new int[] { 0, 1, 4 },          // 2번: 1, 2, 5
        new int[] { 2, 3, 5 },          // 3번: 3, 4, 6
        new int[] { 2, 3, 4, 6 },       // 4번: 3, 4, 5, 7
        new int[] { 3, 4, 7 },          // 5번: 4, 5, 8
        new int[] { 2, 5, 6 },          // 6번: 3, 6, 7
        new int[] { 3, 5, 6, 7 },       // 7번: 4, 6, 7, 8
        new int[] { 4, 6, 7 }           // 8번: 5, 7, 8
    };

    private readonly int[,] symmetryPairs = new int[,]
    {
        { 0, 4 },
        { 1, 5 },
        { 2, 6 },
        { 3, 7 }
    };

    private bool isChecking = false;

    private readonly Color colNormal = new Color(0.22f, 0.17f, 0.28f, 1f);
    private readonly Color colWrong = new Color(0.9f, 0.22f, 0.22f, 1f);
    private readonly Color colGold = new Color(1f, 0.85f, 0.2f, 1f);

    private void Awake()
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            int index = i;
            if (pieces[index].button != null)
            {
                pieces[index].button.onClick.AddListener(() => OnPieceClicked(index));
            }
        }

        if (btnCheckSymmetry != null) btnCheckSymmetry.onClick.AddListener(Submit);
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

    private void OnPieceClicked(int clickedIdx)
    {
        if (IsCleared || isChecking) return;

        int[] linkedIndices = chainMatrix[clickedIdx];
        for (int i = 0; i < linkedIndices.Length; i++)
        {
            int targetIdx = linkedIndices[i];
            pieces[targetIdx].currentRotStep = (pieces[targetIdx].currentRotStep + 1) % 4;
            UpdatePieceVisual(pieces[targetIdx]);
        }
    }

    private void UpdatePieceVisual(NecklacePiece piece)
    {
        if (piece.transform != null)
        {
            piece.transform.localRotation = Quaternion.Euler(0, 0, -piece.currentRotStep * 90f);
        }

        if (piece.pieceImage != null)
        {
            piece.pieceImage.color = colNormal;
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
        for (int p = 0; p < 4; p++)
        {
            int leftIdx = symmetryPairs[p, 0];
            int rightIdx = symmetryPairs[p, 1];

            int leftStep = pieces[leftIdx].currentRotStep;
            int rightStep = pieces[rightIdx].currentRotStep;

            int requiredRightStep = (4 - leftStep) % 4;

            if (rightStep != requiredRightStep)
            {
                return false;
            }
        }
        return true;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal();
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].pieceImage != null)
                pieces[i].pieceImage.color = colGold;
        }

        if (centerEyeGem != null) centerEyeGem.color = colGold;
        if (centerLine != null) centerLine.color = colGold;

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FFD700>목걸이의 8개 문양이 완벽한 대칭을 이룹니다! (세 번째 패턴 기록)</color>";

        if (successNotice != null) successNotice.SetActive(true);
    }

    private IEnumerator FailRoutine()
    {
        isChecking = true;

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FF4444>좌우 대칭이 맞지 않는 조각이 붉게 반응합니다!</color>";

        for (int p = 0; p < 4; p++)
        {
            int leftIdx = symmetryPairs[p, 0];
            int rightIdx = symmetryPairs[p, 1];

            int leftStep = pieces[leftIdx].currentRotStep;
            int rightStep = pieces[rightIdx].currentRotStep;
            int requiredRightStep = (4 - leftStep) % 4;

            if (rightStep != requiredRightStep)
            {
                if (pieces[leftIdx].pieceImage != null) pieces[leftIdx].pieceImage.color = colWrong;
                if (pieces[rightIdx].pieceImage != null) pieces[rightIdx].pieceImage.color = colWrong;
            }
        }

        yield return new WaitForSeconds(0.7f);

        for (int i = 0; i < pieces.Length; i++)
        {
            UpdatePieceVisual(pieces[i]);
        }

        if (textStatusNotice != null)
            textStatusNotice.text = "조각을 회전시켜 중앙선을 기준으로 좌우 대칭을 만드십시오.";

        isChecking = false;
    }

    public override void Reset()
    {
        base.Reset();

        pieces[0].currentRotStep = 0; 
        pieces[1].currentRotStep = 1; 
        pieces[2].currentRotStep = 2; 
        pieces[3].currentRotStep = 3;

        pieces[4].currentRotStep = 0;
        pieces[5].currentRotStep = 3;
        pieces[6].currentRotStep = 2;
        pieces[7].currentRotStep = 1;

        int[] scrambleSequence = { 0, 3, 6, 1, 4, 7, 2 };
        for (int s = 0; s < scrambleSequence.Length; s++)
        {
            int btn = scrambleSequence[s];
            int[] targets = chainMatrix[btn];
            for (int t = 0; t < targets.Length; t++)
            {
                pieces[targets[t]].currentRotStep = (pieces[targets[t]].currentRotStep + 1) % 4;
            }
        }

        if (Validate())
        {
            int[] targets = chainMatrix[0];
            for (int t = 0; t < targets.Length; t++)
                pieces[targets[t]].currentRotStep = (pieces[targets[t]].currentRotStep + 1) % 4;
        }

        for (int i = 0; i < pieces.Length; i++)
        {
            UpdatePieceVisual(pieces[i]);
        }

        if (centerEyeGem != null) centerEyeGem.color = new Color(0.4f, 0.35f, 0.5f, 1f);
        if (centerLine != null) centerLine.color = new Color(0.5f, 0.4f, 0.6f, 0.4f);

        if (textStatusNotice != null)
            textStatusNotice.text = "조각을 회전시켜 중앙선을 기준으로 좌우 대칭을 만드십시오.";
    }

    public void ClosePuzzle()
    {
        if (puzzleUIRoot != null)
            puzzleUIRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}