using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// B5 바스테트 메인 퍼즐 "해와 달의 기억" (뮤지컬 메모리 방식, B1 발자국 + B2 고양이 눈 혼합)
// 신화: 낮의 바스테트는 라의 눈 - 태양의 배를 따라 걸으며 발자국을 남긴다
//       밤의 바스테트는 수호자 - 아펩이 오지 못하게 지나온 길을 거슬러 되짚는다
// 1단계 (해): 같은 발자국 순서가 1개 -> 2개 -> 3개 -> 4개로 늘어나며 빛남. 보인 순서대로 석상 버튼 누르기
// 2단계 (달): 해 원반이 달 원반으로 뒤집히고, 고양이 눈 6개가 1번씩 빛남. 마지막에 빛난 눈부터 거꾸로 누르기
// 퍼즐 팝업 루트(Popup_Bastet5_SunMoon)에 부착. 메뉴 Tools > Bastet > B5 해와 달의 기억 UI 만들기 로 자동 생성
public class BastetSunMoonMemoryPuzzle : PuzzleBase
{
    public enum Phase { Idle, Showing, SunInput, Changing, MoonInput, Cleared }

    [Serializable]
    public class StatueButton
    {
        [Tooltip("누르는 버튼 (석상 받침)")]
        public Button button;
        [Tooltip("해 단계에 빛나는 발자국")]
        public Image footprint;
        [Tooltip("달 단계에 빛나는 고양이 눈")]
        public Image eye;
    }

    [Header("데이터")]
    [Tooltip("규칙, 속도, 색, 소리, 문구. 비우면 기본값으로 동작")]
    [SerializeField] private BastetSunMoonData data;

    [Header("고양이 석상 버튼 (왼쪽부터 순서대로)")]
    [SerializeField] private StatueButton[] statues = new StatueButton[6];

    [Header("해 / 달 원반")]
    [SerializeField] private Image sunMoonDisk;
    [Tooltip("그림이 없을 때 원반 위에 '해' / '달' 글자 표시")]
    [SerializeField] private TMP_Text diskLabel;

    [Header("진행 표시 (해 라운드 수 + 달 1개)")]
    [SerializeField] private Image[] progressDots;

    [Header("[추가] 퍼즐 화면 배경 (데이터의 Frame Sprite를 입힘)")]
    [SerializeField] private Image frameImage;

    [Header("UI")]
    [Tooltip("퍼즐 팝업 루트. 비우면 이 오브젝트")]
    [SerializeField] private GameObject puzzleRoot;
    [SerializeField] private TMP_Text textStatus;
    [SerializeField] private Button btnRestart;
    [SerializeField] private Button btnClose;
    [SerializeField] private GameObject successNotice;

    [Header("클리어 보상 (기존 B5에서 옮겨 옴)")]
    [SerializeField] private GameObject catStencilRewardPanel;
    [Tooltip("클리어하면 꺼지는 출구 막이")]
    [SerializeField] private GameObject worldExitDoor;
    [Tooltip("클리어 보상 아이템 ID (비우면 지급 안 함)")]
    [SerializeField] private string rewardItemId = "PLATE_BASTET";
    [SerializeField] private UnityEvent onCleared;

    [Header("소리 (선택)")]
    [SerializeField] private AudioSource audioSource;

    [Header("플레이어")]
    [Tooltip("퍼즐 창이 열려 있는 동안 플레이어 이동 잠금")]
    [SerializeField] private bool lockPlayerWhileOpen = true;

    // 단계가 바뀔 때 (연출, 사운드 연결용)
    public event Action<Phase> OnPhaseChanged;

    public Phase CurrentPhase => phase;

    private Phase phase = Phase.Idle;
    private int[] sunSeq;          // 해 단계 정답 (길이 = 해 라운드 수)
    private int[] moonSeq;         // 달 단계에서 눈이 빛난 순서 (정답은 이 순서의 역순)
    private int round = 1;         // 지금 해 라운드 (1부터)
    private int inputIndex;        // 이번 라운드에서 맞게 누른 개수
    private int failCount;         // 같은 단계에서 틀린 횟수
    private bool inMoon;           // 달 단계로 넘어갔는지
    private bool needsReplay;      // 보여 주는 도중 창을 닫았으면 다시 열 때 처음부터 보여 줌

    private Coroutine flowRoutine;
    private Coroutine[] flashRoutines;
    private PlayerController2D player;
    private Rigidbody2D playerRb;
    private bool playerLocked;

    // 코루틴 대기 객체 재사용 (GC 줄이기)
    private WaitForSeconds waitBefore, waitGap, waitFlash, waitFail;
    private WaitForSeconds[] waitOnByRound;

    private int ButtonCount => Mathf.Min(data.ButtonCount, statues != null ? statues.Length : 0);
    private int SunRounds => sunSeq != null ? sunSeq.Length : 0;

    // ================= 초기화 =================
    private void Awake()
    {
        if (data == null)
        {
            Debug.LogWarning($"[{name}] BastetSunMoonData가 비어 있어 기본값으로 동작", this);
            data = ScriptableObject.CreateInstance<BastetSunMoonData>();
        }
        if (puzzleRoot == null) puzzleRoot = gameObject;

        ApplyArt(); // [추가] 레이아웃 가이드 그림 적용 (없으면 색 네모 그대로)

        sunSeq = new int[data.SunRounds];
        moonSeq = new int[ButtonCount];
        flashRoutines = new Coroutine[ButtonCount];
        CacheWaits();

        for (int i = 0; i < ButtonCount; i++)
        {
            int index = i; // 람다 캡처용
            if (statues[i] != null && statues[i].button != null)
                statues[i].button.onClick.AddListener(() => OnStatuePressed(index));
            else
                Debug.LogError($"[{name}] 석상 버튼 {i + 1}번이 연결되지 않음", this);
        }

        if (btnRestart != null) btnRestart.onClick.AddListener(RestartPuzzle);
        if (btnClose != null) btnClose.onClick.AddListener(Close);
        if (successNotice != null) successNotice.SetActive(false);
        if (catStencilRewardPanel != null) catStencilRewardPanel.SetActive(false);
    }

    // [추가] 데이터에 그림이 있으면 석상/배경/진행 표시에 입히고, 빛 그림을 석상 크기로 겹침 (Awake에서 1번만)
    private void ApplyArt()
    {
        if (frameImage != null && data.FrameSprite != null)
        {
            frameImage.sprite = data.FrameSprite;
            frameImage.color = Color.white;
            Transform inner = frameImage.transform.Find("FrameInner"); // 빌더가 만든 안쪽 색판은 그림을 가리므로 끔
            if (inner != null) inner.gameObject.SetActive(false);
        }

        for (int i = 0; i < statues.Length; i++)
        {
            if (statues[i] == null || statues[i].button == null) continue;

            if (data.StatueSprite != null && statues[i].button.targetGraphic is Image bg)
            {
                bg.sprite = data.StatueSprite;
                bg.color = Color.white;
                bg.preserveAspect = true;
            }

            if (data.GlowIsFullCanvas)
            {
                StretchToParent(statues[i].eye);
                StretchToParent(statues[i].footprint);
            }
        }

        if (data.ProgressSprite != null && progressDots != null)
        {
            for (int i = 0; i < progressDots.Length; i++)
            {
                if (progressDots[i] == null) continue;
                progressDots[i].sprite = data.ProgressSprite;
                progressDots[i].preserveAspect = true;
                bool moonDot = i == progressDots.Length - 1;
                progressDots[i].rectTransform.sizeDelta = moonDot ? new Vector2(60f, 60f) : new Vector2(48f, 48f);
            }
        }
    }

    private static void StretchToParent(Image img)
    {
        if (img == null) return;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        img.preserveAspect = true;
        img.raycastTarget = false; // 겹친 빛이 버튼 클릭을 막지 않게
    }

    private void CacheWaits()
    {
        waitBefore = new WaitForSeconds(data.BeforeShowDelay);
        waitGap = new WaitForSeconds(data.ShowGapTime);
        waitFlash = new WaitForSeconds(data.PressFlashTime);
        waitFail = new WaitForSeconds(data.FailFlashTime);

        // 라운드별 켜짐 시간 (마지막 칸 = 달 단계용)
        waitOnByRound = new WaitForSeconds[data.SunRounds + 1];
        for (int r = 0; r < data.SunRounds; r++) waitOnByRound[r] = new WaitForSeconds(data.ShowOnTime(r + 1));
        waitOnByRound[data.SunRounds] = new WaitForSeconds(data.ShowOnTime(data.SunRounds));
    }

    private void OnEnable()
    {
        PuzzleOverlayFocus.Enter(this); // 다른 시스템(안내 연출, 일시정지)에 "퍼즐 열림" 알림
        LockPlayer(true);
    }

    private void OnDisable()
    {
        PuzzleOverlayFocus.Exit(this);
        LockPlayer(false);

        // 보여 주는 중이거나 입력 중에 닫으면, 다시 열 때 같은 라운드를 처음부터 보여 줌
        if (phase != Phase.Idle && phase != Phase.Cleared) needsReplay = true;
        StopAllCoroutines();
        flowRoutine = null;
        for (int i = 0; i < flashRoutines.Length; i++) flashRoutines[i] = null;
    }

    private void Update()
    {
        // 새 Input System: Esc로 닫기
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
    }

    // ================= 열기 / 닫기 =================
    public override void Open()
    {
        // 꺼진 상태에서 불려도 Awake가 먼저 돌도록 켜고 시작
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        if (puzzleRoot != null && !puzzleRoot.activeSelf) puzzleRoot.SetActive(true);
        base.Open();

        if (!CanEnter())
        {
            SetButtonsInteractable(false);
            SetStatus(data.LockedMessage);
            return;
        }

        if (IsCleared)
        {
            ShowClearedVisuals();
            SetStatus(data.ClearMessage);
            return;
        }

        if (phase == Phase.Idle) StartNewGame();
        else if (needsReplay) ReplayCurrent();
    }

    public void Close()
    {
        if (puzzleRoot != null) puzzleRoot.SetActive(false);
        else gameObject.SetActive(false);
    }

    // ================= 진행 =================
    private void StartNewGame()
    {
        failCount = 0;
        round = 1;
        inMoon = false;
        needsReplay = false;

        BuildSunSequence();
        BuildMoonSequence();
        TurnAllOff();
        ResetDots();
        SetDisk(false);

        Debug.Log($"[BastetB5] 새 게임. 해 정답 = {SeqToString(sunSeq, sunSeq.Length, false)}", this);
        SetStatus(data.StartMessage);
        StartFlow(ShowSunRoutine());
    }

    private void ReplayCurrent()
    {
        needsReplay = false;
        TurnAllOff();
        SetDisk(inMoon);
        StartFlow(inMoon ? ShowMoonRoutine() : ShowSunRoutine());
    }

    // 해 단계: 지금 라운드 수만큼 발자국을 차례로 켰다 끔
    private IEnumerator ShowSunRoutine()
    {
        SetPhase(Phase.Showing);
        SetButtonsInteractable(false);
        HighlightCurrentDot();
        yield return waitBefore;

        SetStatus(string.Format(data.SunShowMessage, round));
        WaitForSeconds waitOn = waitOnByRound[Mathf.Clamp(round - 1, 0, waitOnByRound.Length - 1)];

        for (int i = 0; i < round; i++)
        {
            int b = sunSeq[i];
            SetFootprint(b, true);
            PlayTone(b);
            yield return waitOn;
            SetFootprint(b, false);
            yield return waitGap;
        }

        inputIndex = 0;
        SetPhase(Phase.SunInput);
        SetButtonsInteractable(true);
        SetStatus(string.Format(data.SunInputMessage, round));
        flowRoutine = null;
    }

    // 해 -> 달 전환: 원반을 동전처럼 뒤집어 달로 바꿈
    private IEnumerator ChangeToMoonRoutine()
    {
        SetPhase(Phase.Changing);
        SetButtonsInteractable(false);
        TurnAllOff();
        SetStatus(data.ChangeMessage);
        PlayClip(data.PhaseChangeClip);

        inMoon = true;
        failCount = 0;

        float half = data.PhaseChangeTime * 0.5f;
        yield return FlipDisk(1f, 0f, half);
        SetDisk(true);
        yield return FlipDisk(0f, 1f, half);

        HighlightCurrentDot();
        yield return ShowMoonRoutine();
    }

    private IEnumerator FlipDisk(float from, float to, float duration)
    {
        if (sunMoonDisk == null) yield break;

        RectTransform rt = sunMoonDisk.rectTransform;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float x = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            rt.localScale = new Vector3(x, 1f, 1f);
            yield return null;
        }
        rt.localScale = new Vector3(to, 1f, 1f);
    }

    // 달 단계: 고양이 눈 6개가 1번씩 빛남
    private IEnumerator ShowMoonRoutine()
    {
        SetPhase(Phase.Showing);
        SetButtonsInteractable(false);
        yield return waitBefore;

        SetStatus(data.MoonShowMessage);
        WaitForSeconds waitOn = waitOnByRound[waitOnByRound.Length - 1];

        for (int k = 0; k < ButtonCount; k++)
        {
            int b = moonSeq[k];
            SetEye(b, true);
            PlayTone(b);
            yield return waitOn;
            SetEye(b, false);
            yield return waitGap;
        }

        inputIndex = 0;
        SetPhase(Phase.MoonInput);
        SetButtonsInteractable(true);
        SetStatus(data.MoonInputMessage);
        flowRoutine = null;
    }

    // ================= 입력 =================
    private void OnStatuePressed(int index)
    {
        if (phase == Phase.SunInput) HandleSunPress(index);
        else if (phase == Phase.MoonInput) HandleMoonPress(index);
    }

    private void HandleSunPress(int index)
    {
        PlayTone(index);

        int expected = sunSeq[inputIndex];
        if (index != expected)
        {
            Debug.Log($"[BastetB5] 해 {round}라운드 {inputIndex + 1}번째 틀림: 누름 {index + 1}, 정답 {expected + 1}", this);
            StartFlow(FailRoutine());
            return;
        }

        Flash(index, false);
        inputIndex++;
        if (inputIndex < round) return;

        // 이번 라운드 성공
        SetDot(round - 1, data.GoldColor);
        Debug.Log($"[BastetB5] 해 {round}라운드 통과", this);

        if (round >= SunRounds)
        {
            StartFlow(ChangeToMoonRoutine());
            return;
        }

        round++;
        StartFlow(ShowSunRoutine());
    }

    private void HandleMoonPress(int index)
    {
        PlayTone(index);

        int expected = MoonAnswerAt(inputIndex);
        if (index != expected)
        {
            Debug.Log($"[BastetB5] 달 {inputIndex + 1}번째 틀림: 누름 {index + 1}, 정답 {expected + 1}", this);
            StartFlow(FailRoutine());
            return;
        }

        // 맞게 누른 눈은 켜진 채로 둬서 진행 상황을 보여 줌
        SetEye(index, true);
        inputIndex++;
        if (inputIndex < ButtonCount) return;

        SetDot(SunRounds, data.GoldColor);
        Submit();
    }

    // 달 단계 정답: 빛난 순서의 역순
    private int MoonAnswerAt(int k) => moonSeq[ButtonCount - 1 - k];

    private IEnumerator FailRoutine()
    {
        Phase failedIn = phase;
        SetPhase(Phase.Showing);
        SetButtonsInteractable(false);
        PlayClip(data.WrongClip);
        failCount++;

        PaintAllWrong();
        yield return waitFail;
        TurnAllOff();

        bool newSequence = failCount >= data.FailsBeforeNewSequence;

        if (failedIn == Phase.MoonInput)
        {
            if (newSequence)
            {
                BuildMoonSequence();
                failCount = 0;
                SetStatus(data.NewSequenceMessage);
            }
            else
            {
                SetStatus(data.MoonRetryMessage);
            }
            yield return ShowMoonRoutine();
            yield break;
        }

        if (newSequence)
        {
            BuildSunSequence();
            failCount = 0;
            round = 1;
            ResetDots();
            SetStatus(data.NewSequenceMessage);
            Debug.Log($"[BastetB5] 해 정답 새로 섞음 = {SeqToString(sunSeq, sunSeq.Length, false)}", this);
        }
        else
        {
            if (data.SunFailRestartsFromRoundOne)
            {
                round = 1;
                ResetDots();
            }
            SetStatus(data.SunRetryMessage);
        }
        yield return ShowSunRoutine();
    }

    // ================= PuzzleBase =================
    public override void Submit()
    {
        if (Validate()) OnSuccessInternal();
    }

    protected override bool Validate()
    {
        return inMoon && inputIndex >= ButtonCount;
    }

    protected override void OnSuccessInternal()
    {
        if (IsCleared) return;
        base.OnSuccessInternal(); // IsCleared + clearFlag + OnSuccess 이벤트

        SetPhase(Phase.Cleared);
        SetButtonsInteractable(false);
        ShowClearedVisuals();
        SetStatus(data.ClearMessage);
        PlayClip(data.ClearClip);

        if (successNotice != null) successNotice.SetActive(true);
        if (catStencilRewardPanel != null) catStencilRewardPanel.SetActive(true);
        if (worldExitDoor != null) worldExitDoor.SetActive(false);

        if (!string.IsNullOrEmpty(rewardItemId))
        {
            if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(rewardItemId);
            else Debug.LogWarning($"[{name}] ItemInventory가 없어 {rewardItemId}를 지급하지 못함", this);
        }

        onCleared?.Invoke();
        Debug.Log("[BastetB5] 해와 달의 기억 클리어!", this);
    }

    // 처음부터 다시 (새 순서)
    public override void Reset()
    {
        if (!Application.isPlaying) return; // PuzzleBase의 Reset 가드 (에디터 Reset 메뉴 무시)
        base.Reset();
        if (IsCleared) return;

        StopAllCoroutines();
        flowRoutine = null;
        phase = Phase.Idle;
        if (isActiveAndEnabled) StartNewGame();
    }

    public void RestartPuzzle() => Reset();

    // ================= 표시 =================
    private void SetFootprint(int i, bool lit)
    {
        if (!IsValid(i)) return;
        Paint(statues[i].footprint, lit, data.FootprintOnSprite, data.FootprintOffSprite, data.ButtonColor(i));
    }

    private void SetEye(int i, bool lit)
    {
        if (!IsValid(i)) return;
        Color litColor = data.MoonUsesButtonColors ? data.ButtonColor(i) : data.MoonEyeColor;
        Paint(statues[i].eye, lit, data.EyeOnSprite, data.EyeOffSprite, litColor);
    }

    // 그림이 있으면 그림 교체 + 색 입히기, 없으면 색만 바꿈
    private void Paint(Image img, bool lit, Sprite onSprite, Sprite offSprite, Color litColor)
    {
        if (img == null) return;

        Sprite s = lit ? onSprite : offSprite;
        if (s != null) img.sprite = s;

        if (lit) img.color = (data.TintWithButtonColor || onSprite == null) ? litColor : Color.white;
        else if (offSprite != null) img.color = Color.white;
        else img.color = data.GlowIsFullCanvas ? Color.clear : data.OffColor; // [수정] 겹치기 모드면 꺼진 빛은 투명
    }

    // 누른 버튼을 잠깐 반짝임 (해 = 발자국, 달 = 눈)
    private void Flash(int i, bool eye)
    {
        if (!IsValid(i)) return;
        if (flashRoutines[i] != null) StopCoroutine(flashRoutines[i]);
        flashRoutines[i] = StartCoroutine(FlashRoutine(i, eye));
    }

    private IEnumerator FlashRoutine(int i, bool eye)
    {
        if (eye) SetEye(i, true); else SetFootprint(i, true);
        yield return waitFlash;
        if (eye) SetEye(i, false); else SetFootprint(i, false);
        flashRoutines[i] = null;
    }

    private void TurnAllOff()
    {
        for (int i = 0; i < ButtonCount; i++)
        {
            if (flashRoutines[i] != null) { StopCoroutine(flashRoutines[i]); flashRoutines[i] = null; }
            SetFootprint(i, false);
            SetEye(i, false);
        }
    }

    private void PaintAllWrong()
    {
        for (int i = 0; i < ButtonCount; i++)
        {
            if (!IsValid(i)) continue;
            if (statues[i].footprint != null) statues[i].footprint.color = data.WrongColor;
            if (statues[i].eye != null) statues[i].eye.color = data.WrongColor;
        }
    }

    private void ShowClearedVisuals()
    {
        for (int i = 0; i < ButtonCount; i++)
        {
            if (!IsValid(i)) continue;
            Paint(statues[i].footprint, true, data.FootprintOnSprite, data.FootprintOffSprite, data.GoldColor);
            Paint(statues[i].eye, true, data.EyeOnSprite, data.EyeOffSprite, data.GoldColor);
        }
        if (progressDots != null)
            for (int i = 0; i < progressDots.Length; i++) SetDot(i, data.GoldColor);
        SetDisk(true);
        SetButtonsInteractable(false);
    }

    private void SetDisk(bool moon)
    {
        if (sunMoonDisk == null) return;

        Sprite s = moon ? data.MoonSprite : data.SunSprite;
        if (s != null) sunMoonDisk.sprite = s;
        sunMoonDisk.color = s != null ? Color.white : (moon ? data.MoonColor : data.SunColor);
        sunMoonDisk.rectTransform.localScale = Vector3.one;

        if (diskLabel != null)
        {
            diskLabel.gameObject.SetActive(s == null);
            diskLabel.text = moon ? "달" : "해";
        }
    }

    private void ResetDots()
    {
        if (progressDots == null) return;
        for (int i = 0; i < progressDots.Length; i++) SetDot(i, data.OffColor);
    }

    // 지금 진행 중인 칸을 해/달 색으로 표시
    private void HighlightCurrentDot()
    {
        int index = inMoon ? SunRounds : round - 1;
        SetDot(index, inMoon ? data.MoonColor : data.SunColor);
    }

    private void SetDot(int index, Color c)
    {
        if (progressDots == null || index < 0 || index >= progressDots.Length || progressDots[index] == null) return;
        progressDots[index].color = c;
    }

    private void SetButtonsInteractable(bool on)
    {
        for (int i = 0; i < ButtonCount; i++)
            if (IsValid(i) && statues[i].button != null) statues[i].button.interactable = on;
    }

    private void SetStatus(string msg)
    {
        if (textStatus != null) textStatus.text = msg;
    }

    private void SetPhase(Phase p)
    {
        if (phase == p) return;
        phase = p;
        OnPhaseChanged?.Invoke(p);
    }

    private bool IsValid(int i) => statues != null && i >= 0 && i < statues.Length && statues[i] != null;

    private void StartFlow(IEnumerator routine)
    {
        if (flowRoutine != null) StopCoroutine(flowRoutine);
        flowRoutine = StartCoroutine(routine);
    }

    // ================= 정답 만들기 =================
    private void BuildSunSequence()
    {
        int count = ButtonCount;
        for (int i = 0; i < sunSeq.Length; i++)
        {
            int pick;
            do
            {
                pick = UnityEngine.Random.Range(0, count);
            }
            while (data.NoRepeatInRow && count > 1 && i > 0 && pick == sunSeq[i - 1]);
            sunSeq[i] = pick;
        }
    }

    // 고양이 눈 6개를 1번씩 섞음 (Fisher-Yates)
    private void BuildMoonSequence()
    {
        for (int i = 0; i < moonSeq.Length; i++) moonSeq[i] = i;
        for (int i = moonSeq.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (moonSeq[i], moonSeq[j]) = (moonSeq[j], moonSeq[i]);
        }
    }

    // ================= 소리 =================
    private void PlayTone(int i)
    {
        if (audioSource == null || data.ToneClip == null) return;
        audioSource.pitch = data.Pitch(i);
        audioSource.PlayOneShot(data.ToneClip, data.Volume);
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(clip, data.Volume);
    }

    // ================= 플레이어 잠금 =================
    private void LockPlayer(bool locked)
    {
        if (!lockPlayerWhileOpen || locked == playerLocked) return;

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController2D>();
            if (player != null) playerRb = player.GetComponent<Rigidbody2D>();
        }
        if (player == null) return;

        player.enabled = !locked;
        if (locked && playerRb != null) playerRb.linearVelocity = Vector2.zero;
        playerLocked = locked;
    }

    // ================= 테스트용 =================
    private static string SeqToString(int[] seq, int count, bool reverse)
    {
        var sb = new StringBuilder();
        for (int k = 0; k < count; k++)
        {
            int v = reverse ? seq[count - 1 - k] : seq[k];
            if (k > 0) sb.Append(" > ");
            sb.Append(v + 1);
        }
        return sb.ToString();
    }

    [ContextMenu("테스트: 지금 정답 순서 출력")]
    private void TestPrintAnswer()
    {
        if (sunSeq == null || moonSeq == null) { Debug.Log("[BastetB5] Play 중에 사용"); return; }
        Debug.Log($"[BastetB5] 해 정답 (지금 {round}라운드): {SeqToString(sunSeq, sunSeq.Length, false)}\n" +
                  $"달 빛난 순서: {SeqToString(moonSeq, moonSeq.Length, false)} -> 누를 순서: {SeqToString(moonSeq, moonSeq.Length, true)}", this);
    }

    [ContextMenu("테스트: 해 단계 건너뛰고 달로")]
    private void TestSkipToMoon()
    {
        if (!Application.isPlaying || IsCleared) return;
        round = SunRounds;
        for (int i = 0; i < SunRounds; i++) SetDot(i, data.GoldColor);
        StartFlow(ChangeToMoonRoutine());
    }

    [ContextMenu("테스트: 바로 클리어")]
    private void TestClear()
    {
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        flowRoutine = null;
        inMoon = true;
        inputIndex = ButtonCount;
        Submit();
    }

    [ContextMenu("테스트: 처음부터 새 순서로")]
    private void TestRestart() => Reset();

    // Scene 뷰 (Play 중, 이 오브젝트 선택 시): 지금 단계 정답 순서를 선으로 표시
    // 해 = 금색 선, 달 = 크림색 선 (누를 순서대로)
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || sunSeq == null || statues == null) return;

        int count = inMoon ? ButtonCount : round;
        Gizmos.color = inMoon ? new Color(0.94f, 0.86f, 0.76f) : new Color(0.91f, 0.70f, 0.23f);

        for (int k = 0; k < count; k++)
        {
            int b = inMoon ? MoonAnswerAt(k) : sunSeq[k];
            if (!IsValid(b) || statues[b].button == null) continue;
            Vector3 p = statues[b].button.transform.position;
            Gizmos.DrawWireSphere(p, 12f + k * 4f); // 뒤로 갈수록 큰 원 = 순서

            if (k == 0) continue;
            int prev = inMoon ? MoonAnswerAt(k - 1) : sunSeq[k - 1];
            if (IsValid(prev) && statues[prev].button != null)
                Gizmos.DrawLine(statues[prev].button.transform.position, p);
        }
    }
}
