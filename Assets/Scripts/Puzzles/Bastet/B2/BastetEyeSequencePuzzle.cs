using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BastetEyeSequencePuzzle : PuzzleBase
{
    [Header("고양이 눈 버튼 (총 6개)")]
    [SerializeField] private Button[] eyeButtons = new Button[6];
    [SerializeField] private Image[] eyeGlowImages = new Image[6];

    [Header("중앙 되감기 및 역순 연출 UI")]
    [SerializeField] private Image centerEmblem;
    [SerializeField] private TMP_Text textDirectionArrow;
    [SerializeField] private Image[] pawRewindIcons = new Image[3];
    [SerializeField] private Image[] stepDots = new Image[4];

    [Header("UI 안내 및 제어")]
    [SerializeField] private GameObject puzzleUIRoot;
    [SerializeField] private TMP_Text textStatusNotice;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private Button btnReplaySequence;
    [SerializeField] private Button btnClose;

    // [추가] 한 번에 같이 켜지는 눈 묶음 (Inspector에서 편집하려고 클래스로 감쌈)
    [System.Serializable]
    public class EyeGroup
    {
        [Tooltip("같이 빛나는 눈 번호 (0~5). 2개 이상이면 동시 점멸")]
        public int[] eyes = { 0 };
        public EyeGroup() { }
        public EyeGroup(params int[] e) { eyes = e; }
    }

    // [수정] 난이도를 Inspector에서 조절 (기본값은 기존과 같음)
    [Header("[추가] 난이도")]
    [Tooltip("눈이 빛나는 순서 (위에서부터). 정답은 이 순서의 역순. 단계 수가 많을수록, 동시 점멸이 많을수록 어려움")]
    [SerializeField] private EyeGroup[] forwardSteps =
    {
        new EyeGroup(0), new EyeGroup(2, 3), new EyeGroup(5), new EyeGroup(1, 4)
    };
    [Tooltip("눈 한 묶음이 켜져 있는 시간 (짧을수록 어려움)")]
    [Range(0.15f, 1.2f)] [SerializeField] private float showOnTime = 0.45f;
    [Tooltip("묶음 사이 꺼져 있는 시간")]
    [Range(0.05f, 0.8f)] [SerializeField] private float showGapTime = 0.18f;
    [Tooltip("켜면 보여 준 뒤 거꾸로 빠르게 한 번 더 보여 줌 (끄면 더 어려움)")]
    [SerializeField] private bool playRewindPreview = true;

    private int[][] forwardSequence;  // forwardSteps에서 만듦
    private int[][] reverseSequence;  // forwardSequence의 역순 (정답)

    private int currentInputStep = 0;
    private HashSet<int> pendingTargetEyes = new HashSet<int>();

    private bool isShowingSequence = false;
    private Coroutine sequenceCoroutine;

    private readonly Color colEyeDefault = new Color(0.2f, 0.16f, 0.26f, 1f);
    private readonly Color colEyeCyan = new Color(0.35f, 0.9f, 1f, 1f);
    private readonly Color colEyeRewind = new Color(1f, 0.45f, 0.75f, 1f);
    private readonly Color colEyeWrong = new Color(0.95f, 0.2f, 0.2f, 1f);
    private readonly Color colEyeGold = new Color(1f, 0.85f, 0.2f, 1f);
    private readonly Color colGold = new Color(1f, 0.85f, 0.2f, 1f);
    private readonly Color colDotOff = new Color(0.3f, 0.25f, 0.35f, 0.4f);
    private readonly Color colDotOn = new Color(0.35f, 0.9f, 1f, 1f);

    private void Awake()
    {
        BuildSequences(); // [추가]

        for (int i = 0; i < eyeButtons.Length; i++)
        {
            int index = i;
            if (eyeButtons[index] != null)
                eyeButtons[index].onClick.AddListener(() => OnEyeButtonClicked(index));
        }

        if (btnReplaySequence != null) btnReplaySequence.onClick.AddListener(PlaySequence);
        if (btnClose != null) btnClose.onClick.AddListener(ClosePuzzle);
    }

    // [추가] Inspector 순서 -> 정방향 / 역순 배열 (Awake에서 1번만)
    private void BuildSequences()
    {
        int n = forwardSteps != null ? forwardSteps.Length : 0;
        forwardSequence = new int[n][];
        reverseSequence = new int[n][];
        for (int s = 0; s < n; s++)
        {
            forwardSequence[s] = forwardSteps[s] != null && forwardSteps[s].eyes != null ? forwardSteps[s].eyes : new int[0];
            reverseSequence[n - 1 - s] = forwardSequence[s];
        }
        if (n == 0) Debug.LogError($"[{name}] Forward Steps가 비어 있음", this);
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
        currentInputStep = 0;
        pendingTargetEyes.Clear();
        ResetVisuals();

        if (textDirectionArrow != null) textDirectionArrow.text = "→";
        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#80D8FF>고양이 눈이 점멸하는 순서를 관찰하십시오... (동시 점멸 포함)</color>";

        yield return new WaitForSeconds(0.5f);

        for (int s = 0; s < forwardSequence.Length; s++)
        {
            int[] eyes = forwardSequence[s];
            for (int i = 0; i < eyes.Length; i++)
            {
                if (eyeGlowImages[eyes[i]] != null)
                    eyeGlowImages[eyes[i]].color = colEyeCyan;
            }

            yield return new WaitForSeconds(showOnTime);

            for (int i = 0; i < eyes.Length; i++)
            {
                if (eyeGlowImages[eyes[i]] != null)
                    eyeGlowImages[eyes[i]].color = colEyeDefault;
            }

            yield return new WaitForSeconds(showGapTime);
        }

        yield return new WaitForSeconds(0.3f);

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FF80AB>시선의 잔상이 거꾸로 되감깁니다!</color>";

        for (int p = pawRewindIcons.Length - 1; p >= 0; p--)
        {
            if (pawRewindIcons[p] != null)
                pawRewindIcons[p].color = new Color(1f, 1f, 1f, 0.1f);
            yield return new WaitForSeconds(0.2f);
        }

        if (textDirectionArrow != null)
        {
            textDirectionArrow.text = "←";
            textDirectionArrow.color = colEyeRewind;
        }

        for (int s = playRewindPreview ? forwardSequence.Length - 1 : -1; s >= 0; s--) // [수정] 미리보기 끄기 가능
        {
            int[] eyes = forwardSequence[s];
            for (int i = 0; i < eyes.Length; i++)
            {
                if (eyeGlowImages[eyes[i]] != null)
                    eyeGlowImages[eyes[i]].color = colEyeRewind;
            }

            yield return new WaitForSeconds(0.18f);

            for (int i = 0; i < eyes.Length; i++)
            {
                if (eyeGlowImages[eyes[i]] != null)
                    eyeGlowImages[eyes[i]].color = colEyeDefault;
            }

            yield return new WaitForSeconds(0.08f);
        }

        LoadCurrentStepPendingEyes();

        if (textStatusNotice != null)
            textStatusNotice.text = $"<b><color=#FF80AB>[◀ 역순 입력]</color></b> 마지막에 켜진 눈부터 거꾸로 누르십시오. (0 / {reverseSequence.Length})";

        isShowingSequence = false;
    }

    private void LoadCurrentStepPendingEyes()
    {
        pendingTargetEyes.Clear();
        if (currentInputStep < reverseSequence.Length)
        {
            int[] targetEyes = reverseSequence[currentInputStep];
            for (int i = 0; i < targetEyes.Length; i++)
            {
                pendingTargetEyes.Add(targetEyes[i]);
            }
        }
    }

    private void OnEyeButtonClicked(int eyeIndex)
    {
        if (IsCleared || isShowingSequence) return;

        if (pendingTargetEyes.Contains(eyeIndex))
        {
            if (eyeGlowImages[eyeIndex] != null)
                eyeGlowImages[eyeIndex].color = colEyeCyan;

            pendingTargetEyes.Remove(eyeIndex);

            if (pendingTargetEyes.Count == 0)
            {
                if (currentInputStep < stepDots.Length && stepDots[currentInputStep] != null)
                    stepDots[currentInputStep].color = colDotOn;

                currentInputStep++;

                StartCoroutine(ClearStepEyes(reverseSequence[currentInputStep - 1]));

                if (currentInputStep >= reverseSequence.Length)
                {
                    Submit();
                }
                else
                {
                    LoadCurrentStepPendingEyes();
                    if (textStatusNotice != null)
                        textStatusNotice.text = $"<b><color=#FF80AB>[◀ 역순 입력]</color></b> 진행: ({currentInputStep} / {reverseSequence.Length})";
                }
            }
        }
        else
        {
            StartCoroutine(FailRoutine(eyeIndex));
        }
    }

    private IEnumerator ClearStepEyes(int[] eyes)
    {
        yield return new WaitForSeconds(0.2f);
        if (!IsCleared)
        {
            for (int i = 0; i < eyes.Length; i++)
            {
                if (eyeGlowImages[eyes[i]] != null)
                    eyeGlowImages[eyes[i]].color = colEyeDefault;
            }
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
        return currentInputStep >= reverseSequence.Length;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal();

        for (int i = 0; i < eyeGlowImages.Length; i++)
        {
            if (eyeGlowImages[i] != null)
                eyeGlowImages[i].color = colEyeGold;
        }

        if (centerEmblem != null) centerEmblem.color = colGold;
        if (textDirectionArrow != null) textDirectionArrow.color = colGold;

        for (int i = 0; i < pawRewindIcons.Length; i++)
        {
            if (pawRewindIcons[i] != null) pawRewindIcons[i].color = colGold;
        }

        for (int i = 0; i < stepDots.Length; i++)
        {
            if (stepDots[i] != null) stepDots[i].color = colGold;
        }

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FFD700>고양이의 시선이 되감겨 진실을 밝혔습니다! (두 번째 패턴 기록)</color>";

        if (successNotice != null) successNotice.SetActive(true);
    }

    private IEnumerator FailRoutine(int wrongIndex)
    {
        isShowingSequence = true;

        if (textStatusNotice != null)
            textStatusNotice.text = "<color=#FF4444>순서가 틀렸습니다! 시선이 흐트러집니다.</color>";

        if (eyeGlowImages[wrongIndex] != null)
            eyeGlowImages[wrongIndex].color = colEyeWrong;

        yield return new WaitForSeconds(0.7f);

        OnFailInternal();
        PlaySequence();
    }

    public override void Reset()
    {
        if (!Application.isPlaying) return; // [추가] 에디터 Reset 가드
        base.Reset();
        currentInputStep = 0;
        isShowingSequence = false;
        pendingTargetEyes.Clear();
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        for (int i = 0; i < eyeGlowImages.Length; i++)
        {
            if (eyeGlowImages[i] != null)
                eyeGlowImages[i].color = colEyeDefault;
        }

        if (centerEmblem != null)
            centerEmblem.color = new Color(0.3f, 0.22f, 0.38f, 1f);

        if (textDirectionArrow != null)
        {
            textDirectionArrow.text = "→";
            textDirectionArrow.color = new Color(0.8f, 0.8f, 0.9f, 0.8f);
        }

        for (int i = 0; i < pawRewindIcons.Length; i++)
        {
            if (pawRewindIcons[i] != null)
                pawRewindIcons[i].color = new Color(0.75f, 0.5f, 0.9f, 0.9f);
        }

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
