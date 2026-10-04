using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BastetNecklacePuzzle : PuzzleBase
{
    [System.Serializable]
    public class NecklacePiece
    {
        public string pieceName = "조각";
        public Button button;
        public RectTransform transform;
        public Image pieceImage;
        [Tooltip("현재 각도 단계 (0: 0도, 1: 90도, 2: 180도, 3: 270도)")]
        public int currentRotStep = 0;
        [Tooltip("목표 정답 단계")]
        public int targetRotStep = 0;
    }

    [Header("4개 조각 (0: 1번 좌상, 1: 2번 좌하, 2: 3번 우상, 3: 4번 우하)")]
    [SerializeField] private NecklacePiece[] pieces = new NecklacePiece[4];

    [Header("중앙 대칭선 및 눈 보석")]
    [SerializeField] private Image centerLine;
    [SerializeField] private Image centerEyeGem;

    [Header("UI 안내 및 제어")]
    [SerializeField] private GameObject puzzleUIRoot;
    [SerializeField] private TMP_Text textStatusNotice;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private Button btnCheckSymmetry;
    [SerializeField] private Button btnClose;

    private bool isCleared = false;
    private bool isChecking = false;

    private readonly Color colNormal = new Color(0.2f, 0.16f, 0.25f, 1f);
    private readonly Color colWrong = new Color(0.9f, 0.25f, 0.25f, 1f); 
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

    private void OnPieceClicked(int index)
    {
        if (IsCleared || isChecking) return;

        switch (index)
        {
            case 0:
                RotateStep(0);
                RotateStep(1);
                break;
            case 1: 
                RotateStep(1);
                RotateStep(3);
                break;
            case 2: 
                RotateStep(2);
                RotateStep(0);
                break;
            case 3:
                RotateStep(3);
                RotateStep(2);
                break;
        }
    }

    private void RotateStep(int pieceIdx)
    {
        if (pieceIdx < 0 || pieceIdx >= pieces.Length) return;

        pieces[pieceIdx].currentRotStep = (pieces[pieceIdx].currentRotStep + 1) % 4;
        UpdatePieceTransform(pieces[pieceIdx]);
    }

    private void UpdatePieceTransform(NecklacePiece piece)
    {
        if (piece.transform != null)
        {
            piece.transform.localRotation = Quaternion.Euler(0, 0, -piece.currentRotStep * 90f);
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
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].currentRotStep != pieces[i].targetRotStep)
                return false;
        }
        return true;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal();
        isCleared = true;

        if (centerEyeGem != null) centerEyeGem.color = colGold;
        if (centerLine != null) centerLine.color = colGold;

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].pieceImage != null)
                pieces[i].pieceImage.color = colGold;
        }

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FFD700>목걸이의 문양이 완벽한 대칭을 이룹니다! (대칭 규칙 획득)</color>";

        if (successNotice != null) successNotice.SetActive(true);
    }

    private IEnumerator FailRoutine()
    {
        isChecking = true;

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FF4444>대칭이 맞지 않습니다! 각도를 다시 조정하십시오.</color>";

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].currentRotStep != pieces[i].targetRotStep && pieces[i].pieceImage != null)
            {
                pieces[i].pieceImage.color = colWrong;
            }
        }

        yield return new WaitForSeconds(0.7f);

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].pieceImage != null)
                pieces[i].pieceImage.color = colNormal;
        }

        if (textStatusNotice != null)
            textStatusNotice.text = "조각을 회전시켜 중앙 세로선을 기준으로 좌우 대칭을 만드십시오.";

        isChecking = false;
    }

    public override void Reset()
    {
        base.Reset();

        pieces[0].targetRotStep = 1; 
        pieces[1].targetRotStep = 2;
        pieces[2].targetRotStep = 3; 
        pieces[3].targetRotStep = 2; 

        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i].currentRotStep = pieces[i].targetRotStep;
        }

        SimulateScramble(0);
        SimulateScramble(3);
        SimulateScramble(1);
        SimulateScramble(2);
        SimulateScramble(0);

        if (Validate()) SimulateScramble(0);

        for (int i = 0; i < pieces.Length; i++)
        {
            UpdatePieceTransform(pieces[i]);
            if (pieces[i].pieceImage != null)
                pieces[i].pieceImage.color = colNormal;
        }

        if (centerEyeGem != null) centerEyeGem.color = colNormal;
        if (centerLine != null) centerLine.color = new Color(0.5f, 0.4f, 0.6f, 0.4f);

        if (textStatusNotice != null)
            textStatusNotice.text = "조각을 회전시켜 중앙 세로선을 기준으로 좌우 대칭을 만드십시오.";
    }

    private void SimulateScramble(int btnIdx)
    {
        switch (btnIdx)
        {
            case 0: pieces[0].currentRotStep = (pieces[0].currentRotStep + 1) % 4; pieces[1].currentRotStep = (pieces[1].currentRotStep + 1) % 4; break;
            case 1: pieces[1].currentRotStep = (pieces[1].currentRotStep + 1) % 4; pieces[3].currentRotStep = (pieces[3].currentRotStep + 1) % 4; break;
            case 2: pieces[2].currentRotStep = (pieces[2].currentRotStep + 1) % 4; pieces[0].currentRotStep = (pieces[0].currentRotStep + 1) % 4; break;
            case 3: pieces[3].currentRotStep = (pieces[3].currentRotStep + 1) % 4; pieces[2].currentRotStep = (pieces[2].currentRotStep + 1) % 4; break;
        }
    }

    public void ClosePuzzle()
    {
        if (puzzleUIRoot != null)
            puzzleUIRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}