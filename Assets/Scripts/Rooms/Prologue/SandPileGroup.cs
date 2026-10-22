using System;
using System.Collections;
using UnityEngine;

// 발굴 구역 모래 더미 3개 묶음 (기획서 2.2)
// - 각 모래 더미는 Inspectable (붓 필요 + 길게 누르기 + Big/Small -> Gone 그림)
//   모래 더미 Inspectable의 Grant Items / Lines는 비워둠 -> 결과는 여기서 정함
// - 털 때마다 모래먼지가 퐁
// - 털은 순서대로: 1번째 = 토기 조각 / 2번째 = 또 토기
// - 마지막(세 번째)으로 털은 것이 항상 스카라브 (어느 더미든 상관없이)
//   -> 카메라가 가까이 + 반짝 + "!" -> 획득 팝업 -> 기쁨 -> 붕괴 연출 (DigCollapseSequence)
public class SandPileGroup : MonoBehaviour
{
    [Header("모래 더미 (Inspectable 3개)")]
    [SerializeField] private Inspectable[] piles;

    [Header("토기 조각")]
    [SerializeField] private DialogueLine[] firstLines = { new DialogueLine("나", "깨진 토기 조각이다.") };
    [SerializeField] private DialogueLine[] laterLines = { new DialogueLine("나", "또 토기다... 역시 그렇지 뭐.") };

    [Header("스카라브")]
    [SerializeField] private string scarabItem = "COL_SCARAB";
    [SerializeField] private string foundFlag = "F_PRO_SCARAB_FOUND";
    [Tooltip("Pro_Obj_SandPile_Scarab")]
    [SerializeField] private Sprite scarabSprite;
    [SerializeField, Min(1f)] private float closeUpSize = 3.5f;
    [SerializeField] private DialogueLine[] beforeLines = { new DialogueLine("나", "...잠깐. 모래 밑에 뭔가 반짝이는데?") };
    [SerializeField]
    private DialogueLine[] afterLines =
    {
        new DialogueLine("나", "청록색 스카라브...! 진짜 유물이야!"),
        new DialogueLine("나", "47일 만에 처음이라고! 빨리 본부에 알려야...")
    };

    [Header("효과 (모래 알갱이 그림, 소리는 비워도 됨)")]
    [SerializeField] private Sprite[] dustSprites;
    [SerializeField] private AudioClip brushDoneSfx;
    [SerializeField] private AudioClip glintSfx;

    [Header("붕괴")]
    [SerializeField] private DigCollapseSequence collapse;

    private int clearedCount;
    private bool scarabFound;

    private void OnEnable()
    {
        if (piles == null) return;
        foreach (Inspectable pile in piles)
        {
            if (pile != null) pile.OnCompleted += HandleCleared;
        }
    }

    private void OnDisable()
    {
        if (piles == null) return;
        foreach (Inspectable pile in piles)
        {
            if (pile != null) pile.OnCompleted -= HandleCleared;
        }
    }

    private void Start()
    {
        if (piles == null || piles.Length == 0) Debug.LogWarning("[SandPileGroup] 모래 더미가 비어 있음");
        if (collapse == null) Debug.LogWarning("[SandPileGroup] DigCollapseSequence가 비어 있음 -> 스카라브 뒤 붕괴가 안 됨");
    }

    private void HandleCleared(Inspectable pile)
    {
        try
        {
            if (scarabFound || pile == null) return;
            clearedCount++;
            CineFX.Puff(dustSprites, pile.transform.position + Vector3.up * 0.2f, 10);
            CineAudio.Play(brushDoneSfx);

            if (clearedCount >= piles.Length) StartCoroutine(FindScarab(pile));
            else StartCoroutine(Cine.Say(clearedCount == 1 ? firstLines : laterLines));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SandPileGroup] 모래 더미 결과 오류: {e}");
        }
    }

    // ================= 스카라브 =================
    private IEnumerator FindScarab(Inspectable pile)
    {
        scarabFound = true;
        if (GameManager.Instance != null) GameManager.Instance.SetFlag(foundFlag);
        Transform player = CineActor.Player();
        Vector3 spot = pile.transform.position + Vector3.up * 0.2f;

        Cine.Begin(false);
        ShowScarabSprite(pile);
        yield return CloseUp(player, spot);
        yield return GrantScarab();
        yield return EmoteBalloonUI.Show(player, "!!", 0.8f);
        yield return Cine.Say(afterLines);
        StartCollapse();
    }

    private IEnumerator CloseUp(Transform player, Vector3 spot)
    {
        CineCamera.Focus(spot);
        CineCamera.Zoom(closeUpSize, 0.8f);
        yield return Cine.Wait(0.9f);

        CineFX.Sparkle(dustSprites, spot, 14);
        CineAudio.Play(glintSfx);
        yield return EmoteBalloonUI.Show(player, "!", 0.7f);
        yield return Cine.Say(beforeLines);
    }

    private IEnumerator GrantScarab()
    {
        bool closed = false;
        ItemPopup.Grant(scarabItem, () => closed = true);
        while (!closed) yield return null;
    }

    private void ShowScarabSprite(Inspectable pile)
    {
        if (pile == null || scarabSprite == null) return;
        SpriteRenderer sr = pile.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = scarabSprite;
    }

    private void StartCollapse()
    {
        if (collapse != null) collapse.Play();
        else
        {
            Debug.LogError("[SandPileGroup] 붕괴 연출이 연결 안 됨 -> 다음 씬으로 못 넘어감 (F12로 넘기기)");
            Cine.End();
        }
    }
}
