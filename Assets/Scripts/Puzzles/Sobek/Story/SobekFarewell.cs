using System.Collections;
using UnityEngine;

// [추가] 소베크의 방 남은 이야기: 메인을 깨고(물이 빠진 뒤) 허브를 떠나기 전에 한 번
// - 출구(중앙 홀로 / 아누비스의 신전으로)에 처음 들어서면 재생 -> 끝나면 원래대로 다음 씬으로
//   1) 케프리가 불러 세움 -> 2) 벽화 연출 "다시 흐르는 강" + "심장의 저울" (SobekMuralScenes.Farewell)
//   3) 페트수코스(악어)와 작별 + 다음 신전 안내 (소베크의 이빨이 있으면 심판 복선, 없으면 숨은 방 힌트)
// - 소베크의 이빨 없이 처음 떠나려 하면: 이야기 뒤 한 번 붙잡음 ("한 번 더 지나가면 떠남")
// 플래그: F_SO_FAREWELL_DONE (이야기 봄), F_SO_LEAVE_WARNED (이빨 힌트 봄)
// 붙이는 곳: 없음 (SobekSceneExit가 부름)
public static class SobekFarewell
{
    public const string FlagDone = "F_SO_FAREWELL_DONE";
    public const string FlagWarned = "F_SO_LEAVE_WARNED";

    private const string Me = SobekStory.Me;
    private const string Khepri = SobekStory.Khepri;

    // 이야기를 볼 차례인지 (메인을 깼고 아직 안 봄)
    public static bool Pending => SobekStory.HasFlag(SobekIds.FlagMainDone) && !SobekStory.HasFlag(FlagDone);

    // 이빨 없이 처음 떠나려 하는지 (한 번만 붙잡음)
    public static bool ShouldWarn =>
        SobekStory.HasFlag(SobekIds.FlagMainDone) && !HasTooth && !SobekStory.HasFlag(FlagWarned);

    private static bool HasTooth => SobekStory.HasFlag(SobekIds.FlagToothGot)
                                    || (ItemInventory.Instance != null && ItemInventory.Instance.Has(SobekIds.Tooth));

    // toHall: 중앙 홀로 가는 출구인지 (마지막 한마디가 달라짐)
    // 끝나면 stay = true 면 떠나지 않음 (이빨 힌트로 붙잡음)
    public static IEnumerator Play(bool toHall, System.Action<bool> done)
    {
        bool story = Pending;
        bool warn = ShouldWarn;
        Debug.Log($"[SobekFarewell] 떠나기 전 이야기 (이야기 {story}, 이빨 힌트 {warn}, {(toHall ? "중앙 홀" : "아누비스")})");
        Cine.Begin(true);

        if (story)
        {
            yield return Cine.Say(new[]
            {
                Line(Khepri, "잠깐! 물이 빠지면서 수로 벽에 그림이 새로 드러났어."),
                Line(Me, "물에 잠겨 있던 벽화... 떠나기 전에 보고 가자."),
            });
            yield return BastetMuralViewer.Get().PlayStory(SobekMuralScenes.Farewell(), false);
            yield return Cine.Say(new[]
            {
                Line(Khepri, "강이 다시 흐르니 태양의 배도 움직일 수 있어. 남은 건 마지막 문 하나야."),
                Line(Me, "물이 빠질 때까지 길을 알려 준 악어... 페트수코스였구나. 고마워."),
                Line(Khepri, "저 악어도 소베크의 아들이니까. 강이 돌아왔으니 이제 연못으로 돌아갈 거야."),
            });
            SobekStory.SetFlag(FlagDone);
            Debug.Log("[노트 44] 소베크: 물은 다시 흐른다. 배를 타고 마지막 문으로. 그곳에서는 심장이 대신 말한다."); // 수첩 메모
        }

        bool stay = false;
        if (warn)
        {
            SobekStory.SetFlag(FlagWarned);
            yield return Cine.Say(new[]
            {
                Line(Khepri, "그런데 아직 이 신전에서 못 본 게 있는 것 같아. 물이 빠진 수위계 기둥 주변을 살펴봤어?"),
                Line(Me, "숨은 게 더 있을지도 몰라... 그래도 떠나려면 한 번 더 지나가자."),
            });
            stay = true;
        }
        else if (story)
        {
            yield return Cine.Say(HasTooth
                ? new[] { Line(Khepri, "소베크의 이빨은 한 번 물면 놓지 않아. 심판에서 한 번 더 버틸 수 있을 거야.") }
                : new[] { Line(Khepri, "준비됐으면 가자.") });
            yield return Cine.Say(toHall
                ? new[] { Line(Me, "중앙 홀 큐브에 악어 형판을 끼우자. 그다음은... 아누비스의 저울이야.") }
                : new[] { Line(Me, "물 빠진 수로 아래, 심판의 문으로 내려가자.") });
        }

        Cine.End();
        if (stay) AnubisToast.Show("한 번 더 출구로 가면 신전을 떠납니다.", 2.5f);
        done?.Invoke(stay);
    }

    private static DialogueLine Line(string speaker, string text) => new DialogueLine(speaker, text);
}
