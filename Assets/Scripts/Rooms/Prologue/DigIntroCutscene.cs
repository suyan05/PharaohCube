using System;
using System.Collections;
using UnityEngine;

// 프롤로그 1 오프닝 연출 (DigSiteDirector가 씬 시작 때 부름)
//
// 1. 검은 화면에 자막 "기자 고원 남쪽, 발굴 캠프 / 발굴 47일째" + 바람 소리
// 2. 캠프 전체를 넓게 보여주다가 천천히 텐트로 다가감
// 3. 텐트에서 나옴 -> "..." 한숨 -> 모래바람이 쓸고 감 -> 콜록 -> 몇 걸음 걷고 한탄
// 4. 무전기가 치직거리며 덜덜 -> "!" -> 무전기로 다가가 통화 (탐사대장: B구역에 가보라)
// 5. 카메라가 B구역(로프 친 곳)을 비춰줌 -> 다시 플레이어 -> 조작 시작
// ESC 1초 = 건너뛰기. 대사는 전부 Inspector에서 바꿀 수 있음
public class DigIntroCutscene : MonoBehaviour
{
    [Header("자막")]
    [SerializeField] private string title = "기자 고원 남쪽, 발굴 캠프";
    [SerializeField] private string subtitle = "발굴 47일째";
    [SerializeField, Min(0f)] private float titleSeconds = 2.2f;

    [Header("위치 (월드 좌표, 맵 왼쪽 아래 = 0, 0)")]
    [SerializeField] private Vector2 tentDoor = new Vector2(4f, 7.4f);
    [SerializeField] private Vector2 tentFront = new Vector2(4f, 6.3f);
    [SerializeField] private Vector2 lamentSpot = new Vector2(5.6f, 6.1f);
    [SerializeField] private Vector2 radioSpot = new Vector2(3.4f, 4.5f);
    [SerializeField] private Vector2 zoneView = new Vector2(22f, 5f);
    [SerializeField] private Transform radio;

    [Header("카메라")]
    [SerializeField, Min(1f)] private float closeSize = 4.5f;
    [SerializeField, Min(1f)] private float zoneSize = 5f;

    [Header("효과 (모래 알갱이 그림, 소리는 비워도 됨)")]
    [SerializeField] private Sprite[] dustSprites;
    [SerializeField] private AudioClip windLoop;
    [SerializeField] private AudioClip gustSfx;
    [SerializeField] private AudioClip radioStaticSfx;

    [Header("대사")]
    [SerializeField] private DialogueLine[] sighLines = { new DialogueLine("나", "후우...") };
    [SerializeField] private DialogueLine[] gustLines = { new DialogueLine("나", "콜록, 콜록...! 모래바람 한번 지독하네.") };
    [SerializeField] private DialogueLine[] lamentLines =
    {
        new DialogueLine("나", "벌써 47일째... 오늘도 토기 조각이나 줍다 끝나겠지."),
        new DialogueLine("나", "'여섯 신의 신전'이라니. 정말 이 모래 밑에 있긴 한 걸까.")
    };
    [SerializeField] private DialogueLine[] staticLines = { new DialogueLine("무전기", "치직... 치지직...") };
    [SerializeField] private DialogueLine[] radioLines =
    {
        new DialogueLine("무전 (탐사대장)", "...캠프, 들리나? 여기 본부다."),
        new DialogueLine("나", "네, 들립니다!"),
        new DialogueLine("무전 (탐사대장)", "어젯밤 모래폭풍에 남동쪽 B구역 표층이 쓸려나갔다. 뭔가 드러났을지도 몰라."),
        new DialogueLine("무전 (탐사대장)", "어제 일지 다시 확인하고, 붓 챙겨서 가봐. 조심하고. 치직..."),
        new DialogueLine("나", "B구역... 알겠습니다!")
    };
    [SerializeField] private DialogueLine[] zoneLines = { new DialogueLine("나", "로프 쳐둔 저기가 B구역이지. 일지부터 보고, 장비 상자에서 붓을 챙기자.") };

    private Transform player;

    // ================= 시작 =================
    public IEnumerator Play()
    {
        player = CineActor.Player();
        Cine.Begin(true);
        Prepare();

        yield return TitleCardUI.Show(title, subtitle, titleSeconds);
        yield return ScreenFader.FadeIn(Cine.Skipping ? 0.2f : 1.5f);
        yield return Cine.Wait(1.2f);

        yield return ExitTent();
        yield return Lament();
        yield return RadioCall();
        yield return ShowZone();
        Finish();
    }

    // 플레이어는 텐트 입구에 숨겨두고, 카메라는 캠프 전체를 넓게 (검은 화면 뒤에서 미리 이동)
    private void Prepare()
    {
        try
        {
            ScreenFader.SetBlack();
            CineAudio.Loop(GameAudio.Or(windLoop, "sfx_wind_loop"), 0.35f);
            if (player != null) CineActor.Place(player, player.GetComponent<Rigidbody2D>(), tentDoor);
            CineActor.SetAlpha(player, 0f);

            CineCamera.Focus(CineCamera.MapCenter(new Vector3(14f, 7f, 0f)));
            CineCamera.Zoom(CineCamera.WideSize(0.3f, 8f));
            CineCamera.SnapZoom();
        }
        catch (Exception e)
        {
            Debug.LogError($"[DigIntroCutscene] 준비 오류: {e}");
        }
    }

    // ================= 장면들 =================
    private IEnumerator ExitTent()
    {
        CineCamera.Focus(tentFront + Vector2.up * 0.8f);
        CineCamera.Zoom(closeSize, 2.5f);
        yield return Cine.Wait(2.2f);

        Cine.Run(CineActor.Fade(player, 0f, 1f, 0.5f));
        yield return CineActor.WalkTo(player, tentFront, 1.6f);
        yield return Cine.Wait(0.5f);
        yield return EmoteBalloonUI.Show(player, "...", 1.2f);
        yield return Cine.Say(sighLines);
    }

    private IEnumerator Lament()
    {
        CineAudio.Play(GameAudio.Or(gustSfx, "sfx_wind_gust"));
        Cine.Run(CineFX.Wind(dustSprites, 1.8f, 40f));
        CineFX.Puff(dustSprites, player != null ? player.position : (Vector3)tentFront, 8);
        CineCamera.Shake(0.4f, 0.04f);
        yield return Cine.Wait(0.4f);
        yield return EmoteBalloonUI.Show(player, "!?", 0.8f);
        yield return Cine.Say(gustLines);

        yield return CineActor.WalkTo(player, lamentSpot, 1.4f);
        yield return Cine.Wait(0.4f);
        yield return EmoteBalloonUI.Show(player, "...", 1f);
        yield return Cine.Say(lamentLines);
    }

    private IEnumerator RadioCall()
    {
        CineAudio.Play(GameAudio.Or(radioStaticSfx, "sfx_radio_static"));
        Cine.Run(CineActor.Jitter(radio, 1.4f, 0.05f));
        yield return EmoteBalloonUI.Show(player, "!", 0.7f);
        if (radio != null) CineCamera.Focus(radio.position + Vector3.up);
        yield return Cine.Say(staticLines);

        yield return CineActor.WalkTo(player, radioSpot, 3f);
        yield return Cine.Wait(0.3f);
        yield return Cine.Say(radioLines);
    }

    private IEnumerator ShowZone()
    {
        CineCamera.Focus(zoneView);
        CineCamera.Zoom(zoneSize, 1.5f);
        yield return Cine.Wait(2f);
        yield return Cine.Say(zoneLines);
        yield return Cine.Wait(0.3f);
    }

    // 스킵했어도 최종 상태는 같게 (보이게, 바람은 작게)
    private void Finish()
    {
        CineActor.SetAlpha(player, 1f);
        CineAudio.SetLoopVolume(0.12f); // 플레이 중에는 아주 작게 (배경음악 밑에 깔리는 정도)
        Cine.End();
    }
}
