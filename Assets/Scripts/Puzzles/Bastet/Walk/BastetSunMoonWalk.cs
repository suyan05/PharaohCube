using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// B5 해와 달의 기억 (걸어서 푸는 메인 퍼즐 = B1 + B2)
// 원형으로 놓인 고양이 석상 6개, 석상마다 앞에 발자국 판 1개
// 1) 해 단계: 발자국이 1개 -> 2개 -> 3개 -> 4개로 늘어나며 빛남. 같은 순서로 밟기 (B1 방식)
// 2) 해가 지면 방이 어두워짐 (전역 조명) -> 횃불·불씨·그릇 시야가 제 역할
// 3) 달 단계: 석상 6개의 눈이 1번씩 빛남. 마지막에 빛난 석상부터 거꾸로 E (B2 방식)
// 틀리면 석상이 횃불을 먹음 + 그 라운드를 다시 보여 줌, 같은 단계에서 여러 번 틀리면 순서를 새로 섞음
// 규칙·속도·문구·색은 BastetSunMoonData(UI 버전과 같은 에셋)에서 읽음
public class BastetSunMoonWalk : BastetWalkPuzzleBase, IBastetStatueOwner
{
    private enum State { Idle, Showing, SunInput, Changing, MoonInput, Cleared }

    [Header("데이터 (UI 버전과 공용)")]
    [SerializeField] private BastetSunMoonData data;

    [Header("석상과 발자국 (같은 번호끼리 짝)")]
    [SerializeField] private BastetFootprintPlate[] plates = new BastetFootprintPlate[6];
    [SerializeField] private BastetEyeStatue[] statues = new BastetEyeStatue[6];

    [Header("밤 연출 (전역 조명)")]
    [Tooltip("비우면 씬의 Global Light 2D를 찾음")]
    [SerializeField] private Light2D globalLight;
    [Range(0f, 1f)] [SerializeField] private float nightIntensity = 0.15f;
    [SerializeField] private Color nightColor = new Color(0.55f, 0.6f, 0.95f);
    [Tooltip("클리어하면 방을 다시 밝게")]
    [SerializeField] private bool restoreDayOnClear = true;

    [Header("[추가] 난이도 (달 단계)")]
    [Tooltip("달 단계에서 눈이 켜지는 석상 수. 6 = 전부(기본), 5 = 난이도 검토 권장")]
    [Range(3, 6)] [SerializeField] private int moonLength = 6;
    [Tooltip("밤이 되면 꺼진 발자국 판도 희미하게 보이게")]
    [SerializeField] private bool dimPlatesAtNight = true;

    [Header("[추가] 해·달 원반 (아트가 오면 연결, 비우면 생략)")]
    [Tooltip("신상 머리 위 원반 SpriteRenderer")]
    [SerializeField] private SpriteRenderer discRenderer;
    [SerializeField] private Sprite sunDiscSprite;   // Bastet_Obj_ShrineDisc_Sun
    [SerializeField] private Sprite moonDiscSprite;  // Bastet_Obj_ShrineDisc_Moon

    [Header("클리어 보상")]
    [Tooltip("클리어하면 꺼지는 출구 막이 (Door_North_Exit)")]
    [SerializeField] private GameObject worldExitDoor;
    [SerializeField] private string rewardItemId = "PLATE_BASTET";

    [Header("[추가] 클리어 후 다음 씬으로 자동 이동 (토트 전까지는 아누비스로)")]
    [Tooltip("켜면 보상 팝업을 닫은 뒤 잠시 후 화면이 어두워지며 다음 씬으로 넘어감. 끄면 북쪽 출구(Exit_ToThoth)를 밟아야 넘어감")]
    [SerializeField] private bool autoExitOnClear = true;
    [Tooltip("Build Profiles의 Scene List에 있는 씬 이름. 토트 씬이 완성되면 Puzzle_Test_Thoth로 바꾸기")]
    [SerializeField] private string nextSceneName = "Puzzle_Test_Anubis_Tutorial";
    [Tooltip("보상 팝업을 닫은 뒤 기다리는 시간 (초)")]
    [Range(0f, 5f)] [SerializeField] private float autoExitDelay = 1.5f;
    [TextArea(1, 2)] [SerializeField] private string exitMessage = "아누비스의 신전으로 향합니다...";
    [Tooltip("북쪽 출구 (어두워지는 연출·폰트·아이템 회수를 같이 씀). 비우면 씬에서 찾음")]
    [SerializeField] private AnubisSceneExit sceneExit;

    private State state = State.Idle;
    private int[] sunSeq, moonSeq;
    private int round = 1, inputIndex, failCount;
    private bool inMoon, started;
    private BastetFootprintPlate lastPlate;
    private Color dayColor = Color.white;
    private float dayIntensity = 1f;
    private WaitForSeconds waitBefore, waitGap, waitFail;
    private WaitForSeconds[] waitOnByRound;

    private int Count => Mathf.Min(plates.Length, statues.Length);
    private int SunRounds => sunSeq != null ? sunSeq.Length : 0;
    private int MoonCount => Mathf.Clamp(moonLength, 1, Count); // [추가] 달 단계 길이

    public override string StarterPrompt
    {
        get
        {
            if (state == State.Cleared) return "바스테트 신상 (통과)";
            if (state == State.SunInput || state == State.MoonInput) return "E: 다시 보여 주기";
            if (state == State.Idle) return started ? "E: 바스테트 신상 다시 깨우기" : "E: 바스테트 신상 깨우기";
            return "";
        }
    }

    // ================= 초기화 =================
    private void Awake()
    {
        if (data == null)
        {
            Debug.LogWarning($"[{name}] BastetSunMoonData가 비어 있어 기본값 사용", this);
            data = ScriptableObject.CreateInstance<BastetSunMoonData>();
        }

        waitBefore = new WaitForSeconds(data.BeforeShowDelay);
        waitGap = new WaitForSeconds(data.ShowGapTime);
        waitFail = new WaitForSeconds(data.FailFlashTime);
        waitOnByRound = new WaitForSeconds[data.SunRounds + 1];
        for (int r = 0; r < data.SunRounds; r++) waitOnByRound[r] = new WaitForSeconds(data.ShowOnTime(r + 1));
        waitOnByRound[data.SunRounds] = new WaitForSeconds(data.ShowOnTime(data.SunRounds));

        sunSeq = new int[data.SunRounds];
        moonSeq = new int[Count];

        for (int i = 0; i < Count; i++)
        {
            if (plates[i] != null) { plates[i].Index = i; plates[i].OnStepped += HandleStep; }
            if (statues[i] != null) statues[i].Init(this, i);
        }

        if (globalLight == null)
            foreach (Light2D l in FindObjectsByType<Light2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (l.lightType == Light2D.LightType.Global) { globalLight = l; break; }
        if (globalLight != null) { dayColor = globalLight.color; dayIntensity = globalLight.intensity; }
        else Debug.LogWarning($"[{name}] Global Light 2D가 없어 밤 연출이 생략됨", this);

        AllOff();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < Count; i++) if (plates[i] != null) plates[i].OnStepped -= HandleStep;
    }

    // ================= 시작 / 다시 보기 =================
    public override void Begin()
    {
        if (state == State.Cleared) { Toast("이미 통과한 시험이다."); return; }
        if (state == State.Showing || state == State.Changing) return;
        StopAllCoroutines();

        SetSolving(true); // [추가] 횃불 타는 시간 멈춤
        if (!started)
        {
            started = true;
            round = 1;
            failCount = 0;
            BuildSun();
            BuildMoon();
            Toast(data.StartMessage);
            Debug.Log($"[BastetB5] 해 정답 = {Seq(sunSeq, false)}", this);
        }
        StartCoroutine(inMoon ? ShowMoon() : ShowSun());
    }

    // 해 단계: 지금 라운드 수만큼 발자국을 켰다 끔
    private IEnumerator ShowSun()
    {
        state = State.Showing;
        AllOff();
        yield return waitBefore;
        Toast(string.Format(data.SunShowMessage, round));

        WaitForSeconds on = waitOnByRound[Mathf.Clamp(round - 1, 0, waitOnByRound.Length - 1)];
        for (int i = 0; i < round; i++)
        {
            int b = sunSeq[i];
            plates[b].SetGlow(true, data.ButtonColor(b));
            yield return on;
            plates[b].SetGlow(false, data.ButtonColor(b));
            yield return waitGap;
        }

        inputIndex = 0;
        lastPlate = null;
        state = State.SunInput;
        Toast(string.Format(data.SunInputMessage, round));
    }

    private void HandleStep(BastetFootprintPlate plate)
    {
        if (state != State.SunInput || plate == lastPlate) return;
        lastPlate = plate;

        int expected = sunSeq[inputIndex];
        if (plate.Index != expected)
        {
            Debug.Log($"[BastetB5] 해 {round}라운드 {inputIndex + 1}번째 틀림: {plate.Index} (정답 {expected})", this);
            StartCoroutine(FailRoutine(plate.Index, false));
            return;
        }

        plate.SetGlow(true, data.GoldColor);
        inputIndex++;
        if (inputIndex < round) return;

        // 라운드 통과
        if (round >= SunRounds) { StartCoroutine(ChangeToMoon()); return; }
        round++;
        StartCoroutine(ShowSun());
    }

    // 해 -> 달: 방이 어두워짐
    private IEnumerator ChangeToMoon()
    {
        state = State.Changing;
        inMoon = true;
        failCount = 0;
        Toast(data.ChangeMessage);
        SetDisc(false); // [추가] 원반을 달로
        yield return FadeGlobal(nightColor, nightIntensity, data.PhaseChangeTime);
        AllOff();
        if (dimPlatesAtNight) SetPlatesDim(true); // [추가] 밤에도 판 자리가 보이게
        yield return ShowMoon();
    }

    // 달 단계: 석상 눈이 1번씩 빛남
    private IEnumerator ShowMoon()
    {
        state = State.Showing;
        AllOffEyes();
        yield return waitBefore;
        Toast(data.MoonShowMessage);

        WaitForSeconds on = waitOnByRound[waitOnByRound.Length - 1];
        for (int k = 0; k < MoonCount; k++) // [수정] Count -> MoonCount
        {
            int b = moonSeq[k];
            statues[b].SetEyes(true, EyeColor(b));
            yield return on;
            statues[b].SetEyes(false, EyeColor(b));
            yield return waitGap;
        }

        inputIndex = 0;
        state = State.MoonInput;
        Toast(data.MoonInputMessage);
    }

    // ================= IBastetStatueOwner =================
    public string PromptFor(int index) => state == State.MoonInput ? "E: 고양이 석상의 눈 누르기" : "";

    public void OnStatuePressed(int index)
    {
        if (state != State.MoonInput) return;

        int expected = moonSeq[MoonCount - 1 - inputIndex]; // 마지막에 빛난 것부터 [수정] Count -> MoonCount
        if (index != expected)
        {
            Debug.Log($"[BastetB5] 달 {inputIndex + 1}번째 틀림: {index} (정답 {expected})", this);
            StartCoroutine(FailRoutine(index, true));
            return;
        }

        statues[index].SetEyes(true, data.GoldColor);
        inputIndex++;
        if (inputIndex >= MoonCount) Submit(); // [수정]
    }

    // 틀림: 석상이 횃불을 먹음 -> 같은 라운드 다시 (여러 번 틀리면 새 순서)
    private IEnumerator FailRoutine(int index, bool moon)
    {
        state = State.Showing;
        failCount++;
        if (statues[index] != null) statues[index].EatTorch(data.WrongColor, data.FailFlashTime);
        if (!moon) plates[index].SetGlow(true, data.WrongColor);
        PunishTorch(moon ? data.MoonRetryMessage : data.SunRetryMessage);
        yield return waitFail;
        AllOff();
        if (moon) AllOffEyes();

        if (failCount >= data.FailsBeforeNewSequence)
        {
            failCount = 0;
            if (moon) BuildMoon();
            else { BuildSun(); round = 1; }
            Toast(data.NewSequenceMessage);
        }
        yield return moon ? ShowMoon() : ShowSun();
    }

    // ================= PuzzleBase =================
    public override void Submit()
    {
        if (Validate()) OnSuccessInternal();
    }

    protected override bool Validate() => inMoon && inputIndex >= MoonCount; // [수정]

    protected override void OnSuccessInternal()
    {
        if (IsCleared) return;
        base.OnSuccessInternal(); // 클리어 플래그 + OnSuccess (BastetRoomProgress가 플래그/알림)
        state = State.Cleared;
        SetSolving(false);     // [추가]
        SetPlatesDim(false);   // [추가]
        SetDisc(true);         // [추가] 아침: 해 원반

        for (int i = 0; i < Count; i++)
        {
            if (plates[i] != null) plates[i].SetGlow(true, data.GoldColor);
            if (statues[i] != null) statues[i].SetEyes(true, data.GoldColor);
        }
        if (worldExitDoor != null) worldExitDoor.SetActive(false);
        if (!string.IsNullOrEmpty(rewardItemId)) ItemPopup.Grant(rewardItemId); // [수정] 팀 공용 획득 팝업 (지급 + 팝업)
        if (restoreDayOnClear) StartCoroutine(FadeGlobal(dayColor, dayIntensity, data.PhaseChangeTime));
        Toast(data.ClearMessage);
        Debug.Log("[BastetB5] 해와 달의 기억 클리어!", this);
        if (autoExitOnClear) StartCoroutine(AutoExit()); // [추가]
    }

    // [추가] 보상 팝업·대사가 닫히면 잠시 뒤 다음 씬으로
    private IEnumerator AutoExit()
    {
        yield return null; // 팝업이 뜨는 프레임 지나고
        while (ItemPopup.IsShowing || DialogueBox.IsOpen) yield return null;
        if (autoExitDelay > 0f) yield return new WaitForSecondsRealtime(autoExitDelay);

        if (sceneExit == null) sceneExit = FindFirstObjectByType<AnubisSceneExit>();
        if (sceneExit != null && sceneExit.GoNow(nextSceneName, exitMessage))
        {
            Debug.Log($"[BastetB5] 다음 씬 '{nextSceneName}'(으)로 이동", this);
            yield break;
        }
        // 출구가 없거나 씬 이름이 Scene List에 없으면: Scene List 다음 번호 씬으로
        Debug.LogWarning($"[BastetB5] '{nextSceneName}'(으)로 못 감 (Scene List 확인) -> 다음 번호 씬으로 이동", this);
        SceneFlow.LoadByNameOrNext(nextSceneName);
    }

    public override void Reset()
    {
        if (!Application.isPlaying) return; // 에디터 Reset 가드
        base.Reset();
        if (IsCleared) return;
        StopAllCoroutines();
        state = State.Idle;
        started = false;
        inMoon = false;
        SetSolving(false);     // [추가]
        SetPlatesDim(false);   // [추가]
        SetDisc(true);         // [추가]
        AllOff();
        AllOffEyes();
        if (globalLight != null) { globalLight.color = dayColor; globalLight.intensity = dayIntensity; }
    }

    // ================= 도우미 =================
    private IEnumerator FadeGlobal(Color toColor, float toIntensity, float time)
    {
        if (globalLight == null) yield break;
        Color fromC = globalLight.color;
        float fromI = globalLight.intensity, t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            globalLight.color = Color.Lerp(fromC, toColor, k);
            globalLight.intensity = Mathf.Lerp(fromI, toIntensity, k);
            yield return null;
        }
        globalLight.color = toColor;
        globalLight.intensity = toIntensity;
    }

    // [추가] 밤 희미한 빛
    private void SetPlatesDim(bool on)
    {
        for (int i = 0; i < Count; i++) if (plates[i] != null) plates[i].SetDim(on);
    }

    // [추가] 신상 원반 그림 교체 (true = 해, false = 달)
    private void SetDisc(bool sun)
    {
        if (discRenderer == null) return;
        Sprite s = sun ? sunDiscSprite : moonDiscSprite;
        if (s != null) discRenderer.sprite = s;
    }

    private Color EyeColor(int i) => data.MoonUsesButtonColors ? data.ButtonColor(i) : data.MoonEyeColor;

    private void AllOff()
    {
        for (int i = 0; i < Count; i++) if (plates[i] != null) plates[i].SetGlow(false, Color.white);
    }

    private void AllOffEyes()
    {
        for (int i = 0; i < Count; i++) if (statues[i] != null) statues[i].SetEyes(false, Color.white);
    }

    private void BuildSun()
    {
        for (int i = 0; i < sunSeq.Length; i++)
        {
            int pick;
            do pick = Random.Range(0, Count);
            while (data.NoRepeatInRow && Count > 1 && i > 0 && pick == sunSeq[i - 1]);
            sunSeq[i] = pick;
        }
    }

    private void BuildMoon()
    {
        for (int i = 0; i < moonSeq.Length; i++) moonSeq[i] = i;
        for (int i = moonSeq.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (moonSeq[i], moonSeq[j]) = (moonSeq[j], moonSeq[i]);
        }
    }

    // [추가] 달 누를 순서 (MoonCount개만, 거꾸로)
    private string SeqMoon()
    {
        var sb = new StringBuilder();
        for (int k = MoonCount - 1; k >= 0; k--)
        {
            if (k < MoonCount - 1) sb.Append(" > ");
            sb.Append(moonSeq[k]);
        }
        return sb.ToString();
    }

    private static string Seq(int[] s, bool reverse)
    {
        var sb = new StringBuilder();
        for (int k = 0; k < s.Length; k++)
        {
            if (k > 0) sb.Append(" > ");
            sb.Append(reverse ? s[s.Length - 1 - k] : s[k]);
        }
        return sb.ToString();
    }

    // ================= 테스트 =================
    [ContextMenu("테스트: 지금 정답 출력")]
    private void TestPrint()
    {
        if (sunSeq == null) return;
        Debug.Log($"[BastetB5] 해 (지금 {round}라운드): {Seq(sunSeq, false)}\n달 누를 순서: {SeqMoon()}", this);
    }

    [ContextMenu("테스트: 해 건너뛰고 달로")]
    private void TestSkipSun()
    {
        if (!Application.isPlaying || IsCleared) return;
        StopAllCoroutines();
        if (!started) { started = true; BuildSun(); BuildMoon(); }
        StartCoroutine(ChangeToMoon());
    }

    [ContextMenu("테스트: 바로 클리어")]
    private void TestClear()
    {
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        inMoon = true;
        inputIndex = MoonCount; // [수정]
        Submit();
    }

#if UNITY_EDITOR
    public void EditorSetup(BastetSunMoonData d, BastetFootprintPlate[] p, BastetEyeStatue[] s, GameObject door)
    {
        data = d; plates = p; statues = s;
        if (door != null) worldExitDoor = door;
    }

    // [추가] 실전 방 세팅 메뉴가 출구·다음 씬을 연결
    public void EditorSetExit(AnubisSceneExit exit, string scene, string message)
    {
        sceneExit = exit;
        if (!string.IsNullOrEmpty(scene)) nextSceneName = scene;
        if (!string.IsNullOrEmpty(message)) exitMessage = message;
    }
#endif
}
