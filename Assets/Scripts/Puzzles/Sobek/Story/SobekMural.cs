using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// [추가] 소베크의 방 전실 벽화 (4장) - 바스테트 벽화(BastetMural)와 같은 방식, 소베크 이야기로
// - 전실 위쪽 벽에 작은 도트 벽화 (Assets/image/Sobek/Story/Sobek_Mural{n}.png, 4x2칸), 플레이어보다 뒤에 그려짐
// - E: 카메라가 벽화로 다가감 -> (0) 벽화 그림 + 벽의 글 -> E -> (1) 올려다보기 -> (2) 이야기 연출 -> 다시 게임
//   (재생은 바스테트와 같은 BastetMuralViewer, 대본은 SobekMuralScenes)
// - 처음 볼 때 고고학 수첩 메모 [노트 40~43] + "벽화 n/4" 알림
// - 4장을 다 보면 F_SO_MURALS_ALL + 케프리가 다음 할 일(소용돌이 섬부터)을 알려 줌
// - 안 본 벽화는 은은한 빛 + "!" 말풍선
// 붙이는 곳: Mural_Sobek_1~4 (Tools/Sobek/7 허브 보강 메뉴가 만듦)
public class SobekMural : MonoBehaviour, IInteractable
{
    public const string FlagAll = "F_SO_MURALS_ALL";
    public static string Flag(int i) => $"F_SO_MURAL_{i}";

    [Header("벽화 번호 (1~4) - 그림과 이야기가 이 번호로 정해짐")]
    [Range(1, 4)] [SerializeField] private int index = 1;

    [Header("연출")]
    [SerializeField] private bool playLook = true;
    [SerializeField] private bool playStory = true;
    [Tooltip("두 번째로 볼 때도 연출까지 (끄면 그림만)")]
    [SerializeField] private bool storyOnRevisit = false;
    [Tooltip("다가갈 때 카메라 크기 (작을수록 크게 확대)")]
    [Range(0.8f, 4f)] [SerializeField] private float zoomSize = 1.6f;
    [Range(0.2f, 1.5f)] [SerializeField] private float approachSeconds = 0.55f;
    [Tooltip("카메라가 볼 높이 (벽화 그림 가운데)")]
    [SerializeField] private float focusHeight = 1f;

    [Header("고고학 수첩 메모 (0이면 안 함)")]
    [SerializeField] private int noteNumber = 40;

    [Header("연결 (비우면 자동)")]
    [SerializeField] private SpriteRenderer slab;
    [SerializeField] private SpriteRenderer newBubble;

    [Header("안 본 벽화 빛")]
    [SerializeField] private Color glowColor = new Color(0.55f, 0.85f, 0.8f);
    [Range(0.5f, 5f)] [SerializeField] private float glowRadius = 2.6f;
    [Range(0f, 2f)] [SerializeField] private float glowIntensity = 0.6f;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 3f;

    [Header("4장을 다 봤을 때 (이름, 글 짝)")]
    [SerializeField] private string[] allSeenLines =
    {
        SobekStory.Khepri, "소베크가 마지막 물을 이 신전에 가둬 둔 거야. 그 물을 강으로 돌려보내야 배가 다시 떠.",
        SobekStory.Khepri, "네 섬의 시험을 지나 부적을 모으자. 길이 막힌 섬은 앞 섬의 부적으로 열 수 있을 거야.",
        SobekStory.Me, "먼저 왼쪽 위, 소용돌이 섬부터 가 보자.",
    };

    private Light2D glow;
    private Vector3 bubbleBasePos;
    private bool playing;

    public int Index => index;
    public string Title => SobekMuralScenes.Title(index);
    public bool Seen => SobekStory.HasFlag(Flag(index));

    public static int SeenCount()
    {
        int n = 0;
        for (int i = 1; i <= SobekMuralScenes.Count; i++) if (SobekStory.HasFlag(Flag(i))) n++;
        return n;
    }

    private void Start()
    {
        if (slab == null) slab = GetComponent<SpriteRenderer>();
        SortBehindPlayer();
        float h = slab != null && slab.sprite != null ? slab.sprite.bounds.size.y : 2f;
        glow = RaGlowFx.PointLight(transform, new Vector3(0f, h * 0.5f, 0f), glowColor, glowRadius, glowIntensity);
        if (newBubble != null) bubbleBasePos = newBubble.transform.localPosition;
    }

    // 플레이어와 같은 정렬 레이어에서 2칸 뒤, "!"는 플레이어 위
    private void SortBehindPlayer()
    {
        Transform p = CineActor.Player();
        SpriteRenderer psr = p != null ? p.GetComponentInChildren<SpriteRenderer>() : null;
        int layer = psr != null ? psr.sortingLayerID : SortingLayer.NameToID("Objects");
        int order = psr != null ? psr.sortingOrder : 0;
        if (slab != null) { slab.sortingLayerID = layer; slab.sortingOrder = order - 2; }
        if (newBubble != null) { newBubble.sortingLayerID = layer; newBubble.sortingOrder = order + 5; }
    }

    private void Update()
    {
        bool seen = Seen;
        if (glow != null)
        {
            float target = seen ? glowIntensity * 0.25f : glowIntensity * (0.8f + 0.2f * Mathf.Sin(Time.time * 2.2f));
            glow.intensity = Mathf.MoveTowards(glow.intensity, target, Time.deltaTime * 2f);
        }
        if (newBubble == null) return;
        bool show = !seen && !playing && !Cine.Running;
        newBubble.enabled = show;
        if (show) newBubble.transform.localPosition = bubbleBasePos + new Vector3(0f, Mathf.Sin(Time.time * bobSpeed) * bobHeight, 0f);
    }

    public string GetPrompt()
    {
        if (playing || Cine.Running) return null;
        return $"E: 벽화 '{Title}' 보기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (playing || DialogueBox.IsOpen || DialogueBox.ClosedFrame == Time.frameCount || Cine.Running || BastetMuralViewer.IsPlaying || SobekStory.Busy) return;
            StartCoroutine(View());
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 벽화 오류: {e}", this);
        }
    }

    private IEnumerator View()
    {
        playing = true;
        bool first = !Seen;
        int number = SeenCount() + (first ? 1 : 0);
        bool more = first || storyOnRevisit;
        Debug.Log($"[SobekMural] 벽화 {index} '{Title}' 보기 ({(first ? "처음" : "다시")})", this);

        Cine.Begin(true);
        CineCamera.Focus(transform.position + Vector3.up * focusHeight);
        CineCamera.Zoom(zoomSize, 0.7f);
        GameAudio.Play("sfx_dream_shimmer", 0.5f, 0.8f);
        yield return null; // 벽화를 연 그 E가 다가가기를 바로 끝내지 않게
        yield return Cine.Wait(approachSeconds);

        MuralShot[] shots = SobekMuralScenes.For(index, playLook && more, playStory && more);
        yield return BastetMuralViewer.Get().Play(SobekMuralScenes.PictureTitle(index, number), SobekMuralScenes.WallLine(index),
            SobekMuralScenes.PictureArt(index), shots);

        CineCamera.Release();
        Cine.End();
        playing = false;
        if (first) FirstView();
    }

    private void FirstView()
    {
        SobekStory.SetFlag(Flag(index));
        int seen = SeenCount();
        if (noteNumber > 0)
            Debug.Log($"[노트 {noteNumber}] 벽화 '{Title}': {SobekMuralScenes.WallLine(index)}"); // NotebookNotes가 이 로그를 받아 수첩에 저장
        AnubisToast.Show($"벽화 {seen}/{SobekMuralScenes.Count}  -  고고학 수첩에 기록되었다 (J)", 2.5f);
        Debug.Log($"[SobekMural] 벽화 {index} 처음 봄 ({seen}/{SobekMuralScenes.Count})", this);

        if (seen >= SobekMuralScenes.Count && !SobekStory.HasFlag(FlagAll))
        {
            SobekStory.SetFlag(FlagAll);
            Debug.Log("[SobekMural] 벽화 4장 모두 봄 -> 케프리 안내", this);
            if (allSeenLines != null && allSeenLines.Length >= 2) SobekStory.Say(allSeenLines);
        }
    }

    private void OnDisable()
    {
        if (playing && Cine.Running) Cine.End();
        playing = false;
    }

    [ContextMenu("테스트: 이 벽화 연출 보기")]
    private void TestView()
    {
        if (Application.isPlaying && !playing && !Cine.Running) StartCoroutine(View());
    }

    [ContextMenu("테스트: 이 벽화를 본 것으로 처리")]
    private void TestMarkSeen()
    {
        if (Application.isPlaying && !Seen) FirstView();
    }

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
