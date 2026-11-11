using System;
using System.Collections;
using UnityEngine;

// [히든] 매의 회랑 안 둥지 - E로 조사하면 둥지 뒤 좁은 길(옆모습 통로 "매의 둥지")로 들어감 -> 끝 방 벽화 컷
// - 회랑(HorusHiddenGallery)이 열려야 둥지가 보이므로, 우자트 부적으로 벽을 연 뒤에만 갈 수 있음
// - 임시 도형(갈색 원)이던 둥지를 도트 그림(HO_Nest)으로 바꿈
// - HiddenVaultSpawner가 씬이 열릴 때 OBJ_HG1_Nest에 자동으로 붙임
public class HorusNestVault : MonoBehaviour, IInteractable
{
    private const string Art = "Horus/PixelFx/HO_Nest";
    private bool busy;

    private void Awake()
    {
        try
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            Sprite nest = Resources.Load<Sprite>(Art);
            if (sr != null && nest != null)
            {
                sr.sprite = nest;
                sr.color = Color.white;
                transform.localScale = Vector3.one * 1.3f;
            }
            if (GetComponent<Collider2D>() != null) return;
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true; // 조사용 (길은 안 막음)
        }
        catch (Exception e)
        {
            Debug.LogError($"[매의 둥지] 준비 오류: {e}");
        }
    }

    public string GetPrompt()
    {
        if (busy || Cine.Running) return null;
        return HiddenVault.Seen(HiddenVault.HorusFlag) ? "E: 둥지 뒤 길로 다시 들어가기" : "E: 매의 둥지 살펴보기";
    }

    public void Interact(GameObject who)
    {
        if (busy || Cine.Running || DialogueBox.IsOpen) return;
        busy = true;
        Cine.Run(Sequence());
    }

    private IEnumerator Sequence()
    {
        Cine.Begin(true);
        Vector3 nest = transform.position;
        if (!HiddenVault.Seen(HiddenVault.HorusFlag))
        {
            yield return Cine.Say("나", "매의 둥지다. 알이 하나... 아직 따뜻해.", "어? 둥지 뒤쪽 벽에 사람 하나 지나갈 틈이 있어.");
        }
        yield return CineShots.Crane(nest + Vector3.down * 1.2f, nest + Vector3.up * 0.4f, 5f, 2.8f, 1.6f); // 아래에서 둥지로 올라가며 다가감
        yield return HiddenVault.Play(HiddenVaultScenes.HorusWalk(), HiddenVaultScenes.HorusReveal(), HiddenVault.HorusFlag, HiddenVaultScenes.HorusAfter);
        Cine.End();
        busy = false;
    }
}
