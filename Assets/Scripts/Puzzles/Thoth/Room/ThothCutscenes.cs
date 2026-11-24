using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 토트의 방 큰 연출 3개 (ThothDirector가 차례로 부름)
public static class ThothCutscenes
{
    // ================= 처음 들어옴 =================
    // [수정] 플레이어가 있는 곳에서 시작 -> 제목 -> 천창 달빛(달 시계)을 보여 주고 -> 다시 플레이어로
    public static IEnumerator Intro(ThothDirector d)
    {
        Cine.Begin(true);
        Transform player = CineActor.Player();
        Vector3 me = (player != null ? player.position : d.Skylight.position) + Vector3.up * 0.6f;
        Vector3 sky = d.Skylight.position;
        CineCamera.Focus(me);
        CineCamera.Zoom(4.5f, 0.05f);
        CineCamera.SnapZoom();
        Cine.Run(TitleCardUI.Show("토트의 방", "달의 서고 - 밤의 여섯째 시간", 2.2f));
        yield return Cine.Wait(2.6f);
        GameAudio.Play("sfx_dream_shimmer", 0.5f, 0.7f);
        yield return CineShots.Crane(me, sky, 4.5f, 6.5f, 2.4f);
        yield return Cine.Say(ThothLines.Intro);
        yield return CineShots.Crane(sky, me, 6.5f, 5f, 1.8f);
        yield return Cine.Say(ThothLines.IntroAfter);
        ThothState.Set(ThothIds.Intro);
        CineCamera.Release();
        Cine.End();
    }

    // ================= 달 시계를 맞춤 -> 달빛 + 벽감이 열림 =================
    public static IEnumerator Reveal(ThothDirector d)
    {
        Cine.Begin(true);
        CineCamera.Focus(d.Skylight.position);
        CineCamera.Zoom(5f, 0.8f);
        GameAudio.Play("sfx_dream_shimmer", 0.7f, 1f);
        yield return Cine.Wait(0.6f);
        for (float t = 0f; t < 1.6f && !Cine.Skipping && !Cine.Hurry; t += Time.deltaTime)
        {
            d.SetBeam(t / 1.6f);
            yield return null;
        }
        d.SetBeam(1f);
        yield return LiftSlab(d);
        ThothState.Set(ThothIds.RevealDone);
        CineCamera.Focus(d.Skylight.position);
        yield return Cine.Wait(0.6f);
        yield return Cine.Say(ThothLines.DialReveal);
        // [수정] 달 조각(2)은 여기서 주지 않음 -> 다 맞춘 달 시계를 다시 들여다봐야 나옴 (숨은 방을 진짜 숨김)
        CineCamera.Release();
        Cine.End();
    }

    private static IEnumerator LiftSlab(ThothDirector d)
    {
        if (d.Slab == null) yield break;
        CineCamera.Focus(d.Slab.position);
        yield return Cine.Wait(0.5f);
        GameAudio.Play("sfx_th_stone", 0.9f);
        GameAudio.Play("sfx_rumble", 0.6f);
        CineCamera.Shake(1.2f, 0.1f);
        SpriteRenderer sr = d.Slab.GetComponentInChildren<SpriteRenderer>();
        Vector3 from = d.Slab.position;
        for (float t = 0f; t < 1.4f && !Cine.Skipping && !Cine.Hurry; t += Time.deltaTime)
        {
            float u = t / 1.4f;
            d.Slab.position = from + Vector3.up * (u * 1.6f);
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f - u);
            yield return null;
        }
        d.OpenSlabNow();
    }

    // ================= 주문 완성 -> 문이 열리고 토트 =================
    public static IEnumerator Finale(ThothDirector d)
    {
        Cine.Begin(true);
        if (d.Gate != null) CineCamera.Focus(d.Gate.transform.position);
        CineCamera.Zoom(4f, 0.8f);
        yield return Cine.Wait(0.6f);
        GameAudio.Play("sfx_rumble", 0.8f);
        CineCamera.Shake(1.4f, 0.14f);
        yield return Cine.Wait(0.8f);
        if (d.Gate != null) d.Gate.Refresh();
        GameAudio.Play("sfx_th_chime", 0.9f, 0.7f);
        yield return Cine.Say(ThothLines.FinaleOpen);
        yield return ShowThoth();
        yield return ThothFrescoWall.GrantPopup(ThothIds.Plate);
        yield return ThothFrescoWall.GrantPopup(ThothIds.Amulet);
        ThothState.Set(ThothIds.Done);
        d.SetGlobal(d.GlobalBright);
        yield return Cine.Say(ThothLines.FinaleAfter);
        CineCamera.Release();
        Cine.End();
    }

    // 토트 일러스트를 화면 가득 띄우고 대사 (대사창은 그 위에 뜸)
    private static IEnumerator ShowThoth()
    {
        RectTransform root = UIBuild.CreateCanvas(null, "ThothIllust", 92, out GameObject go);
        Image black = UIBuild.Stretch(root, "Black").gameObject.AddComponent<Image>();
        black.color = new Color(0f, 0f, 0f, 0f);
        Image art = UIBuild.Stretch(root, "Art").gameObject.AddComponent<Image>();
        Sprite god = GodIllustration.Get(2); // [수정] 공용 신 일러스트 (Illustration/Thoth.png) 우선
        art.sprite = god != null ? god : ThothState.Art("UI/UI_Illust");
        art.preserveAspect = true;
        art.color = new Color(1f, 1f, 1f, 0f);
        GameAudio.Play("sfx_dream_shimmer", 0.8f, 0.6f);
        yield return FadeIllust(black, art, 0f, 1f, 1f);
        yield return Cine.Say(ThothLines.FinaleThoth);
        yield return FadeIllust(black, art, 1f, 0f, 0.8f);
        Object.Destroy(go);
    }

    private static IEnumerator FadeIllust(Image black, Image art, float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds && !Cine.Skipping && !Cine.Hurry; t += Time.deltaTime)
        {
            float a = Mathf.Lerp(from, to, t / seconds);
            black.color = new Color(0f, 0f, 0f, a * 0.9f);
            art.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        black.color = new Color(0f, 0f, 0f, to * 0.9f);
        art.color = new Color(1f, 1f, 1f, to);
    }
}
