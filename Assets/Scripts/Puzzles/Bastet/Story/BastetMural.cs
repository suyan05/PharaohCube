using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// [추가] 바스테트의 방 벽화 (4장). 스토리 기획서: 벽화를 모두 보면 방 스토리 연출
// - E: 벽화 조사 창(CluePanel, 비석과 같은 화면)에 제목 + 그림 + 글
// - 처음 볼 때 고고학 수첩 메모 [노트 n] + "벽화 n/4" 알림
// - 4장을 다 보면 F_BASTET_MURALS_ALL -> BastetStoryDirector가 방 스토리 연출을 틈
// - 안 본 벽화는 어둠 속에서도 보이게 은은한 빛 + "!" 말풍선, 본 뒤에는 빛이 약해짐
// 붙이는 곳: 가운데 홀 Mural_Bastet_1~4 (Tools/Bastet/★ 스토리·검은 고양이 세팅 메뉴가 만듦)
public class BastetMural : MonoBehaviour, IInteractable
{
    [Header("벽화 번호 (1~4)")]
    [Range(1, 4)] [SerializeField] private int index = 1;

    [Header("내용")]
    [SerializeField] private string title = "라의 눈";
    [TextArea(2, 4)] [SerializeField] private string body = "";
    [Tooltip("조사 창에 크게 보일 벽화 그림 (아트가 오면 연결. 비우면 글만 보임)")]
    [SerializeField] private Sprite illustration;

    [Header("고고학 수첩 메모 (0이면 안 함)")]
    [SerializeField] private int noteNumber = 16;
    [TextArea(1, 3)] [SerializeField] private string noteText = "";

    [Header("연결 (비우면 자동으로 찾음)")]
    [SerializeField] private CluePanel panel;
    [Tooltip("벽화 석판 그림 (비우면 임시 석판)")]
    [SerializeField] private SpriteRenderer slab;
    [Tooltip("\"!\" 말풍선 (자식 SpriteRenderer)")]
    [SerializeField] private SpriteRenderer newBubble;

    [Header("안 본 벽화 빛")]
    [SerializeField] private Color glowColor = new Color(1f, 0.78f, 0.45f);
    [Range(0.5f, 5f)] [SerializeField] private float glowRadius = 2.6f;
    [Range(0f, 2f)] [SerializeField] private float glowIntensity = 0.75f;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 3f;

    // 벽화를 처음 봤을 때 (번호)
    public static event Action<int> Viewed;

    private Light2D glow;
    private Vector3 bubbleBasePos;

    public int Index => index;
    public bool Seen => BastetStoryIds.Has(BastetStoryIds.MuralFlag(index));

    // ================= 초기화 =================
    private void Start()
    {
        if (panel == null) panel = FindFirstObjectByType<CluePanel>(FindObjectsInactive.Include);
        if (panel == null) Debug.LogError($"[{name}] 씬에 CluePanel이 없음 -> Tools/Bastet/실전 방 세팅 먼저 실행", this);

        if (slab == null) slab = GetComponent<SpriteRenderer>();
        if (slab != null && slab.sprite == null) slab.sprite = BastetPixelArt.MuralSlab;

        float h = slab != null && slab.sprite != null ? slab.sprite.bounds.size.y : 2f;
        glow = RaGlowFx.PointLight(transform, new Vector3(0f, h * 0.5f, 0f), glowColor, glowRadius, glowIntensity);

        if (newBubble != null) bubbleBasePos = newBubble.transform.localPosition;
    }

    // ================= 빛 / "!" =================
    private void Update()
    {
        bool seen = Seen;
        bool panelOpen = panel != null && panel.IsOpen;

        if (glow != null)
        {
            // 안 봤으면 숨 쉬듯 깜빡, 봤으면 1/4 밝기
            float target = seen ? glowIntensity * 0.25f : glowIntensity * (0.8f + 0.2f * Mathf.Sin(Time.time * 2.2f));
            glow.intensity = Mathf.MoveTowards(glow.intensity, target, Time.deltaTime * 2f);
        }

        if (newBubble == null) return;
        bool show = !seen && !panelOpen && !Cine.Running;
        newBubble.enabled = show;
        if (show) newBubble.transform.localPosition = bubbleBasePos + new Vector3(0f, Mathf.Sin(Time.time * bobSpeed) * bobHeight, 0f);
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (Cine.Running) return null;
        return panel != null && panel.IsOpen ? "E: 닫기" : "E: 벽화 살펴보기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (panel == null || Cine.Running) return;
            if (panel.IsOpen) { panel.Hide(); return; }
            // 창을 닫은 바로 그 E로 다시 열리지 않게
            if (panel.ClosedFrame == Time.frameCount || DialogueBox.IsOpen || DialogueBox.ClosedFrame == Time.frameCount) return;

            var content = new ClueData.ClueContent
            {
                title = $"벽화 {index} - {title}",
                body = body,
                illustration = illustration,
                illustrationTint = Color.white
            };
            panel.Show(content, null, who);

            if (!Seen) FirstView();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 벽화 오류: {e}", this);
        }
    }

    private void FirstView()
    {
        BastetStoryIds.Set(BastetStoryIds.MuralFlag(index));
        int seen = BastetStoryIds.MuralsSeen();

        if (noteNumber > 0)
        {
            string memo = string.IsNullOrEmpty(noteText) ? $"벽화 '{title}': {body}" : noteText;
            Debug.Log($"[노트 {noteNumber}] {memo}"); // NotebookNotes가 이 로그를 받아 수첩 메모에 저장
        }
        AnubisToast.Show($"벽화 {seen}/{BastetStoryIds.MuralCount}  ·  고고학 수첩에 기록되었다 (J)", 2.5f);
        Debug.Log($"[BastetMural] 벽화 {index} 처음 봄 ({seen}/{BastetStoryIds.MuralCount})", this);

        Viewed?.Invoke(index);
        if (seen >= BastetStoryIds.MuralCount) BastetStoryIds.Set(BastetStoryIds.FlagMuralsAll);
    }

    // ================= 테스트 =================
    [ContextMenu("테스트: 이 벽화를 본 것으로 처리")]
    private void TestMarkSeen()
    {
        if (Application.isPlaying && !Seen) FirstView();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = glowColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, glowRadius);
    }

#if UNITY_EDITOR
    public void EditorSetup(int i, string t, string b, int note, string memo, CluePanel p, SpriteRenderer s, SpriteRenderer bubble)
    {
        index = i; title = t; body = b; noteNumber = note; noteText = memo; panel = p; slab = s; newBubble = bubble;
    }
#endif
}
