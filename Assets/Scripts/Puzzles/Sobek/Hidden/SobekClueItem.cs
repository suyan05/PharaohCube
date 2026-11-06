using System.Collections;
using UnityEngine;

// 소베크 단서 1개 (기획서 8.3 ClueItem): 줍기 -> 렌즈 해독 -> 수첩 기록 -> 화면 위 빈 홈이 하나 채워짐
//  단서 1: 전실 바닥 범람 비석 조각 (처음부터)
//  단서 2: 물 빠진 수로 바닥 진흙 (메인 퍼즐을 깨고 물이 빠지면 나타남)
//  단서 3: 제단 옆 갈대 덤불 (물이 빠진 뒤. 악어가 꼬리로 갈대를 치면 반짝임, 혼자 찾아도 됨)
// 붙이는 곳: 단서 오브젝트 (SpriteRenderer + 트리거 콜라이더)
[RequireComponent(typeof(Collider2D))]
public class SobekClueItem : MonoBehaviour, IInteractable
{
    [Header("단서 번호 (1~3)")]
    [Range(1, 3)] [SerializeField] private int index = 1;

    [Header("나타나는 조건 (비우면 처음부터)")]
    [SerializeField] private string spawnFlag = "";
    [Tooltip("조건 전에도 보이는 물건이면(갈대 등) 이 문구를 보여 줌. 비우면 조건 전엔 숨김")]
    [TextArea(1, 2)] [SerializeField] private string beforeSpawnText = "";

    [Header("해독 문장 (렌즈로 읽음)")]
    [SerializeField] private string title = "범람 조각";
    [TextArea(2, 4)] [SerializeField] private string sentence = "나일이 넘치면 검은 흙이 남고, 땅은 다시 산다.";
    [Range(6, 20)] [SerializeField] private int charsPerLine = 12;

    [Header("주운 뒤 대사 (이름, 글 짝. 비우면 없음)")]
    [SerializeField] private string[] afterLines = { };

    [Header("수첩 메모 번호 (0 = 안 함)")]
    [SerializeField] private int noteNumber;

    [Header("주운 뒤 숨길 그림 (비우면 이 오브젝트 SpriteRenderer)")]
    [SerializeField] private SpriteRenderer pickupRenderer;
    [SerializeField] private bool keepObjectAfterPickup; // 갈대처럼 물건은 남고 단서만 사라짐

    private Collider2D col;
    private bool busy;

    public int Index => index;
    private string GotFlag => SobekIds.ClueGot(index);

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (pickupRenderer == null) pickupRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnFlagChanged += HandleFlag;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
        if (busy) PlayerLock.Unlock(this);
        busy = false;
    }

    private void Start()
    {
        if (GameManager.Instance != null) { GameManager.Instance.OnFlagChanged -= HandleFlag; GameManager.Instance.OnFlagChanged += HandleFlag; }
        RefreshVisible();
    }

    private void HandleFlag(string f)
    {
        if (f == spawnFlag || f == GotFlag) RefreshVisible();
    }

    private bool Spawned => string.IsNullOrEmpty(spawnFlag) || SobekStory.HasFlag(spawnFlag);
    private bool Got => SobekStory.HasFlag(GotFlag);

    private void RefreshVisible()
    {
        bool objectVisible = keepObjectAfterPickup || (!Got && (Spawned || !string.IsNullOrEmpty(beforeSpawnText)));
        if (pickupRenderer != null) pickupRenderer.enabled = objectVisible;
        if (col != null) col.enabled = objectVisible && !(Got && !keepObjectAfterPickup);
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (busy) return "";
        if (Got) return keepObjectAfterPickup ? "갈대 덤불" : "";
        if (!Spawned) return "E: 살펴보기";
        return "E: 단서 줍기";
    }

    public void Interact(GameObject player)
    {
        if (busy || Got || SobekStory.Busy) return;
        if (!Spawned)
        {
            if (!string.IsNullOrEmpty(beforeSpawnText)) AnubisToast.Show(beforeSpawnText, 2.5f);
            return;
        }
        StartCoroutine(PickRoutine());
    }

    private IEnumerator PickRoutine()
    {
        busy = true;
        PlayerLock.Lock(this);
        yield return SteleDecodeUI.Decode(title, RaNightClueText.ToLines(sentence, charsPerLine));
        PlayerLock.Unlock(this);
        busy = false;

        SobekStory.SetFlag(GotFlag);
        Debug.Log($"[SobekClueItem] 단서 {index} 주움: {sentence}", this);
        if (noteNumber > 0)
        {
            Debug.Log($"[노트 {noteNumber}] 「{sentence}」"); // NotebookNotes가 이 로그를 받아 수첩에 저장
            AnubisToast.Show($"[노트 {noteNumber}] 고고학 수첩에 기록되었다 (J)", 2.5f);
        }
        AnubisToast.Show($"단서 {CountGot()} / 3 — 수위계의 빈 눈금에 맞을 것 같다.", 2.5f);
        SobekClueHud.Refresh();
        RefreshVisible();
        yield return null;
        if (afterLines != null && afterLines.Length >= 2) SobekStory.Say(afterLines);
    }

    private static int CountGot()
    {
        int n = 0;
        for (int i = 1; i <= 3; i++) if (SobekStory.HasFlag(SobekIds.ClueGot(i))) n++;
        return n;
    }

    // 악어 신호용: 단서 위치에서 반짝
    public void Sparkle()
    {
        if (pickupRenderer == null || Got) return;
        for (int i = 0; i < 6; i++)
            RaGlowFx.Spark(transform.position + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0f, 0.6f), 0f),
                new Color(0.6f, 0.95f, 1f), pickupRenderer.sortingLayerName, pickupRenderer.sortingOrder + 1);
    }

    [ContextMenu("테스트: 이 단서 바로 얻기")]
    private void TestGet()
    {
        if (!Application.isPlaying) return;
        SobekStory.SetFlag(GotFlag);
        SobekClueHud.Refresh();
        RefreshVisible();
    }

#if UNITY_EDITOR
    public void EditorSetup(int i, string spawn, string before, string t, string body, string[] after, int note, bool keepObject)
    {
        index = i; spawnFlag = spawn; beforeSpawnText = before; title = t; sentence = body; afterLines = after; noteNumber = note; keepObjectAfterPickup = keepObject;
    }
#endif
}
