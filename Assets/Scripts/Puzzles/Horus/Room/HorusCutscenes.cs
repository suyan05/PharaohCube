using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 호루스의 방 연출 장면들 (HorusDirector가 순서를 정해서 부름)
// 쓰는 기법: 위아래 검은 바, 타이틀 자막, 암전, 꿈 화면 효과(스핑크스와 같은 Volume: 일렁임/번짐) + 낮은 울림,
//            눈물/물결/깃털 속에서 날개 펴며 나타나는 이시스의 유령(HorusIsisGhost), 신전을 울리는 호루스의 목소리,
//            매가 하늘 틈에서 뛰어들어 방을 돌고 제단에 앉음(클로즈업 -> 천천히 밀기 -> 뒤로 빠지기),
//            카메라 밀기/당기기/흔들기, 화면 번쩍임, 천장 별 반짝임, 머리 위 "!", 형판이 떠올라 내 손으로, 방 전체가 밝아짐
public static class HorusCutscenes
{
    public const string Intro = "INTRO";
    public const string Isis = "ISIS";
    public const string Awaken = "AWAKEN";
    public const string Ending = "ENDING";
    public const string LapisAll = "F_HO_LAPIS_ALL";

    private const string EmberItem = "ITEM_RA_EMBER";
    private static readonly Vector3 HallCenter = new Vector3(12f, 8f, 0f);
    private static readonly Vector2 HallHalf = new Vector2(11f, 7f);
    private static readonly Color Gold = new Color(1f, 0.82f, 0.4f, 0.9f);
    private static readonly Color StarBlue = new Color(0.65f, 0.8f, 1f, 1f);
    private static readonly Color IsisLight = new Color(0.78f, 0.86f, 1f, 0.8f);

    // 퍼즐을 풀면: 호루스 한마디 (답을 대놓고 말하지 않게) + 매가 다음 목적지로 날아감
    // mural: 매가 날아간 뒤 보여줄 벽화 컷신 (이시스의 기억, HorusMuralScenes). 없으면 null
    private struct Reaction
    {
        public string line;
        public bool flyNext;
        public string mural;
        public Reaction(string line, bool flyNext, string mural = null) { this.line = line; this.flyNext = flyNext; this.mural = mural; }
    }

    private static readonly Dictionary<string, Reaction> Reactions = new Dictionary<string, Reaction>
    {
        { "F_HO_CHART_1", new Reaction("지지 않는 별이 다시 눈을 떴다. 나의 오른눈은 해... 해의 방에서 빛을 나의 눈에 돌려다오.", true) },
        { "F_HO_H1_DONE", new Reaction("해의 빛이 다시 나의 눈에 닿았다... 빛의 조각을 하늘로 돌려보내라.", true) },
        { "F_HO_CHART_2", new Reaction("두 번째 별이 떠올랐다. 찢긴 나의 눈이 석탁 위에서 기다린다.", true, HorusMuralScenes.EyeKey) },
        { "F_HO_H3_DONE", new Reaction("보아라, 나의 눈이 다시 형태를 얻었다. 달의 눈을 하늘 앞에 걸어라.", true) },
        { "F_HO_CHART_3", new Reaction("세 번째 별이 뒤따른다. 나일의 방으로 가라. 세트의 돌배가 너의 다리가 되리라.", true, HorusMuralScenes.RaceKey) },
        { "F_HO_H4_DONE", new Reaction("세트의 돌배는 강바닥에 잠들었다. 꾀로 이긴 자가 강을 건넌다. 마지막 조각을 하늘에 바쳐라.", true) },
        { "F_HO_CHART_4", new Reaction("네 별이 모였다. 제단으로 오라, 인간이여. 해와 달, 매와 왕관... 안에서 밖으로 세워라.", true) },
        { LapisAll, new Reaction("밤하늘의 조각을 모두 모았구나... 매가 너를 인정한다.", false) }
    };

    public static bool HasReaction(string flag) => Reactions.ContainsKey(flag);

    public static IEnumerator For(HorusDirector d, string key)
    {
        if (key == Intro) return IntroScene(d);
        if (key == Isis) return IsisStatueScene(d);
        if (key == Awaken) return AwakenScene(d);
        if (key == Ending) return EndingScene(d);
        return ReactionScene(d, Reactions[key]);
    }

    // ================= 1. 입구 전실: 꿈결 같은 이시스의 목소리 =================
    private static IEnumerator IntroScene(HorusDirector d)
    {
        Cine.Begin(true);
        HorusFx.MatchPlayer();
        Transform p = CineActor.Player();
        ScreenFader.SetBlack();
        yield return TitleCardUI.Show("호루스의 신전", "하늘을 잃은 매", 1.8f);
        yield return ScreenFader.FadeIn(Cine.Skipping ? 0.2f : 1.2f);
        // [수정] 입구 복도가 생김: 걸어 들어가는 대신 카메라가 긴 복도의 찢긴 벽화들을 훑고 돌아옴
        //        (이시스는 들어오자마자 나오지 않음 -> 전실의 여신상을 조사하면 나옴, 라/바스테트처럼)
        yield return CorridorSweep();
        if (p != null) d.StartCoroutine(HorusFx.Emote(p, d.EmoteSprite, 1f));
        // [수정] 최종 스토리(B안): 입장하면 케프리가 이 방이 밤의 몇 시인지 한 줄로 알려 줌
        yield return Cine.Say(Khepri, "밤의 일곱째 시간이다. 매가 별을 따라 배를 이끄는 길, 호루스의 신전이지.");
        yield return Cine.Say("나", "벽마다 찢긴 벽화가 이어져 있어. 하나씩 살펴보자.");
        if (HasItem(EmberItem)) yield return Cine.Say("나", "라의 불씨가 아직 따뜻해. 이 어둠 속에서도 횃불은 꺼지지 않겠어.");
        HorusDirector.SetFlag(d.EnteredFlag);
        CineCamera.Release();
        Cine.End();
    }

    private const string Khepri = "케프리";

    private static Vector3 StatuePos() => Pos("OBJ_HA0_IsisStatue", new Vector3(8.9f, -2.9f, 0f));

    // 복도 서쪽 -> 동쪽으로 카메라가 천천히 훑음 (찢긴 벽화 4점) -> 플레이어에게 돌아옴
    private static IEnumerator CorridorSweep()
    {
        CineCamera.Zoom(4.2f, 0.6f);
        foreach (string n in new[] { "OBJ_HC1_MuralWall", "OBJ_HC2_MuralWall", "OBJ_HC3_MuralWall", "OBJ_HC4_MuralWall" })
        {
            GameObject go = GameObject.Find(n);
            if (go == null) continue;
            CineCamera.Focus(go.transform.position + Vector3.up * 1.2f);
            yield return Cine.Wait(0.9f);
        }
        Transform p = CineActor.Player();
        if (p != null) CineCamera.Focus(p.position + Vector3.up * 0.8f);
        CineCamera.Zoom(3.8f, 0.6f);
        yield return Cine.Wait(0.6f);
    }

    // ================= 1-1. 이시스 석상 조사: 짧게 나타난 이시스 -> 벽화로 보여주는 기억 -> 부탁 =================
    private static IEnumerator IsisStatueScene(HorusDirector d)
    {
        Cine.Begin(true);
        HorusFx.MatchPlayer();
        Vector3 pos = StatuePos() + Vector3.up * 0.3f;
        HorusIsisGhost isis = null;
        yield return IsisRise(d, pos, g => isis = g);
        yield return Cine.Say(IsisBeforeMural());
        yield return MuralStage.Play(HorusMuralScenes.For(HorusMuralScenes.IsisKey));
        CineCamera.Focus(pos + Vector3.up * 1.6f);
        yield return Cine.Say(IsisAfterMural());
        yield return IsisFade(d, isis, pos);
        HorusDirector.SetFlag(d.IntroFlag);
        Cine.End();
    }

    private static IEnumerator IsisRise(HorusDirector d, Vector3 pos, System.Action<HorusIsisGhost> made)
    {
        CineCamera.Focus(pos + Vector3.up * 1.6f);
        CineCamera.Zoom(3.6f, 0.8f);
        yield return DreamIn(d);
        if (d.IsisSprite == null) yield break;
        HorusIsisGhost isis = HorusIsisGhost.Create(pos, d.IsisSprite, d.IsisWing, d.FeatherSprite, d.RippleSprite);
        made(isis);
        if (isis != null) yield return isis.QuickAppear();
    }

    private static IEnumerator IsisFade(HorusDirector d, HorusIsisGhost isis, Vector3 pos)
    {
        CineCamera.Zoom(4.6f, 1.2f);
        if (isis != null) yield return isis.Vanish(pos + Vector3.up * 10f);
        yield return DreamOut(d);
        CineCamera.Release();
    }

    private static DialogueLine[] IsisBeforeMural()
    {
        return new[]
        {
            new DialogueLine("이시스", "...잠든 모래 너머에서, 낯선 숨결이 들리는구나."),
            new DialogueLine("이시스", "나는 이시스. 이 신전에 잠든 아이의 어미다. 내 기억을 보여 주마.")
        };
    }

    private static DialogueLine[] IsisAfterMural()
    {
        return new[]
        {
            new DialogueLine("이시스", "그 아이는 지금도 한쪽 눈을 잃은 채 별을 보지 못한다."),
            new DialogueLine("이시스", "본당 천장의 별을 다시 이어다오. 하늘이 열리면, 아이가 깨어날 것이다."),
            new DialogueLine("나", "...벽화 속 이야기가 진짜였구나. 본당으로 가 보자.")
        };
    }

    // 이시스 유령 그림이 있으면 유령 연출, 없으면 예전 빛 덩어리 연출
    private static IEnumerator IsisSpeaks(HorusDirector d, Vector3 pos)
    {
        if (d.IsisSprite != null) return IsisGhostScene(d, pos);
        return IsisGlowScene(d, pos);
    }

    // 카메라: 살짝 넓게 시작 -> 눈물이 떨어지고 모습이 생기는 동안 천천히 다가감(돌리 인)
    //         -> 말할 땐 얼굴 가까이 -> 사라질 땐 흩어지는 깃털을 따라 위(본당 쪽)로 올라감
    private static IEnumerator IsisGhostScene(HorusDirector d, Vector3 pos)
    {
        Vector3 face = pos + Vector3.up * 2.1f;
        CineCamera.Focus(face);
        CineCamera.Zoom(5.2f);
        yield return DreamIn(d);
        HorusIsisGhost isis = HorusIsisGhost.Create(pos, d.IsisSprite, d.IsisWing, d.FeatherSprite, d.RippleSprite);
        CineCamera.Zoom(3.3f, 2.4f);
        if (isis != null) yield return isis.Appear();
        CineCamera.Focus(face + Vector3.down * 0.5f); // 대사창에 안 가리게 살짝 위에 걸리도록
        yield return Cine.Say(IsisLines());
        CineCamera.Zoom(4.6f, 1.2f);
        CineCamera.Focus(pos + Vector3.up * 3.5f);
        if (isis != null) yield return isis.Vanish(pos + Vector3.up * 10f);
        yield return DreamOut(d);
        CineCamera.Release();
    }

    private static IEnumerator IsisGlowScene(HorusDirector d, Vector3 pos)
    {
        CineCamera.Focus(pos);
        CineCamera.Zoom(4f);
        yield return DreamIn(d);
        SpriteRenderer glow = RaGlowFx.Glow(null, pos, 3f, IsisLight, HorusFx.Layer, HorusFx.Above - 1);
        RaGlowFx.PointLight(glow.transform, Vector3.zero, IsisLight, 4f, 0.9f);
        d.StartCoroutine(Breathe(glow, 3f));
        HorusFx.StarBurst(pos, new Vector2(0.6f, 0.6f), 16, StarBlue);
        yield return Cine.Say(IsisLines());
        HorusFx.StarBurst(pos, new Vector2(0.5f, 0.5f), 12, StarBlue);
        Object.Destroy(glow.gameObject);
        yield return DreamOut(d);
        CineCamera.Release();
    }

    // 여신의 말투: 직접 시키지 않고, 신화와 은유로
    private static DialogueLine[] IsisLines()
    {
        return new[]
        {
            new DialogueLine("이시스", "...잠든 모래 너머에서, 낯선 숨결이 들리는구나."),
            new DialogueLine("이시스", "나는 이시스. 흩어진 남편을 찾아 나일의 끝까지 헤맨 자."),
            new DialogueLine("이시스", "이곳은 내 아들의 하늘. 그 아이는 한쪽 눈을 잃은 뒤로 별을 보지 못한다."),
            new DialogueLine("이시스", "서쪽 벽 너머에서도 바람이 운다... 아들의 눈을 되찾은 자만이 그 길을 걸으리라."),
            new DialogueLine("이시스", "기억은 글에 머물고, 길은 돌 위에 새겨지지... 읽고, 보거라.")
        };
    }

    // 빛 덩어리가 숨 쉬듯 커졌다 작아짐 (지워지면 멈춤)
    private static IEnumerator Breathe(SpriteRenderer glow, float size)
    {
        while (glow != null)
        {
            glow.transform.localScale = Vector3.one * size * (1f + Mathf.Sin(Time.time * 2.2f) * 0.12f);
            yield return null;
        }
    }

    // 스핑크스 꿈과 같은 효과: 꿈 Volume을 서서히 켜고 + 낮은 웅웅 소리 + 화면 출렁
    private static IEnumerator DreamIn(HorusDirector d)
    {
        CineAudio.Loop(GameAudio.Or(null, "sfx_dream_drone_loop"), 0.45f);
        GameAudio.Play("sfx_dream_shimmer", 0.6f);
        DreamFX.Ripple(1.8f, 0.3f);
        yield return Weight(d.DreamVolume, 1f, 1.2f);
        DreamFX.SetHaze(0.25f);
    }

    private static IEnumerator DreamOut(HorusDirector d)
    {
        DreamFX.SetHaze(0f);
        yield return Weight(d.DreamVolume, 0f, 1f);
        yield return CineAudio.StopLoop(1f);
    }

    private static IEnumerator Weight(Volume v, float to, float seconds)
    {
        if (v == null) yield break;
        float from = v.weight, t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            v.weight = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        v.weight = to;
    }

    // ================= 2. 본당 첫 입장: 신전이 울리고 호루스의 매가 날아옴 =================
    // 카메라 콘티: (1) 넓게 - 신전 전체가 울림 -> (2) 하늘 틈으로 휙 - 매가 뛰어듦, 카메라가 앞질러 따라감
    //              -> (3) 넓게 - 매가 방을 크게 돎 -> (4) 제단 위에 착지, 빠른 줌인 클로즈업 + 날개 활짝
    //              -> 말하는 동안 천천히 더 밀기 -> 마지막 대사에 뒤로 쭉 빠져 방 전체 공개 -> (5) 첫 목적지로 출발
    private static IEnumerator AwakenScene(HorusDirector d)
    {
        Cine.Begin(true);
        HorusFx.MatchPlayer();
        yield return AwakenEstablish(d);
        yield return AwakenFalconEntry(d);
        yield return AwakenCloseUp(d);
        yield return AwakenTakeOff(d);
        CineLight.Restore(0.8f);
        HorusDirector.SetFlag(d.AwakenFlag);
        HorusDirector.SetFlag("F_HO_STORY");     // [추가] 최종 스토리: 방 스토리 연출 끝 = 길잡이 동물과 첫 만남
        HorusDirector.SetFlag("F_HO_GUIDE_MET");
        CineCamera.Release();
        Cine.End();
    }

    // (1) 넓은 화면: 땅울림이 점점 세지고 천장에서 금빛 먼지 + 어디선가 들리는 목소리
    private static IEnumerator AwakenEstablish(HorusDirector d)
    {
        CineLight.Dim(0.6f, 0.6f);
        CineCamera.Focus(HallCenter);
        CineCamera.Zoom(7.5f);
        GameAudio.Play("sfx_rumble", 0.7f, 0.7f);
        HorusFx.StarBurst(HallCenter + Vector3.up * 5f, new Vector2(HallHalf.x, 1.2f), 18, Gold);
        yield return CineCamera.Rumble(0.9f, 0.02f, 0.07f);
        GameAudio.Play("sfx_wind_gust", 0.4f, 1.3f); // [수정] 최종 스토리: 천장 별 일부가 꺼져 있고, 멀리서 날갯짓 소리
        yield return Cine.Wait(0.6f);
    }

    // (2)(3) 하늘 틈(오른쪽 위)으로 카메라가 휙 -> 매가 뛰어들어 방 가운데로 -> 넓게 빠지며 방을 한 바퀴
    private static IEnumerator AwakenFalconEntry(HorusDirector d)
    {
        if (d.Falcon == null) yield break;
        Vector3 sky = new Vector3(23f, 16.5f, 0f);
        CineCamera.Focus(sky + new Vector3(-2.5f, -1.5f, 0f));
        CineCamera.Zoom(HorusWorld.SafeZoom(sky + new Vector3(-2.5f, -1.5f, 0f), 4f), 0.2f);
        yield return Cine.Wait(0.45f);
        GameAudio.Play("sfx_wind_gust", 0.8f, 0.9f);
        StartCoroutineSafe(d, ScreenTintUI.Flash(new Color(1f, 0.9f, 0.6f, 0.3f), 0.03f, 0.05f, 0.35f));
        yield return d.Falcon.DiveIn(sky, HallCenter + new Vector3(3f, 1f, 0f));
        CineCamera.Focus(HallCenter + Vector3.up);
        CineCamera.Zoom(7f, 0.6f);
        HorusFx.StarBurst(HallCenter, HallHalf, 40, StarBlue);
        yield return d.Falcon.CircleAround(HallCenter + Vector3.up, 3.2f, 2f, false);
    }

    // (4) 제단 위 착지 -> 빠른 줌인 + 번쩍 + 날개 활짝 -> 말하며 천천히 밀기 -> 마지막 대사에 뒤로 빠짐
    // [수정] 최종 스토리(B안): 호루스 목소리 2줄 -> 매가 매 석상 위에 내려앉음('삐이') -> 케프리
    private static IEnumerator AwakenCloseUp(HorusDirector d)
    {
        Vector3 perch = TopOf("OBJ_H12_FalconStatue_L", new Vector3(9.5f, 12.8f, 0f));
        if (d.Falcon != null) yield return d.Falcon.PerchAt(perch, true);
        CineCamera.Focus(perch + Vector3.down * 0.6f); // 대사창 위쪽 가운데에 매가 오게
        CineCamera.Zoom(2.6f, 0.25f);
        GameAudio.Play("sfx_discover", 0.7f, 0.8f);
        CineCamera.Shake(0.3f, 0.05f);
        if (d.Falcon != null) d.StartCoroutine(d.Falcon.Spread(1.4f));
        yield return ScreenTintUI.Flash(new Color(1f, 0.85f, 0.5f, 0.4f), 0.03f, 0.08f, 0.5f);
        yield return HorusVoice(d, "여기는 밤하늘의 길이다. 이 길은 내가 지킨다. 매가 별을 따라 배를 이끌어야 한다.");
        CineCamera.Zoom(2.2f, 3f);
        yield return HorusVoice(d, "그런데 아펩이 별을 삼켜 길을 지웠다. 나는 그 길을 되찾을 것이다.");
        GameAudio.Play("sfx_discover", 0.6f, 1.4f); // 매 울음 '삐이' (전용 소리가 생기면 교체)
        CineCamera.Zoom(6f, 1f);
        CineCamera.Focus(HallCenter + Vector3.up * 2f);
        HorusFx.StarBurst(HallCenter, HallHalf, 30, StarBlue);
        yield return Cine.Say(Khepri, "매는 하늘의 왕이다. 매의 그림자가 지나가는 곳을 봐라.");
    }

    // (5) 매가 첫 목적지로 출발 (카메라가 따라감)
    private static IEnumerator AwakenTakeOff(HorusDirector d)
    {
        if (d.Falcon == null) yield break;
        CineCamera.Zoom(5f, 0.8f);
        yield return d.Falcon.FlyToCurrent(true);
        HorusWorld.SafeFocus(d.Falcon.Position, 4.2f);
        yield return Cine.Wait(0.7f);
    }

    private static void StartCoroutineSafe(HorusDirector d, IEnumerator routine)
    {
        if (d != null && routine != null) d.StartCoroutine(routine);
    }

    // 호루스가 말할 때: 낮은 울림 + 살짝 흔들림 (위엄)
    private static IEnumerator HorusVoice(HorusDirector d, params string[] lines)
    {
        GameAudio.Play("sfx_sphinx_voice", 0.75f);
        CineCamera.Shake(0.4f, 0.03f);
        yield return Cine.Say(Horus(d, lines));
    }

    // ================= 3. 퍼즐을 풀 때마다: 별 반짝 + 호루스 + 매가 다음 목적지로 =================
    private static IEnumerator ReactionScene(HorusDirector d, Reaction r)
    {
        Cine.Begin(true);
        HorusFx.MatchPlayer();
        GameAudio.Play("sfx_dream_shimmer", 0.6f);
        CineCamera.Shake(0.3f, 0.03f);
        HorusFx.StarBurst(HallCenter, HallHalf, 24, StarBlue);
        yield return ScreenTintUI.Flash(new Color(1f, 0.85f, 0.5f, 0.25f), 0.05f, 0.05f, 0.4f);
        if (r.flyNext && d.Falcon != null)
        {
            CineCamera.Zoom(6f);
            yield return d.Falcon.FlyToCurrent(true);
            HorusWorld.SafeFocus(d.Falcon.Position, 4.2f); // [수정] 맵 끝(동쪽 벽 성도판 등)에서도 매가 화면 안쪽에 오게 가까이
            yield return Cine.Wait(0.5f);
        }
        if (!string.IsNullOrEmpty(r.mural)) yield return MuralStage.Play(HorusMuralScenes.For(r.mural));
        yield return HorusVoice(d, r.line);
        CineCamera.Release();
        Cine.End();
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
        yield return MuralStage.Play(HorusMuralScenes.For(HorusMuralScenes.EndingKey));
        yield return FinaleLines(d);
        yield return PlateToPlayer(plate);
        yield return Cine.Say(Khepri, "하늘이 우리 편이다.");
        yield return DoorOpens();
        HorusDirector.SetFlag(d.EndingFlag);
        HorusDirector.SetFlag("F_HO_FINALE"); // [추가] 최종 스토리: 형판 개수는 이 플래그로 셈
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

    // 방 전체가 밝아지고 별이 쏟아짐 + 매가 방을 크게 돎 + 호루스 (마지막엔 세트에 대한 경고)
    private static IEnumerator SkyReturns(HorusDirector d)
    {
        CineCamera.Focus(HallCenter);
        CineCamera.Zoom(8f);
        d.StartCoroutine(HorusFx.LightTo(d.ClearIntensity, d.ClearColor, 2.5f));
        HorusFx.StarBurst(HallCenter, HallHalf, 60, StarBlue);
        if (d.Falcon != null) d.StartCoroutine(d.Falcon.CircleAround(HallCenter, 4f, 2.6f, false));
        yield return Cine.Wait(2.2f); // [수정] 최종 스토리: 꺼져 있던 천장의 별이 하나씩 켜짐 (대사는 벽화 뒤 FinaleLines)
    }

    // 최종 스토리(B안) 호루스 클리어 대사 3~5 (1~2는 엔딩 벽화 EndThrone)
    // 세트를 라의 배 앞에 세운 이야기 -> 라의 방 벽화 4(뱃머리의 세트) 회수, 지평선의 문지기(스핑크스)도 호루스
    private static IEnumerator FinaleLines(HorusDirector d)
    {
        yield return HorusVoice(d, "지평선에서 너를 맞이한 문지기도 나였다. 세트는 내가 죽이지 않았다. 라의 배 앞을 지키는 자리를 그에게 주었다.");
        yield return HorusVoice(d, "태양의 조각을 가져가라. 심판의 방에서는 매가 네 위를 날 것이다.");
        yield return HorusVoice(d, "강이 마르면 배가 멈춘다. 물의 주인에게 가라.");
    }

    private static IEnumerator PlateToPlayer(SpriteRenderer plate)
    {
        Transform p = CineActor.Player();
        if (p != null) CineCamera.Focus(p.position);
        CineCamera.Zoom(4.5f);
        if (plate != null) yield return HorusFx.MoveInto(plate.transform, p, 0.7f);
        GameAudio.Play("sfx_item_get", 0.8f);
        yield return TitleCardUI.Show("매 형판", "태양의 네 번째 조각을 되찾았다", 1.4f);
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

    // 그 물건 그림의 맨 위 (매가 앉을 자리)
    private static Vector3 TopOf(string objName, Vector3 fallback)
    {
        GameObject go = GameObject.Find(objName);
        SpriteRenderer sr = go != null ? go.GetComponent<SpriteRenderer>() : null;
        if (sr == null && go != null) sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return go != null ? go.transform.position + Vector3.up : fallback;
        return new Vector3(sr.bounds.center.x, sr.bounds.max.y + 0.3f, 0f);
    }

    private static bool HasItem(string id) => ItemInventory.Instance != null && ItemInventory.Instance.Has(id);
}
