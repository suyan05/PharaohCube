using System;
using System.Collections;
using UnityEngine;

// H5 호루스의 하늘 (몸으로 푸는 퍼즐) - 예전 화면 퍼즐을 '제단 둘레를 도는 밤하늘'로 바꿈
// 판정/정답/힌트/보상은 그대로 SkyAlignPuzzle이 함 (이 스크립트는 별을 움직이고 SelectStar를 부르기만 함)
// 신화 + 이집트 천문:
// - 별은 동쪽에서 떠올라 서쪽으로 진다 -> 별 4개가 제단 둘레를 천천히 돌고, 남쪽 절반(지평선 아래)에 있는 별은 띄울 수 없음
// - 메스케티우(북두칠성)는 '지지 않는 별' -> 북쪽에 멈춰 있음 (노트 3: 마지막까지 하늘에 남는다)
// - 붉은 별 = 형제를 죽인 세트의 별 -> 띄우면 하늘이 흔들리고 처음부터
// - 별 4개를 띄우면 매(호루스)가 날아와 제단 한가운데 앉음 = 다섯 번째 입력 (노트 5: 매는 모든 별이 모인 뒤 한가운데 앉는다)
// 제단을 E로 조사하면: 제단에 새겨진 성좌 카드 + 노트 (예전 화면의 왼쪽 카드/오른쪽 단서)
[RequireComponent(typeof(SkyAlignPuzzle))]
public class HorusSkyRing : MonoBehaviour, IInteractable
{
    public const int FalconIndex = 5;
    private const int StarCount = 5; // 0 메스케티우 ~ 4 붉은 별 (5 = 매는 제단)

    [Header("연결 (빌더가 채움)")]
    [SerializeField] private SkyAlignPuzzle puzzle;
    [Tooltip("0 메스케티우, 1 사흐, 2 소프데트, 3 우자트, 4 붉은 별 (H5_02~05, H5_07)")]
    [SerializeField] private Sprite[] starSprites = new Sprite[StarCount];

    [Header("하늘 고리")]
    [SerializeField] private Vector2 radius = new Vector2(2.7f, 2.0f);
    [SerializeField] private float orbitSeconds = 30f;
    [SerializeField] private float starSize = 0.8f;

    private HorusSkyStar[] stars;
    private float phase;
    private bool shown, landing, done;

    public bool Busy => landing || !puzzle.CanInput;
    private Vector3 Center => transform.position;

    // ================= 준비 =================
    private void Start()
    {
        try
        {
            if (puzzle == null) puzzle = GetComponent<SkyAlignPuzzle>();
            HorusFx.MatchPlayer();
            BuildStars();
            puzzle.OnFail += HandleFail;
            done = HorusDirector.HasFlag(DoneFlag());
            SetShown(puzzle.IsAwake || done);
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘 고리] 준비 오류: {e}");
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (puzzle != null) puzzle.OnFail -= HandleFail;
    }

    private static string DoneFlag() => "F_HO_H5_DONE";

    private void BuildStars()
    {
        stars = new HorusSkyStar[StarCount];
        for (int i = 0; i < StarCount; i++)
        {
            Sprite s = starSprites != null && i < starSprites.Length ? starSprites[i] : null;
            stars[i] = HorusSkyStar.Create(this, i, s, starSize, puzzle.Stars[i].label, i == 4);
        }
    }

    private void SetShown(bool on)
    {
        shown = on;
        foreach (HorusSkyStar s in stars) if (s != null) s.gameObject.SetActive(on);
    }

    // ================= 매 프레임: 별이 돈다 =================
    private void Update()
    {
        try
        {
            if (stars == null) return;
            if (!shown && puzzle.IsAwake) StartCoroutine(Awaken());
            if (!shown) return;
            done |= puzzle.IsCleared;
            if (!done && !Cine.Running && !DialogueBox.IsOpen) phase += Time.deltaTime / orbitSeconds * 360f;
            for (int i = 0; i < StarCount; i++) Place(i);
            if (!done && !landing && puzzle.CanInput && puzzle.Input.Count == StarCount - 1) StartCoroutine(FalconLands());
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘 고리] 갱신 오류: {e}");
            enabled = false;
        }
    }

    private void Place(int i)
    {
        float angle = i == 0 ? 90f : puzzle.Stars[i].angle + phase; // 메스케티우는 북쪽에 멈춰 있음
        float rad = angle * Mathf.Deg2Rad;
        Vector3 pos = Center + new Vector3(Mathf.Cos(rad) * radius.x, Mathf.Sin(rad) * radius.y, 0f);
        bool risen = i == 0 || Mathf.Sin(rad) > -0.05f;
        bool lit = done || Contains(puzzle.Input, i);
        stars[i].SetState(pos, risen, lit, puzzle.TrapHint && i == 4, puzzle.NextHintStar == i);
    }

    private static bool Contains(System.Collections.Generic.IReadOnlyList<int> list, int v)
    {
        for (int k = 0; k < list.Count; k++) if (list[k] == v) return true;
        return false;
    }

    // 성좌 4개를 다 밝히면 제단 둘레에 별이 하나씩 떠오름
    private IEnumerator Awaken()
    {
        SetShown(true);
        GameAudio.Play("sfx_dream_shimmer", 0.7f, 0.9f);
        HorusFx.StarBurst(Center, radius, 24, new Color(0.65f, 0.8f, 1f));
        yield return null;
    }

    // ================= 별 띄우기 (HorusSkyStar가 부름) =================
    public void TryRaise(int index, bool risen)
    {
        if (Busy || done) return;
        if (!risen)
        {
            DialogueBox.Play(new[] { new DialogueLine("나", "지평선 아래로 진 별이야. 동쪽에서 다시 떠오를 때까지 기다리자.") });
            return;
        }
        puzzle.SelectStar(index);
        GameAudio.Play("sfx_blip_horus", 0.55f, 0.8f + puzzle.Input.Count * 0.12f);
        HorusFx.StarBurst(stars[index].transform.position, new Vector2(0.3f, 0.3f), 8, new Color(1f, 0.85f, 0.45f));
    }

    // 별 4개가 떠오르면: 매가 날아와 제단 한가운데 앉음 -> 다섯 번째 입력(매) -> SkyAlignPuzzle이 판정
    private IEnumerator FalconLands()
    {
        landing = true;
        yield return new WaitForSeconds(0.6f);
        HorusFalconGuide falcon = FindFirstObjectByType<HorusFalconGuide>();
        if (falcon != null) yield return falcon.PerchAt(Center + Vector3.up * 0.3f, false);
        GameAudio.Play("sfx_wind_gust", 0.5f, 1.2f);
        puzzle.SelectStar(FalconIndex);
        yield return new WaitForSeconds(0.8f);
        landing = false;
    }

    // 틀렸을 때 (SkyAlignPuzzle.OnFail -> 아직 입력이 남아 있는 순간)
    private void HandleFail()
    {
        try
        {
            bool trap = Contains(puzzle.Input, 4);
            CineCamera.Shake(0.3f, trap ? 0.08f : 0.04f);
            StartCoroutine(ScreenTintUI.Flash(new Color(1f, 0.25f, 0.2f, trap ? 0.4f : 0.25f), 0.03f, 0.06f, 0.4f));
            string line = trap
                ? "붉은 별에 손이 닿자 하늘이 흔들린다... 형제를 죽인 자의 별은 따르면 안 돼."
                : "매가 앉자마자 별들이 흩어졌다. 별이 떠오르는 순서가 틀렸나 봐. 제단의 글을 다시 읽어 보자.";
            DialogueBox.Play(new[] { new DialogueLine("나", line) });
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘 고리] 실패 연출 오류: {e}");
        }
    }

    // ================= 제단 E: 새겨진 글 읽기 =================
    public string GetPrompt()
    {
        if (done) return "E: 제단 보기";
        return puzzle.IsAwake ? "E: 제단의 글 읽기" : "E: 매의 제단 조사";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (DialogueBox.IsOpen || Cine.Running) return;
            if (done) DialogueBox.Play(new[] { new DialogueLine("나", "하늘은 다시 호루스의 것이 되었다.") });
            else if (!puzzle.IsAwake) DialogueBox.Play(new[] { new DialogueLine("나", "제단 위 하늘이 아직 어둡다. 성도판의 성좌 네 개를 모두 밝혀야 할 것 같아.") });
            else DialogueBox.Play(InscriptionLines());
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘 고리] Interact 오류: {e}");
        }
    }

    private DialogueLine[] InscriptionLines()
    {
        var lines = new System.Collections.Generic.List<DialogueLine>
        {
            new DialogueLine("제단", "\"별이 떠오른 순서대로 하늘에 올려라. 매는 모든 별이 모인 뒤 한가운데 앉는다.\""),
            new DialogueLine("나", "별은 동쪽에서 떠올라 서쪽으로 지는구나. 지평선 아래(남쪽)로 진 별은 띄울 수 없겠어.")
        };
        foreach (string c in puzzle.GetCardLines()) lines.Add(new DialogueLine("제단", c));
        foreach (string n in puzzle.GetNoteLines()) lines.Add(new DialogueLine("수첩", n));
        return lines.ToArray();
    }
}
