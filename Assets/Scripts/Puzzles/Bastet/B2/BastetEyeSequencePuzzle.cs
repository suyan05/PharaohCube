using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BastetEyeSequencePuzzle : PuzzleBase
{
    [Header("고양이 눈 버튼 (4개: 상/우/하/좌)")]
    [SerializeField] private Button[] eyeButtons = new Button[4];
    [SerializeField] private Image[] eyeGlowImages = new Image[4];

    [Header("중앙 개안 문양 및 입력 진행 인디케이터")]
    [SerializeField] private Image centerEyeIcon;
    [SerializeField] private Image[] stepDots = new Image[5];

    [Header("UI 안내 및 제어")]
    [SerializeField] private GameObject puzzleUIRoot;
    [SerializeField] private TMP_Text textStatusNotice;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private Button btnReplaySequence;
    [SerializeField] private Button btnClose;

    private readonly int[] targetSequence = { 0, 2, 1, 3, 0 };
    private int currentStep = 0;

    private bool isShowingSequence = false;
    private Coroutine sequenceCoroutine;

    // 테마 색상
    private readonly Color colEyeDefault = new Color(0.2f, 0.16f, 0.25f, 1f);
    private readonly Color colEyeFlash = new Color(0.4f, 0.9f, 1f, 1f);
    private readonly Color colEyeWrong = new Color(0.95f, 0.2f, 0.2f, 1f);
    private readonly Color colEyeGold = new Color(1f, 0.85f, 0.2f, 1f);
    private readonly Color colDotOff = new Color(0.3f, 0.25f, 0.35f, 0.5f);
    private readonly Color colDotOn = new Color(0.4f, 0.9f, 1f, 1f);

    private void Awake()
    {
        for (int i = 0; i < eyeButtons.Length; i++)
        {
            int index = i;
            if (eyeButtons[index] != null)
                eyeButtons[index].onClick.AddListener(() => OnEyeButtonClicked(index));
        }

        if (btnReplaySequence != null) btnReplaySequence.onClick.AddListener(PlaySequence);
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
            PlaySequence();
        }
    }

    public void PlaySequence()
    {
        if (IsCleared || isShowingSequence) return;

        if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
        sequenceCoroutine = StartCoroutine(ShowSequenceRoutine());
    }

    private IEnumerator ShowSequenceRoutine()
    {
        isShowingSequence = true;
        currentStep = 0;
        ResetVisuals();

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#80D8FF>고양이 눈이 점멸하는 순서를 기억하십시오...</color>";

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < targetSequence.Length; i++)
        {
            int eyeIdx = targetSequence[i];

            if (eyeGlowImages[eyeIdx] != null)
                eyeGlowImages[eyeIdx].color = colEyeFlash;

            yield return new WaitForSeconds(0.45f);

            if (eyeGlowImages[eyeIdx] != null)
                eyeGlowImages[eyeIdx].color = colEyeDefault;

            yield return new WaitForSeconds(0.15f);
        }

        ResetVisuals();

        if (textStatusNotice != null)
            textStatusNotice.text = "기억한 순서대로 눈을 누르십시오. (0 / 5)";

        isShowingSequence = false;
    }

    private void OnEyeButtonClicked(int eyeIndex)
    {
        if (IsCleared || isShowingSequence) return;

        StartCoroutine(FlashEyeButton(eyeIndex));

        if (eyeIndex == targetSequence[currentStep])
        {
            if (currentStep < stepDots.Length && stepDots[currentStep] != null)
                stepDots[currentStep].color = colDotOn;

            currentStep++;

            if (textStatusNotice != null)
                textStatusNotice.text = $"순서 입력 중: ({currentStep} / {targetSequence.Length})";

            if (currentStep >= targetSequence.Length)
            {
                Submit();
            }
        }
        else
        {
            StartCoroutine(FailRoutine(eyeIndex));
        }
    }

    private IEnumerator FlashEyeButton(int index)
    {
        if (eyeGlowImages[index] != null)
            eyeGlowImages[index].color = colEyeFlash;

        yield return new WaitForSeconds(0.2f);

        if (!IsCleared && eyeGlowImages[index] != null)
            eyeGlowImages[index].color = colEyeDefault;
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
        return currentStep >= targetSequence.Length;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal();

        for (int i = 0; i < eyeGlowImages.Length; i++)
        {
            if (eyeGlowImages[i] != null)
                eyeGlowImages[i].color = colEyeGold;
        }

        if (centerEyeIcon != null) centerEyeIcon.color = colEyeGold;

        for (int i = 0; i < stepDots.Length; i++)
        {
            if (stepDots[i] != null) stepDots[i].color = colEyeGold;
        }

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FFD700>고양이의 눈이 진실을 응시합니다! (두 번째 패턴 획득)</color>";

        if (successNotice != null) successNotice.SetActive(true);
    }

    private IEnumerator FailRoutine(int wrongIndex)
    {
        isShowingSequence = true;

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FF4444>순서가 어긋났습니다! 시선이 흩어집니다.</color>";

        if (eyeGlowImages[wrongIndex] != null)
            eyeGlowImages[wrongIndex].color = colEyeWrong;

        yield return new WaitForSeconds(0.7f);

        OnFailInternal();
        PlaySequence();
    }

    public override void Reset()
    {
        base.Reset();
        currentStep = 0;
        isShowingSequence = false;
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        for (int i = 0; i < eyeGlowImages.Length; i++)
        {
            if (eyeGlowImages[i] != null)
                eyeGlowImages[i].color = colEyeDefault;
        }

        if (centerEyeIcon != null)
            centerEyeIcon.color = colDotOff;

        for (int i = 0; i < stepDots.Length; i++)
        {
            if (stepDots[i] != null)
                stepDots[i].color = colDotOff;
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