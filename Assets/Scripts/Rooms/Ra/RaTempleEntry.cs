using System;
using System.Collections;
using UnityEngine;

// 라의 신전 첫 입장 연출 (기존 RaIntroCutscene 대신 - 이제 신전은 어둡고 내 횃불만 있음)
// 1. 검은 화면에 자막 "라의 신전 / 첫 번째 문: 태양"
// 2. 화면이 열리면 내 횃불 빛만 보임 (가까이) -> "..." -> 혼잣말
// 3. 카메라가 천천히 물러남 -> 가장 가까운 벽 횃불을 비춤 -> "!" -> "내 불을 옮겨 붙이자"
// 4. 조작 시작 (목표: 벽 횃불에 불 붙이기 = RaObjectiveGuide 첫 단계)
// - 한 번만 (F_RA_TEMPLE_ENTERED). 끝나면 F_RA_INTRO_PLAYED도 켜서 예전 흐름과 맞춤
// - ESC 1초 = 건너뛰기
public class RaTempleEntry : MonoBehaviour
{
    [SerializeField] private string playedFlag = "F_RA_TEMPLE_ENTERED";
    [SerializeField] private string legacyIntroFlag = "F_RA_INTRO_PLAYED";
    [Tooltip("처음 비춰줄 벽 횃불 (배치 도구가 시작 위치에서 가장 가까운 것으로 채움)")]
    [SerializeField] private WallTorch firstTorch;

    [Header("자막")]
    [SerializeField] private string title = "라의 신전";
    [SerializeField] private string subtitle = "첫 번째 문 - 태양";

    [Header("카메라")]
    [SerializeField, Min(1f)] private float closeSize = 3f;
    [SerializeField, Min(1f)] private float roomSize = 5f;

    [Header("대사")]
    [SerializeField] private DialogueLine[] darkLines =
    {
        new DialogueLine("나", "...아무것도 안 보여."),
        new DialogueLine("나", "중앙 홀에서 횃불을 챙겨 오길 잘했다.")
    };
    [SerializeField] private DialogueLine[] torchLines =
    {
        new DialogueLine("나", "벽에 횃불이 걸려 있어. 다 꺼져 있네."),
        new DialogueLine("나", "내 불을 옮겨 붙이자. (벽 횃불 앞에서 E 길게)")
    };

    private void Awake()
    {
        if (!HasFlag(playedFlag)) ScreenFader.SetBlack(); // 첫 화면이 번쩍 보이지 않게
    }

    private IEnumerator Start()
    {
        if (HasFlag(playedFlag)) yield break;
        yield return null; // 다른 Start(카메라, 횃불)가 먼저 준비되게 한 프레임 기다림
        yield return Play();
    }

    private IEnumerator Play()
    {
        Transform player = CineActor.Player();
        Cine.Begin(true);
        Prepare(player);

        yield return TitleCardUI.Show(title, subtitle, 2.2f);
        yield return ScreenFader.FadeIn(Cine.Skipping ? 0.2f : 1.6f);
        yield return Cine.Wait(0.8f);
        yield return EmoteBalloonUI.Show(player, "...", 1f);
        yield return Cine.Say(darkLines);

        yield return ShowTorch(player);
        Finish();
    }

    private void Prepare(Transform player)
    {
        try
        {
            ScreenFader.SetBlack();
            if (player != null) CineCamera.Focus(player.position + Vector3.up * 0.5f);
            CineCamera.Zoom(closeSize);
            CineCamera.SnapZoom();
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaTempleEntry] 준비 오류: {e}");
        }
    }

    private IEnumerator ShowTorch(Transform player)
    {
        CineCamera.Zoom(roomSize, 2.2f);
        yield return Cine.Wait(1.4f);
        if (firstTorch != null)
        {
            CineCamera.Focus(firstTorch.transform.position);
            yield return Cine.Wait(1.2f);
        }
        yield return EmoteBalloonUI.Show(player, "!", 0.7f);
        yield return Cine.Say(torchLines);
    }

    private void Finish()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            if (!gm.HasFlag(playedFlag)) gm.SetFlag(playedFlag);
            if (!gm.HasFlag(legacyIntroFlag)) gm.SetFlag(legacyIntroFlag);
        }
        Cine.End();
    }

    private static bool HasFlag(string flag)
    {
        return GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }
}
