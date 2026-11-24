using System;
using System.Collections;
using UnityEngine;

// 숨은 방 입구: 동쪽 서고 벽의 달 모양 홈 세 개 (thoth_Hidden Mural01)
// 달 조각 3개 (복도 책장 / 달 시계 / 동쪽 먼지 구석)를 모두 가지면 E -> 벽이 열리고
// 옆모습 통로를 걸어가 '토트의 책' 벽화 (HiddenVault 공통 흐름) -> 토트의 석판(ITEM_THOTH_TABLET)
// [수정] 진짜 숨김: 세 조각을 다 모으기 전에는 홈도 안 보이고 E 안내도 없음 (평범한 벽)
//        석판은 아누비스 심판에서 한 번 실패를 되돌림 (AnubisSecondChance)
public class ThothHiddenNiche : MonoBehaviour, IInteractable
{
    [SerializeField] private SpriteRenderer[] sockets = new SpriteRenderer[3]; // 홈에 끼운 조각 표시 (빌더가 채움)

    private bool busy;

    private void Update()
    {
        for (int i = 0; i < sockets.Length; i++)
        {
            if (sockets[i] == null) continue;
            bool on = ThothState.Has(ThothIds.Vault) || (Ready && ThothState.HasItem(ThothIds.MoonFrag(i + 1)));
            sockets[i].enabled = on;
            if (on && !ThothState.Has(ThothIds.Vault)) sockets[i].color = new Color(1f, 1f, 1f, 0.35f); // 가졌지만 아직 안 끼움
            else if (on) sockets[i].color = Color.white;
        }
    }

    private static bool Ready => ThothState.MoonFragCount() >= 3;

    public string GetPrompt()
    {
        if (ThothState.Has(ThothIds.Vault)) return "E: 열린 벽 살펴보기";
        return Ready ? "E: 달 모양 홈 살펴보기" : null; // 조각이 모자라면 그냥 벽
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (busy || ThothState.Busy) return;
            if (ThothState.Has(ThothIds.Vault)) Cine.Run(Talk(ThothLines.NicheSeen));
            else if (Ready) Cine.Run(Open());
        }
        catch (Exception e)
        {
            Debug.LogError($"[토트 숨은 방] Interact 오류: {e}");
        }
    }

    private IEnumerator Talk(DialogueLine[] lines)
    {
        busy = true;
        Cine.Begin(false);
        yield return Cine.Say(lines);
        Cine.End();
        busy = false;
    }

    private IEnumerator Open()
    {
        busy = true;
        Cine.Begin(true);
        CineCamera.Focus(transform.position);
        CineCamera.Zoom(3.2f, 0.6f);
        foreach (SpriteRenderer s in sockets) if (s != null) s.color = Color.white;
        GameAudio.Play("sfx_th_chime", 0.7f, 0.9f);
        yield return Cine.Wait(0.6f);
        GameAudio.Play("sfx_rumble", 0.7f);
        CineCamera.Shake(0.8f, 0.12f);
        yield return Cine.Say(ThothLines.NicheOpen);
        yield return HiddenVault.Play(ThothMuralScenes.VaultWalk(), ThothMuralScenes.VaultReveal(), ThothIds.Vault, ThothLines.VaultAfter);
        ThothState.Set(ThothIds.Vault); // 스킵해도 다시 열 수 있게 하지 않음
        yield return ThothFrescoWall.GrantPopup(ThothIds.Tablet);
        CineCamera.Release();
        Cine.End();
        busy = false;
    }
}
