using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 서쪽 벽 틈 = 숨은 방(매의 회랑) 복선
// - 어두운 방에서도 멀리서 보이게: 금 간 자국 사이로 푸른빛이 숨 쉬듯 새어 나옴 (Unlit + 작은 빛)
// - 틈에서 먼지가 바람에 실려 방 쪽으로 날아옴 (벽 너머에 공간이 있다는 느낌)
// - 처음 가까이 가면: 바람 소리 + 머리 위 "!" + 혼잣말 한 번 + 수첩 메모
// - 우자트 부적을 가지고 있으면 빛이 더 세고 빠르게 뜀 (석상에 끼우러 가라는 신호)
// - 회랑이 열리면 HorusHiddenGallery가 이 오브젝트를 꺼버림
public class HorusWallWhisper : MonoBehaviour
{
    [Header("연결 (빌더가 채움)")]
    [SerializeField] private SpriteRenderer crack;
    [SerializeField] private Light2D leak;
    [SerializeField] private Sprite emoteSprite;
    [SerializeField] private string amuletItem = HorusItemIds.WedjatAmulet;

    [Header("알아채기")]
    [SerializeField] private float noticeRadius = 3.5f;
    [SerializeField] private string noticedFlag = "F_HO_CRACK_NOTICED";
    [SerializeField] private int noteId = 30;

    [Header("모양")]
    [SerializeField] private Color dustColor = new Color(0.65f, 0.85f, 1f, 0.8f);
    [SerializeField] private Vector2 windDir = new Vector2(1f, 0.15f);

    private static readonly DialogueLine[] NoticeLines =
    {
        new DialogueLine("나", "...? 어디서 찬 바람이 불어오지?"),
        new DialogueLine("나", "서쪽 벽 틈으로 푸른빛이 새어 나와. 이 벽 너머에... 공간이 있는 것 같아.")
    };

    private float dustTimer;
    private bool noticing;

    private void Start()
    {
        try
        {
            if (crack != null && RaDarkness.Unlit != null) crack.sharedMaterial = RaDarkness.Unlit;
        }
        catch (Exception e)
        {
            Debug.LogError($"[벽 틈] 준비 오류: {e}");
        }
    }

    private void Update()
    {
        try
        {
            bool strong = HasAmulet();
            Breathe(strong);
            Dust(strong);
            TryNotice();
        }
        catch (Exception e)
        {
            Debug.LogError($"[벽 틈] 갱신 오류: {e}");
            enabled = false;
        }
    }

    // 빛이 숨 쉬듯 밝아졌다 어두워짐 (부적이 있으면 더 세고 빠르게)
    private void Breathe(bool strong)
    {
        float speed = strong ? 4.5f : 1.8f;
        float k = 0.5f + 0.5f * Mathf.Sin(Time.time * speed);
        if (leak != null) leak.intensity = Mathf.Lerp(0.25f, strong ? 1.3f : 0.75f, k);
        if (crack != null) crack.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.65f, 1f, k));
    }

    // 틈 여기저기서 작은 먼지가 바람에 실려 방 쪽으로 날아가며 사라짐
    private void Dust(bool strong)
    {
        dustTimer -= Time.deltaTime;
        if (dustTimer > 0f) return;
        dustTimer = UnityEngine.Random.Range(0.25f, strong ? 0.4f : 0.7f);
        Vector3 from = transform.position + new Vector3(0.3f, UnityEngine.Random.Range(-0.9f, 0.9f), 0f);
        HorusDust.Spawn(from, windDir.normalized, dustColor);
    }

    private void TryNotice()
    {
        if (noticing || HorusDirector.HasFlag(noticedFlag)) return;
        if (Cine.Running || DialogueBox.IsOpen || HorusFocus.PanelOpen) return;
        Transform p = CineActor.Player();
        if (p == null || Vector2.Distance(p.position, transform.position) > noticeRadius) return;
        noticing = true;
        HorusDirector.SetFlag(noticedFlag);
        GameAudio.Play("sfx_wind_gust", 0.6f, 0.75f);
        HorusFx.MatchPlayer();
        StartCoroutine(HorusFx.Emote(p, emoteSprite, 1.2f));
        DialogueBox.Play(NoticeLines);
        Debug.Log($"[노트 {noteId}] 서쪽 벽 틈 - 푸른빛과 바람이 샌다. 벽 너머에 공간? 한쪽 눈이 빈 매 석상이 이쪽을 보고 있다.");
    }

    private bool HasAmulet()
    {
        return ItemInventory.Instance != null && !string.IsNullOrEmpty(amuletItem) && ItemInventory.Instance.Has(amuletItem);
    }
}
