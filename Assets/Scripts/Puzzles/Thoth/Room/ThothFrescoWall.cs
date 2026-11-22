using System;
using System.Collections;
using UnityEngine;

// 부서진 벽화 (서쪽 A / 동쪽 B / 북쪽 벽감 C)
// E: 처음엔 바닥에 흩어진 6조각을 주움 -> 벽화 맞추기 화면 (ThothFrescoUI)
//    다 맞추면: 벽화 그림이 완성본으로 바뀌고 -> 카메라가 다가가며 벽화가 살아 움직이는 컷 (ThothMuralScenes)
//    A를 되살리면 '토트의 여백 메모' (글자 뜻 + 읽는 방향)
// 완성 뒤 E: 벽화 컷 다시 보기
public class ThothFrescoWall : MonoBehaviour, IInteractable
{
    [SerializeField] private string fresco = "A";
    [SerializeField] private SpriteRenderer art;     // 벽 그림 (빌더가 채움)
    [SerializeField] private SpriteRenderer glint;   // 아직 안 맞춘 벽화 표시 (빛 번짐)

    private bool playing;

    private string DoneFlag => ThothIds.FrescoDone(fresco);
    private bool IsDone => ThothState.Has(DoneFlag);

    private void Start()
    {
        RefreshArt();
    }

    private void Update()
    {
        if (glint == null) return;
        glint.enabled = !IsDone;
        if (glint.enabled) glint.color = new Color(1f, 0.86f, 0.5f, 0.22f + Mathf.Sin(Time.time * 2.2f) * 0.12f);
    }

    public void RefreshArt()
    {
        if (art == null) return;
        Sprite s = ThothState.Art($"World/TH_Fresco{fresco}_{(IsDone ? "Done" : "Wall")}");
        if (s != null) art.sprite = s;
    }

    public string GetPrompt()
    {
        if (IsDone) return $"E: 벽화 '{ThothFrescoData.Title(fresco)}' 다시 보기";
        return ThothState.Has(ThothIds.WallPieces(fresco)) ? "E: 벽화 맞추기" : "E: 부서진 벽화 살펴보기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (playing || ThothState.Busy) return;
            if (IsDone) Cine.Run(PlayStory(false));
            else if (!ThothState.Has(ThothIds.WallPieces(fresco))) Cine.Run(FirstLook());
            else ThothFrescoUI.Open(fresco, OnSolved);
        }
        catch (Exception e)
        {
            Debug.LogError($"[토트 벽화 {fresco}] Interact 오류: {e}");
        }
    }

    // 처음: 무너진 벽화를 보고 6조각을 주움 -> 바로 맞추기 화면
    private IEnumerator FirstLook()
    {
        playing = true;
        Cine.Begin(true);
        CineCamera.Focus(transform.position);
        CineCamera.Zoom(3.6f, 0.6f);
        yield return Cine.Wait(0.5f);
        yield return Cine.Say(ThothLines.WallFirst(fresco));
        ThothState.Set(ThothIds.WallPieces(fresco));
        yield return ShowPopup(ThothIds.PieceItem(fresco));
        Cine.End();
        playing = false;
        ThothFrescoUI.Open(fresco, OnSolved);
    }

    private void OnSolved()
    {
        ThothState.Set(DoneFlag);
        RefreshArt();
        Cine.Run(PlayStory(true));
    }

    // 벽화 컷: 카메라가 벽화로 다가가며 어두워짐 -> 살아 움직이는 벽화 -> 처음이면 보상 + 다음 길 안내
    private IEnumerator PlayStory(bool first)
    {
        playing = true;
        Cine.Begin(true);
        CineCamera.Focus(transform.position);
        CineCamera.Zoom(first ? 2.6f : 3.2f, 0.8f);
        if (first) GameAudio.Play("sfx_dream_shimmer", 0.6f, 0.85f);
        else yield return Cine.Say(ThothLines.Replay(fresco));
        yield return Cine.Wait(0.7f);
        yield return MuralStage.Play(ThothMuralScenes.For(fresco));
        CineCamera.Release();
        if (first) yield return AfterFirst();
        Cine.End();
        playing = false;
    }

    private IEnumerator AfterFirst()
    {
        if (fresco == "A")
        {
            yield return Cine.Say(ThothLines.AfterA);
            yield return GrantPopup(ThothIds.Notes);
        }
        else if (fresco == "B") yield return Cine.Say(ThothLines.AfterB);
        else yield return Cine.Say(ThothLines.AfterC);
    }

    public static IEnumerator ShowPopup(string itemId)
    {
        bool closed = false;
        ItemPopup.Show(itemId, () => closed = true);
        while (!closed && !Cine.Skipping) yield return null;
    }

    public static IEnumerator GrantPopup(string itemId)
    {
        bool closed = false;
        ItemPopup.Grant(itemId, () => closed = true);
        while (!closed && !Cine.Skipping) yield return null;
    }
}
