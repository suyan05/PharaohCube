using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BastetFootprintPuzzle : PuzzleBase
{
    [Header("4x4 타일 그리드 버튼 (16개)")]
    [SerializeField] private Button[] gridTiles = new Button[16];
    [SerializeField] private Image[] tileGlowImages = new Image[16];

    [Header("UI 안내 및 제어")]
    [SerializeField] private GameObject puzzleUIRoot;
    [SerializeField] private TMP_Text textStatusNotice;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private Button btnReplayPath;
    [SerializeField] private Button btnClose;

    private readonly int[] correctPath = { 1, 5, 6, 10, 11 };
    private int currentStep = 0;

    private bool isPlayingSequence = false;
    private Coroutine sequenceCoroutine;

    private readonly Color colDefault = new Color(0.18f, 0.16f, 0.22f, 1f);
    private readonly Color colPurple = new Color(0.72f, 0.38f, 0.95f, 1f);
    private readonly Color colWrong = new Color(0.9f, 0.2f, 0.2f, 1f);
    private readonly Color colGold = new Color(1f, 0.85f, 0.2f, 1f);

    private void Awake()
    {
        for (int i = 0; i < gridTiles.Length; i++)
        {
            int index = i;
            if (gridTiles[index] != null)
                gridTiles[index].onClick.AddListener(() => OnTileClicked(index));
        }

        if (btnReplayPath != null) btnReplayPath.onClick.AddListener(PlayPathSequence);
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
            PlayPathSequence();
        }
    }

    public void PlayPathSequence()
    {
        if (IsCleared || isPlayingSequence) return;

        if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
        sequenceCoroutine = StartCoroutine(ShowSequenceRoutine());
    }

    private IEnumerator ShowSequenceRoutine()
    {
        isPlayingSequence = true;
        currentStep = 0;
        ResetTileColors();

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#BB86FC>고양이의 발자국을 관찰하십시오...</color>";

        yield return new WaitForSeconds(0.4f);

        for (int i = 0; i < correctPath.Length; i++)
        {
            int tileIndex = correctPath[i];
            if (tileGlowImages[tileIndex] != null)
                tileGlowImages[tileIndex].color = colPurple;

            yield return new WaitForSeconds(0.55f);

            if (tileGlowImages[tileIndex] != null)
                tileGlowImages[tileIndex].color = colDefault;

            yield return new WaitForSeconds(0.1f);
        }

        ResetTileColors();

        if (textStatusNotice != null)
            textStatusNotice.text = "기억한 발자국 순서대로 타일을 누르십시오. (0 / 5)";

        isPlayingSequence = false;
    }

    private void OnTileClicked(int clickedIndex)
    {
        if (IsCleared || isPlayingSequence) return;

        if (clickedIndex == correctPath[currentStep])
        {
            if (tileGlowImages[clickedIndex] != null)
                tileGlowImages[clickedIndex].color = colPurple;

            currentStep++;

            if (textStatusNotice != null)
                textStatusNotice.text = $"발자국 진행: ({currentStep} / {correctPath.Length})";

            if (currentStep >= correctPath.Length)
            {
                Submit();
            }
        }
        else
        {
            StartCoroutine(FailRoutine(clickedIndex));
        }
    }

    public override void Submit()
    {
        if (Validate())
        {
            OnSuccessInternal();
        }
        else
        {
            OnFailInternal();
        }
    }

    protected override bool Validate()
    {
        return currentStep >= correctPath.Length;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal(); 
        for (int i = 0; i < correctPath.Length; i++)
        {
            int tileIdx = correctPath[i];
            if (tileGlowImages[tileIdx] != null)
                tileGlowImages[tileIdx].color = colGold;
        }

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FFD700>발자국 경로 복원 완료!</color>";

        if (successNotice != null) successNotice.SetActive(true);
    }

    private IEnumerator FailRoutine(int wrongIndex)
    {
        isPlayingSequence = true;

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FF4444>발자국이 흐트러졌습니다!</color>";

        if (tileGlowImages[wrongIndex] != null)
            tileGlowImages[wrongIndex].color = colWrong;

        yield return new WaitForSeconds(0.7f);

        OnFailInternal();
        PlayPathSequence();
    }

    public override void Reset()
    {
        base.Reset();
        currentStep = 0;
        isPlayingSequence = false;
        ResetTileColors();
    }

    private void ResetTileColors()
    {
        for (int i = 0; i < tileGlowImages.Length; i++)
        {
            if (tileGlowImages[i] != null)
                tileGlowImages[i].color = colDefault;
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