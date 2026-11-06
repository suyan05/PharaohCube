using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 착시 퍼즐 씬 하나의 흐름 (서브: 판 1개 / 메인: 구역 3개를 차례로)
// - 지금 구역 판만 켜 두고 나머지는 꺼 둠 (다른 퍼즐이 안 보이게)
// - 구역을 풀면 화면이 어두워졌다가 다음 구역이 나타남 (푼 구역은 플래그로 기억 -> 다시 와도 이어서)
// - 서브를 다 풀면: 부적 지급 + 클리어 플래그 -> 허브의 그 게이트 앞으로 돌아감
// - 메인을 다 풀면: 소베크 대사 -> 형판 공개·큐브(BastetEnding) -> 허브 (물 빠짐 연출은 허브에서)
// - Esc: 퍼즐을 그만두고 허브로
// 붙이는 곳: 퍼즐 씬의 SobekPuzzle 오브젝트
public class SobekIllusionFlow : MonoBehaviour
{
    [Serializable]
    public class Stage
    {
        public SobekIllusionBoard board;
        [Tooltip("이 구역이 나타날 때 알림으로 한 줄씩")]
        [TextArea(1, 2)] public string[] lines;
    }

    [Header("구역 (순서대로, 서브는 1개)")]
    [SerializeField] private Stage[] stages;
    [Tooltip("구역 클리어 플래그 앞부분 (뒤에 번호가 붙음)")]
    [SerializeField] private string stageFlagPrefix = "F_SOBEK_PUZZLE_STAGE_";

    [Header("서브 보상 (메인이면 비움)")]
    [SerializeField] private string rewardItemId = "";
    [SerializeField] private string clearFlag = "";

    [Header("메인 마무리")]
    [SerializeField] private bool isMain;
    [SerializeField] private BastetEnding ending;
    [SerializeField] private string plateItemId = SobekIds.Plate;
    [SerializeField] private string[] altarLines =
    {
        SobekStory.Me, "소베크-라의 태양 원반이 빛난다…",
        SobekStory.Sobek, "아펩이 강을 마시기 전, 나는 마지막 범람을 이 신전에 가둬 두었다.",
        SobekStory.Sobek, "보는 자리에 따라 길을 찾는구나. 가둔 물을 강으로 돌려보내겠다.",
        SobekStory.Sobek, "그 형판을 가져가라. 큐브의 다섯 번째 면이 될 것이다.",
        SobekStory.Sobek, "물이 빠지면 물속에 잠겨 있던 문이 드러난다. 자칼이 그 너머에서 기다린다.",
    };

    [Header("돌아가기")]
    [SerializeField] private string returnScene = SobekIds.HubScene;
    [Tooltip("허브에서 이 게이트 앞에 다시 섬 (SobekPuzzleGate.gateId)")]
    [SerializeField] private string returnGateId = "";
    [Range(0.1f, 3f)] [SerializeField] private float fadeTime = 0.7f;
    [Range(1f, 6f)] [SerializeField] private float lineTime = 2.6f;

    private int current = -1;
    private bool leaving;

    private string StageFlag(int i) => stageFlagPrefix + i;
    private static bool HasFlag(string f) => !string.IsNullOrEmpty(f) && GameManager.Instance != null && GameManager.Instance.HasFlag(f);
    private static void SetFlag(string f) { if (!string.IsNullOrEmpty(f) && GameManager.Instance != null) GameManager.Instance.SetFlag(f); }

    private void Start()
    {
        if (stages == null || stages.Length == 0) { Debug.LogError("[SobekIllusionFlow] 구역이 없음", this); return; }
        foreach (Stage s in stages)
        {
            if (s.board == null) continue;
            s.board.Cleared += HandleCleared;
            s.board.gameObject.SetActive(false);
        }
        int first = 0;
        while (first < stages.Length - 1 && HasFlag(StageFlag(first))) first++; // 이미 푼 구역은 건너뜀
        StartCoroutine(Enter(first, false));
    }

    private void OnDestroy()
    {
        if (stages != null) foreach (Stage s in stages) if (s.board != null) s.board.Cleared -= HandleCleared;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame && !leaving && !SobekStory.Busy)
            StartCoroutine(Leave("퍼즐을 그만두고 신전으로 돌아간다.", false));
    }

    private IEnumerator Enter(int i, bool fade)
    {
        if (fade) yield return ScreenFader.FadeOut(fadeTime);
        if (current >= 0) stages[current].board.gameObject.SetActive(false);
        current = i;
        stages[i].board.gameObject.SetActive(true);
        if (fade) yield return ScreenFader.FadeIn(fadeTime);
        Debug.Log($"[SobekIllusionFlow] 구역 {i + 1}/{stages.Length}: {stages[i].board.Level?.DisplayName}", this);
        if (stages[i].lines == null) yield break;
        foreach (string line in stages[i].lines)
        {
            if (string.IsNullOrEmpty(line) || leaving) continue;
            AnubisToast.Show(line, lineTime);
            yield return new WaitForSeconds(lineTime);
        }
    }

    private void HandleCleared(SobekIllusionBoard b)
    {
        int i = Array.FindIndex(stages, s => s.board == b);
        if (i < 0 || leaving) return;
        SetFlag(StageFlag(i));
        if (i + 1 < stages.Length) StartCoroutine(Next(i + 1));
        else StartCoroutine(isMain ? FinishMain() : FinishSub());
    }

    private IEnumerator Next(int next)
    {
        stages[current].board.InputLocked = true;
        AnubisToast.Show("길이 이어졌다… 더 깊은 곳으로.", 2f);
        yield return new WaitForSeconds(1.2f);
        yield return Enter(next, true);
    }

    private IEnumerator FinishSub()
    {
        stages[current].board.InputLocked = true;
        bool first = !HasFlag(clearFlag);
        SetFlag(clearFlag);
        if (first && !string.IsNullOrEmpty(rewardItemId) && ItemInventory.Instance != null && !ItemInventory.Instance.Has(rewardItemId))
        {
            bool closed = false;
            ItemPopup.Grant(rewardItemId, () => closed = true);
            while (!closed) yield return null;
        }
        yield return Leave("부적을 품고 신전으로 돌아간다.", true);
    }

    private IEnumerator FinishMain()
    {
        stages[current].board.InputLocked = true;
        yield return new WaitForSeconds(0.6f);
        bool talking = true;
        SobekStory.Say(() => talking = false, altarLines);
        while (talking) yield return null;

        SetFlag(SobekIds.FlagMainDone);
        SetFlag(SobekIds.FlagTabletReady);
        Debug.Log("[SobekIllusionFlow] 메인 클리어 -> 형판 공개 -> 큐브 -> 허브(물 빠짐)", this);
        SobekPuzzleGate.SetReturn("");
        if (ending != null)
        {
            if (ItemInventory.Instance != null && !ItemInventory.Instance.Has(plateItemId)) ItemInventory.Instance.Grant(plateItemId);
            ending.Play(); // 끝나면 허브로 (BastetEnding의 다음 씬)
            yield break;
        }
        bool closed = false;
        ItemPopup.Grant(plateItemId, () => closed = true);
        while (!closed) yield return null;
        yield return Leave("물이 빠지는 소리가 들린다…", true);
    }

    private IEnumerator Leave(string msg, bool cleared)
    {
        if (leaving) yield break;
        leaving = true;
        if (current >= 0) stages[current].board.InputLocked = true;
        if (!string.IsNullOrEmpty(msg)) AnubisToast.Show(msg, fadeTime + 1f);
        SobekPuzzleGate.SetReturn(returnGateId);
        yield return ScreenFader.FadeOut(fadeTime);
        var hud = FindFirstObjectByType<SobekHud>();
        if (hud != null) hud.Hide();
        SceneFlow.LoadByNameOrNext(returnScene);
    }

    [ContextMenu("테스트: 지금 구역 건너뛰기")]
    private void TestSkip()
    {
        if (!Application.isPlaying || current < 0) return;
        HandleCleared(stages[current].board);
    }

#if UNITY_EDITOR
    public void EditorSetup(SobekIllusionBoard[] boards, string[][] lines, string flagPrefix, string reward, string flag, bool main, BastetEnding end, string gateId)
    {
        stages = new Stage[boards.Length];
        for (int i = 0; i < boards.Length; i++) stages[i] = new Stage { board = boards[i], lines = lines != null && i < lines.Length ? lines[i] : null };
        stageFlagPrefix = flagPrefix; rewardItemId = reward; clearFlag = flag; isMain = main; ending = end; returnGateId = gateId;
    }
#endif
}
