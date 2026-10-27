using System;
using System.Collections;
using UnityEngine;

// 프롤로그 2 시작 연출 (기획서 3.1의 1~3) - 1인칭으로 눈 뜨고 -> 꿈 물결 -> 3인칭으로 바뀜
//
// [1인칭] 플레이어는 안 보임. 카메라 = 내 눈
//   1. 검은 화면에 목소리 "...일어나거라."
//   2. 눈꺼풀이 열렸다 감겼다. 처음엔 뿌옇고 번지다가 깜빡일수록 또렷해짐 (DreamFX)
//   3. 눈앞에 거대한 스핑크스 얼굴. 눈이 금빛으로 빛나며 말함
// [전환] 화면이 물결치며 보랏빛으로 번쩍 -> 카메라가 뒤로 빠짐
// [3인칭] 4. 빛 알갱이 속에서 플레이어가 서서히 나타남 -> 두리번 -> 혼잣말
//         5. 위를 올려다봄 -> 카메라가 스핑크스까지 넓어짐 -> "!" -> 스핑크스 대사
// ESC 1초 = 건너뛰기. SphinxDreamDirector가 씬 시작 때 부름
public class SphinxWakeCutscene : MonoBehaviour
{
    [SerializeField] private SphinxHead head;
    [SerializeField] private Vector2 wakePoint = new Vector2(4.5f, 2.5f);

    [Header("1인칭 카메라 (스핑크스 얼굴)")]
    [SerializeField] private Vector2 povFocus = new Vector2(11f, 10.4f);
    [SerializeField, Min(1f)] private float povSize = 3.2f;

    [Header("3인칭 카메라")]
    [SerializeField, Min(1f)] private float playerSize = 4f;
    [SerializeField, Min(1f)] private float wideSize = 7f;

    [Header("효과 (비워도 됨)")]
    [SerializeField] private Sprite[] sparkleSprites;
    [Tooltip("BGM_PROLOGUE_SPHINX (낮게 울리는 드론)")]
    [SerializeField] private AudioClip droneLoop;
    [SerializeField] private AudioClip shimmerSfx;
    [SerializeField] private Color flashColor = new Color(0.86f, 0.8f, 1f, 0.85f);

    [Header("대사")]
    [SerializeField] private SphinxWakeLines lines = new SphinxWakeLines();

    private Transform player;

    public IEnumerator Play()
    {
        player = CineActor.Player();
        Cine.Begin(true);
        Prepare();

        yield return TitleCardUI.Show(lines.voice, "", 1.6f);
        yield return OpenEyes();
        yield return FirstPersonTalk();
        yield return ToThirdPerson();
        yield return LookAround();
        yield return MeetSphinx();
        Finish();
    }

    // 검은 화면 뒤에서 미리: 눈 감김, 플레이어 숨김, 뿌연 상태, 카메라 = 스핑크스 얼굴 가까이
    private void Prepare()
    {
        try
        {
            ScreenFader.SetBlack();
            EyelidUI.SetClosed();
            CineAudio.Loop(GameAudio.Or(droneLoop, "sfx_dream_drone_loop"), 0.6f);
            DreamFX.SetHaze(1f);

            if (player != null) CineActor.Place(player, player.GetComponent<Rigidbody2D>(), wakePoint);
            CineActor.SetAlpha(player, 0f);
            CineCamera.Focus(povFocus);
            CineCamera.Zoom(povSize);
            CineCamera.SnapZoom();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SphinxWakeCutscene] 준비 오류: {e}");
        }
    }

    // ================= 1인칭 =================
    // 깜빡일 때마다 조금씩 또렷해짐
    private IEnumerator OpenEyes()
    {
        yield return ScreenFader.FadeIn(0.01f);
        yield return Cine.Wait(0.5f);
        yield return EyelidUI.OpenTo(0.25f, 0.9f);
        yield return EyelidUI.OpenTo(0f, 0.3f);
        yield return Cine.Wait(0.4f);

        Cine.Run(DreamFX.HazeTo(0.55f, 1.2f));
        yield return EyelidUI.OpenTo(0.6f, 0.8f);
        yield return EyelidUI.OpenTo(0.08f, 0.2f);

        Cine.Run(DreamFX.HazeTo(0.15f, 1.6f));
        yield return EyelidUI.OpenTo(1f, 1.1f);
        EyelidUI.Hide();
    }

    private IEnumerator FirstPersonTalk()
    {
        yield return Cine.Wait(0.6f);
        yield return Cine.Say(lines.blurry);
        if (head != null) yield return head.Say(lines.povSphinx);
        else yield return Cine.Say(lines.povSphinx);
        yield return Cine.Say(lines.povReaction);
    }

    // ================= 전환 =================
    private IEnumerator ToThirdPerson()
    {
        CineAudio.Play(GameAudio.Or(shimmerSfx, "sfx_dream_shimmer"));
        DreamFX.Ripple(1.6f, 0.45f);
        Cine.Run(ScreenTintUI.Flash(flashColor, 0.45f, 0.35f, 0.9f));
        yield return Cine.Wait(0.5f);

        // 번쩍인 동안 카메라를 플레이어 쪽으로
        CineCamera.Focus(wakePoint + Vector2.up * 0.8f);
        CineCamera.Zoom(playerSize, 0.6f);
        Cine.Run(DreamFX.HazeTo(0f, 1.5f));
        yield return Cine.Wait(0.6f);

        CineFX.Sparkle(sparkleSprites, (Vector3)wakePoint + Vector3.up * 0.7f, 16);
        yield return CineActor.Fade(player, 0f, 1f, 1f);
    }

    // ================= 3인칭 =================
    private IEnumerator LookAround()
    {
        yield return Cine.Wait(0.4f);
        yield return EmoteBalloonUI.Show(player, "...", 0.9f);
        yield return CineActor.Look(player, Vector2.left);
        yield return Cine.Wait(0.6f);
        yield return CineActor.Look(player, Vector2.right);
        yield return Cine.Wait(0.6f);
        yield return CineActor.Look(player, Vector2.down);
        yield return Cine.Say(lines.wake);
        yield return EmoteBalloonUI.Show(player, "?", 0.8f);
    }

    private IEnumerator MeetSphinx()
    {
        yield return CineActor.Look(player, Vector2.up);
        Vector3 headPoint = head != null ? head.transform.position + Vector3.up * 1.5f : (Vector3)povFocus;
        CineCamera.Focus(headPoint);
        CineCamera.Zoom(wideSize, 2.5f);
        DreamFX.Ripple(2f, 0.15f);
        yield return Cine.Wait(2.6f);
        yield return EmoteBalloonUI.Show(player, "!", 0.8f);

        if (head != null) yield return head.Say(lines.greet);
        else yield return Cine.Say(lines.greet);
        yield return Cine.Wait(0.3f);
    }

    // 스킵했어도 최종 상태는 같게
    private void Finish()
    {
        EyelidUI.Hide();
        CineActor.SetAlpha(player, 1f);
        DreamFX.SetHaze(0f);
        CineAudio.SetLoopVolume(0.4f);
        Cine.End();
    }
}
