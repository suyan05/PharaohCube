using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// [수정] 바스테트의 방 벽화 (4장) - 호루스 입구 복도 벽화와 같은 방식으로 바꿈
// - 벽에는 작은 도트 벽화 그림 (Assets/image/Bastet/Story/Bastet_Mural{n}.png, 4x2칸)
// - E: 게임 카메라가 벽화로 쑥 다가가며 어두워짐
//      -> (1) 플레이어 등 뒤에서 큰 벽화를 올려다보는 장면 + 벽화 글 '벽화 이름 (n/4)'
//      -> (2) 그 그림의 이야기 연출 (벽화 속 인물이 움직임, BastetMuralScenes)
//      -> 다시 게임 화면
// - 처음 볼 때 고고학 수첩 메모 [노트 16~19] + "벽화 n/4" 알림
// - 4장을 다 보면 F_BASTET_MURALS_ALL -> BastetStoryDirector가 방 스토리 연출
// - 안 본 벽화는 어둠 속에서도 보이게 은은한 빛 + "!" 말풍선, 본 뒤에는 빛이 약해짐
// - 연출 중 ESC 길게 = 건너뛰기 (그래도 '본 것'으로 처리)
// 붙이는 곳: Mural_Bastet_1~4 (Tools/Bastet/★ 스토리·검은 고양이 세팅 메뉴가 만듦)
public class BastetMural : MonoBehaviour, IInteractable
{
    [Header("벽화 번호 (1~4) - 그림과 이야기가 이 번호로 정해짐")]
    [Range(1, 4)] [SerializeField] private int index = 1;

    [Header("연출")]
    [Tooltip("올려다보기 다음에 그림 이야기 연출까지 보여 줌 (끄면 올려다보기만)")]
    [SerializeField] private bool playStory = true;
    [Tooltip("두 번째로 볼 때도 이야기 연출까지 (끄면 올려다보기만)")]
    [SerializeField] private bool storyOnRevisit = false;
    [Tooltip("다가갈 때 카메라 크기 (작을수록 크게 확대)")]
    [Range(0.8f, 4f)] [SerializeField] private float zoomSize = 1.6f;
    [Tooltip("다가가는 시간 (초)")]
    [Range(0.2f, 1.5f)] [SerializeField] private float approachSeconds = 0.55f;
    [Tooltip("카메라가 볼 높이 (벽화 그림 가운데)")]
    [SerializeField] private float focusHeight = 1f;

    [Header("고고학 수첩 메모 (0이면 안 함)")]
    [SerializeField] private int noteNumber = 16;

    [Header("연결 (비우면 자동)")]
    [Tooltip("벽에 걸린 도트 벽화 그림 (비우면 임시 석판)")]
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
    private bool playing;

    public int Index => index;
    public string Title => BastetMuralScenes.Title(index);
    public bool Seen => BastetStoryIds.Has(BastetStoryIds.MuralFlag(index));

    // ================= 초기화 =================
    private void Start()
    {
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
        if (glow != null)
        {
            // 안 봤으면 숨 쉬듯 깜빡, 봤으면 1/4 밝기
            float target = seen ? glowIntensity * 0.25f : glowIntensity * (0.8f + 0.2f * Mathf.Sin(Time.time * 2.2f));
            glow.intensity = Mathf.MoveTowards(glow.intensity, target, Time.deltaTime * 2f);
        }

        if (newBubble == null) return;
        bool show = !seen && !playing && !Cine.Running;
        newBubble.enabled = show;
        if (show) newBubble.transform.localPosition = bubbleBasePos + new Vector3(0f, Mathf.Sin(Time.time * bobSpeed) * bobHeight, 0f);
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (playing || Cine.Running) return null;
        return $"E: 벽화 '{Title}' 보기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (playing || DialogueBox.IsOpen || DialogueBox.ClosedFrame == Time.frameCount || Cine.Running || MuralStage.IsPlaying) return;
            Cine.Run(View());
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 벽화 오류: {e}", this);
        }
    }

    // 카메라가 벽화로 다가감 -> 올려다보기 -> 이야기 연출 -> 원래대로
    private IEnumerator View()
    {
        playing = true;
        bool first = !Seen;
        int number = BastetStoryIds.MuralsSeen() + (first ? 1 : 0);
        Debug.Log($"[BastetMural] 벽화 {index} '{Title}' 보기 ({(first ? "처음" : "다시")})", this);

        Cine.Begin(true);
        CineCamera.Focus(transform.position + Vector3.up * focusHeight);
        CineCamera.Zoom(zoomSize, 0.7f);
        GameAudio.Play("sfx_dream_shimmer", 0.5f, 0.8f);
        yield return Cine.Wait(approachSeconds);

        yield return MuralStage.Play(BastetMuralScenes.For(index, number, playStory && (first || storyOnRevisit)));

        CineCamera.Release();
        Cine.End();
        if (first) FirstView(); // 연출이 끝난 뒤 알림 (4장째면 방 스토리 연출이 이어서 시작)
        playing = false;
    }

    private void FirstView()
    {
        BastetStoryIds.Set(BastetStoryIds.MuralFlag(index));
        int seen = BastetStoryIds.MuralsSeen();

        if (noteNumber > 0)
            Debug.Log($"[노트 {noteNumber}] 벽화 '{Title}': {BastetMuralScenes.WallLines[index - 1]}"); // NotebookNotes가 이 로그를 받아 수첩에 저장
        AnubisToast.Show($"벽화 {seen}/{BastetStoryIds.MuralCount}  ·  고고학 수첩에 기록되었다 (J)", 2.5f);
        Debug.Log($"[BastetMural] 벽화 {index} 처음 봄 ({seen}/{BastetStoryIds.MuralCount})", this);

        Viewed?.Invoke(index);
        if (seen >= BastetStoryIds.MuralCount) BastetStoryIds.Set(BastetStoryIds.FlagMuralsAll);
    }

    private void OnDisable()
    {
        playing = false;
    }

    // ================= 테스트 =================
    [ContextMenu("테스트: 이 벽화 연출 보기")]
    private void TestView()
    {
        if (Application.isPlaying && !playing && !Cine.Running) Cine.Run(View());
    }

    [ContextMenu("테스트: 이 벽화를 본 것으로 처리")]
    private void TestMarkSeen()
    {
        if (Application.isPlaying && !Seen) FirstView();
    }

    // Scene 뷰: 빛 범위 + 카메라가 다가갈 자리
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = glowColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, glowRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + Vector3.up * focusHeight, new Vector3(zoomSize * 2f * 16f / 9f, zoomSize * 2f, 0f));
    }

#if UNITY_EDITOR
    public void EditorSetup(int i, int note, SpriteRenderer s, SpriteRenderer bubble)
    {
        index = i; noteNumber = note; slab = s; newBubble = bubble;
    }
#endif
}
