using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 호루스의 방 연출 장면들 (HorusDirector가 순서를 정해서 부름)
// 쓰는 기법: 위아래 검은 바, 타이틀 자막, 암전, 카메라 밀기/당기기/흔들기, 화면 번쩍임,
//            빛 덩어리(이시스), 바닥을 가로지르는 거대한 매 그림자, 천장 별 반짝임, 머리 위 "!" 말풍선,
//            형판이 떠올라 내 손으로 빨려 들어옴, 방 전체 밝기 변화(하늘을 되찾음)
public static class HorusCutscenes
{
    public const string Intro = "INTRO";
    public const string Awaken = "AWAKEN";
    public const string Ending = "ENDING";

    private static readonly Vector3 HallCenter = new Vector3(12f, 8f, 0f);
    private static readonly Vector2 HallHalf = new Vector2(11f, 7f);
    private static readonly Color Gold = new Color(1f, 0.82f, 0.4f, 0.9f);
    private static readonly Color StarBlue = new Color(0.65f, 0.8f, 1f, 1f);
    private static readonly Color IsisLight = new Color(0.75f, 0.85f, 1f, 0.85f);
    private static readonly Color Shadow = new Color(0f, 0f, 0f, 0.45f);

    // 퍼즐을 풀면: 호루스 한마디 + 다음 목적지 (기획서 4.2 플래그 순서)
    private struct Reaction
    {
        public string line;
        public string next;
        public Reaction(string line, string next) { this.line = line; this.next = next; }
    }

    private static readonly Dictionary<string, Reaction> Reactions = new Dictionary<string, Reaction>
    {
        { "F_HO_CHART_1", new Reaction("메스케티우가 깨어났다. 서쪽 관측대로 가라. 반쪽 날개로는 날 수 없다.", "H1_MirrorWing") },
        { "F_HO_H1_DONE", new Reaction("날개가 온전해졌구나. 얻은 별 조각을 성도판의 꺼진 별에 끼워라.", "H2_ConstellationChart") },
        { "F_HO_CHART_2", new Reaction("사흐가 떠올랐다. 은빛 실로 찢긴 눈을 이어라.", "H3_WedjatRestore") },
        { "F_HO_H3_DONE", new Reaction("찢긴 눈도 다시 하늘을 본다. 달의 눈을 성도판 앞에 걸어라.", "OBJ_H04_MoonEyeMount") },
        { "F_HO_CHART_3", new Reaction("소프데트가 뒤따른다. 매 인장으로 세트의 봉인을 풀어라.", "H4_StarPathLock") },
        { "F_HO_H4_DONE", new Reaction("매는 동쪽에서 떠올라 중심에 앉는다. 마지막 별 조각을 성도판에 끼워라.", "H2_ConstellationChart") },
        { "F_HO_CHART_4", new Reaction("네 별자리가 모두 빛난다. 아이여, 제단으로 오라.", "H5_SkyAlign") }
    };

    public static bool HasReaction(string flag) => Reactions.ContainsKey(flag);

    public static IEnumerator For(HorusDirector d, string key)
    {
        if (key == Intro) return IntroScene(d);
        if (key == Awaken) return AwakenScene(d);
        if (key == Ending) return EndingScene(d);
        return ReactionScene(d, Reactions[key]);
    }

    // ================= 1. 입구 전실: 이시스의 목소리 =================
    private static IEnumerator IntroScene(HorusDirector d)
    {
        Cine.Begin(true);
        HorusFx.MatchPlayer();
        Transform p = CineActor.Player();
        ScreenFader.SetBlack();
        yield return TitleCardUI.Show("호루스의 신전", "하늘을 잃은 매의 방", 1.8f);
        yield return ScreenFader.FadeIn(Cine.Skipping ? 0.2f : 1.2f);
        if (p != null) yield return CineActor.WalkTo(p, new Vector2(12f, -5.6f), 2.2f);
        yield return IsisSpeaks(d, new Vector3(12f, -3.6f, 0f));
        if (p != null) d.StartCoroutine(HorusFx.Emote(p, d.EmoteSprite, 1f));
        yield return Cine.Say("나", "방금... 누구 목소리였지? 이시스라고?");
        HorusDirector.SetFlag(d.IntroFlag);
        Cine.End();
    }

    // 모습 없이 빛 덩어리로만 나타나는 이시스 (스토리 기획서: 아트가 없으면 목소리 + 빛)
    private static IEnumerator IsisSpeaks(HorusDirector d, Vector3 pos)
    {
        CineCamera.Focus(pos);
        CineCamera.Zoom(4f);
        GameAudio.Play("sfx_dream_shimmer", 0.6f);
        SpriteRenderer glow = RaGlowFx.Glow(null, pos, 2.2f, IsisLight, HorusFx.Layer, HorusFx.Above - 1);
        RaGlowFx.PointLight(glow.transform, Vector3.zero, IsisLight, 3.5f, 0.9f);
        HorusFx.StarBurst(pos, new Vector2(0.5f, 0.5f), 14, StarBlue);
        yield return Cine.Say(IsisLines());
        HorusFx.StarBurst(pos, new Vector2(0.4f, 0.4f), 10, StarBlue);
        Object.Destroy(glow.gameObject);
        CineCamera.Release();
    }

    private static DialogueLine[] IsisLines()
    {
        return new[]
        {
            new DialogueLine("이시스", "...들리니, 낯선 이여. 나는 이시스."),
            new DialogueLine("이시스", "이곳은 내 아들 호루스의 신전. 내 아들은 눈을 잃고도 싸움을 멈추지 않았다."),
            new DialogueLine("이시스", "옆의 책을 읽어 보렴. 그 아이가 왜 하늘을 잃었는지 알게 될 거야."),
            new DialogueLine("이시스", "그리고 벽에 남은 그림도 잘 보렴. 매가 날아간 길이 새겨져 있단다.")
        };
    }

    // ================= 2. 본당 첫 입장: 호루스가 깨어남 =================
    private static IEnumerator AwakenScene(HorusDirector d)
    {
        Cine.Begin(true);
        HorusFx.MatchPlayer();
        CineLight.Dim(0.5f, 0.6f);
        CineCamera.Focus(HallCenter);
        CineCamera.Zoom(9.5f);
        yield return Cine.Wait(1f);
        yield return ShadowSweep(d);
        HorusFx.StarBurst(HallCenter, HallHalf, 40, StarBlue);
        yield return Cine.Say(Horus(d, "하늘을 잃은 아이여...", "나의 눈을 되찾아 다오."));
        CineLight.Restore(0.8f);
        yield return ShowNext(d, "H2_ConstellationChart", "동쪽 벽의 성도판에서 별을 이어라. 첫 별자리가 길을 열 것이다.");
        HorusDirector.SetFlag(d.AwakenFlag);
        CineCamera.Release();
        Cine.End();
    }

    // 거대한 매 그림자가 바닥을 동쪽에서 서쪽으로 쓸고 지나감 + 바람 + 흔들림
    private static IEnumerator ShadowSweep(HorusDirector d)
    {
        GameAudio.Play("sfx_wind_gust", 0.8f);
        CineCamera.Shake(0.7f, 0.05f);
        d.StartCoroutine(HorusFx.Fly(d.FalconSprite, new Vector3(27f, 12f, 0f), new Vector3(-3f, 5f, 0f), 2.4f, 7f, Shadow, HorusFx.Floor, 0f, false));
        yield return Cine.Wait(1.6f);
    }

    // ================= 3. 퍼즐을 풀 때마다 =================
    private static IEnumerator ReactionScene(HorusDirector d, Reaction r)
    {
        Cine.Begin(true);
        HorusFx.MatchPlayer();
        GameAudio.Play("sfx_dream_shimmer", 0.6f);
        CineCamera.Shake(0.3f, 0.03f);
        HorusFx.StarBurst(HallCenter, HallHalf, 24, StarBlue);
        yield return ScreenTintUI.Flash(new Color(1f, 0.85f, 0.5f, 0.25f), 0.05f, 0.05f, 0.4f);
        yield return ShowNext(d, r.next, r.line);
        CineCamera.Release();
        Cine.End();
    }

    // 다음 목적지를 카메라로 비추고 빛나게 한 뒤 호루스가 말함
    private static IEnumerator ShowNext(HorusDirector d, string objName, string line)
    {
        GameObject go = GameObject.Find(objName);
        if (go != null)
        {
            CineCamera.Focus(go.transform.position);
            CineCamera.Zoom(4.5f);
            d.StartCoroutine(HorusFx.Pulse(go.transform.position, 2.2f, Gold, 2.4f));
            yield return Cine.Wait(0.6f);
        }
        yield return Cine.Say(Horus(d, line));
    }

    // ================= 4. H5 클리어: 하늘을 되찾음 =================
    private static IEnumerator EndingScene(HorusDirector d)
    {
        Cine.Begin(true);
        HorusFx.MatchPlayer();
        Vector3 altar = Pos("H5_SkyAlign", new Vector3(12f, 12.3f, 0f));
        CineCamera.Focus(altar);
        CineCamera.Zoom(4.2f);
        yield return Cine.Wait(0.5f);
        SpriteRenderer plate = null;
        yield return PlateRises(d, altar, sr => plate = sr);
        yield return SkyReturns(d);
        yield return PlateToPlayer(plate);
        yield return DoorOpens();
        HorusDirector.SetFlag(d.EndingFlag);
        CineCamera.Release();
        Cine.End();
    }

    private static IEnumerator PlateRises(HorusDirector d, Vector3 altar, System.Action<SpriteRenderer> made)
    {
        GameAudio.Play("sfx_dream_shimmer", 0.9f);
        yield return CineCamera.Rumble(1f, 0.02f, 0.12f);
        HorusFx.StarBurst(altar, new Vector2(1.5f, 1.2f), 30, Gold);
        yield return ScreenTintUI.Flash(new Color(1f, 1f, 1f, 0.7f), 0.05f, 0.15f, 0.8f);
        SpriteRenderer plate = HorusFx.Spawn("RisingPlate", d.PlateSprite, altar, 1.2f, Color.white, HorusFx.Above);
        made(plate);
        d.StartCoroutine(HorusFx.Pulse(altar + Vector3.up * 1.4f, 2.6f, Gold, 3f));
        yield return HorusFx.Rise(plate, 1.4f, 1.6f);
    }

    // 방 전체가 밝아지고 별이 쏟아짐 + 매 그림자 한 번 더 + 호루스
    private static IEnumerator SkyReturns(HorusDirector d)
    {
        CineCamera.Focus(HallCenter);
        CineCamera.Zoom(9.5f);
        d.StartCoroutine(HorusFx.LightTo(d.ClearIntensity, d.ClearColor, 2.5f));
        HorusFx.StarBurst(HallCenter, HallHalf, 60, StarBlue);
        yield return ShadowSweep(d);
        yield return Cine.Say(Horus(d, "보아라... 하늘이 다시 빛난다.", "이제 하늘은 다시 나의 것이다!"));
        yield return Cine.Say(Horus(d, "고맙다, 아이여. 그 형판을 큐브의 면에 끼워라.", "아버지 오시리스께서 다시 눈을 뜨실 것이다."));
    }

    private static IEnumerator PlateToPlayer(SpriteRenderer plate)
    {
        Transform p = CineActor.Player();
        if (p != null) CineCamera.Focus(p.position);
        CineCamera.Zoom(4.5f);
        if (plate != null) yield return HorusFx.MoveInto(plate.transform, p, 0.7f);
        GameAudio.Play("sfx_item_get", 0.8f);
        yield return TitleCardUI.Show("하늘의 형판", "호루스의 시험을 마쳤다", 1.4f);
    }

    private static IEnumerator DoorOpens()
    {
        Vector3 door = Pos("OBJ_H13_NorthDoor", new Vector3(12f, 15.7f, 0f));
        CineCamera.Focus(door);
        CineCamera.Zoom(4.5f);
        UnlockRumble.Play();
        yield return CineCamera.Rumble(0.8f, 0.03f, 0.1f);
        HorusFx.StarBurst(door, new Vector2(1f, 0.5f), 16, Gold);
        yield return Cine.Say("나", "북쪽 문이 열렸어. 중앙 홀로 돌아가자.");
    }

    // ================= 도우미 =================
    private static DialogueLine[] Horus(HorusDirector d, params string[] texts)
    {
        var lines = new DialogueLine[texts.Length];
        for (int i = 0; i < texts.Length; i++) lines[i] = new DialogueLine("호루스", texts[i]) { portrait = d.HorusPortrait };
        return lines;
    }

    private static Vector3 Pos(string objName, Vector3 fallback)
    {
        GameObject go = GameObject.Find(objName);
        return go != null ? go.transform.position : fallback;
    }
}
