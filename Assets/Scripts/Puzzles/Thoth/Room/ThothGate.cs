using System;
using System.Collections;
using UnityEngine;

// 지혜의 문 (홀 북쪽 벽 가운데) - 주문을 다 읊으면 열림 (여는 연출은 ThothDirector)
// 열린 뒤 E: 문으로 걸어 들어가며 화면이 어두워짐 -> 중앙 홀
public class ThothGate : MonoBehaviour, IInteractable
{
    [SerializeField] private SpriteRenderer art;
    [SerializeField] private string hallScene = ThothIds.HallScene;

    private bool used, busy;

    private bool Opened => ThothState.Has(ThothIds.SpellDone);

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (art == null) return;
        Sprite s = ThothState.Art(Opened ? "World/TH_Gate_Open" : "World/TH_Gate_Closed");
        if (s != null) art.sprite = s;
    }

    public string GetPrompt() => ThothState.Has(ThothIds.Done) ? "E: 중앙 홀로 돌아가기" : "E: 지혜의 문 살펴보기";

    public void Interact(GameObject who)
    {
        try
        {
            if (used || busy || ThothState.Busy) return;
            if (ThothState.Has(ThothIds.Done)) Cine.Run(Leave(who != null ? who.transform : CineActor.Player()));
            else Cine.Run(Talk());
        }
        catch (Exception e)
        {
            Debug.LogError($"[지혜의 문] Interact 오류: {e}");
        }
    }

    private IEnumerator Talk()
    {
        busy = true;
        Cine.Begin(false);
        yield return Cine.Say(ThothLines.GateClosed);
        Cine.End();
        busy = false;
    }

    private IEnumerator Leave(Transform player)
    {
        used = true;
        Cine.Begin(false);
        if (player != null) yield return CineActor.WalkTo(player, (Vector2)transform.position + Vector2.down * 0.55f, 2f);
        if (player != null) Cine.Run(CineActor.Fade(player, 1f, 0f, 0.6f));
        yield return ScreenFader.FadeOut(0.9f);
        try
        {
            SceneFlow.LoadByNameOrNext(hallScene);
        }
        catch (Exception e)
        {
            Debug.LogError($"[지혜의 문] 씬 이동 오류: {e}");
            Cine.End();
        }
    }
}
