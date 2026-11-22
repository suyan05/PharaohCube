using UnityEngine;

// [추가] 소베크 숨은 방 가는 길 "페트수코스의 길" (옆모습 통로, 라·호루스·바스테트 숨은 방과 같은 SideWalkStage)
// - 수위계 옆 계단을 처음 내려가면 이 통로를 직접 걸어감 -> 끝의 문을 지나면 숨은 방(물 빠진 하층)에 도착 -> 소베크의 이빨
// - 신화: 셰데트 신전 연못에서 섬긴 성스러운 악어 페트수코스, 미라가 된 악어들, 그물로 건진 넷(아누비스 복선)
// 문구는 여기서만 고치면 됨. 흐름 공통 코드(SideWalkStage)는 사랑님 것을 그대로 씀 (안 고침)
public static class SobekVaultScenes
{
    private const string Me = "나";
    private const string Wall = "벽의 글귀";

    public const string FlagWalked = "F_SO_VAULT_WALKED"; // 통로를 한 번 걸었음 (다음부터는 계단으로 바로)

    public static SideWalkScene Walk()
    {
        return new SideWalkScene("페트수코스의 길", "물 빠진 신전 아래", 760f)
            .Tint(new Color(0.80f, 0.92f, 0.90f, 1f), new Color(0.55f, 0.85f, 1f, 1f))
            .Start(new DialogueLine(Me, "계단 아래로 축축한 길이 이어져 있어. 물이 빠진 지 얼마 안 됐나 봐."),
                   new DialogueLine(Me, "벽마다 악어가 새겨져 있어. 끝까지 가 보자."))
            .Spot(200f, 60f, null, "E: 벽의 글귀 읽기",
                  new DialogueLine(Wall, "신전 연못의 악어를 소베크의 아들이라 불렀다. 이름은 페트수코스."),
                  new DialogueLine(Wall, "금 귀걸이를 걸어 주고, 죽으면 미라로 만들어 다음 악어에게 자리를 물려주었다."))
            .Spot(430f, 96f, "SW_Mark", "E: 벽감 살펴보기",
                  new DialogueLine(Me, "벽감마다 붕대에 감긴 작은 악어들이 누워 있어."),
                  new DialogueLine(Me, "몇 대에 걸친 페트수코스들일까..."))
            .Spot(640f, 70f, null, "E: 그물 그림 살펴보기",
                  new DialogueLine(Wall, "소베크는 물에서 넷을 그물로 건져 올렸다. 그들은 지금 심판의 방에서 죽은 자의 장기를 지킨다."),
                  new DialogueLine(Me, "넷... 심판의 방. 기억해 두자."))
            .Exit("E: 마른 연못으로 들어가기");
    }

    public static DialogueLine[] Arrive => new[]
    {
        new DialogueLine(Me, "마른 연못이야. 페트수코스가 살던 곳..."),
        new DialogueLine(Me, "제단 위에서 뭔가 빛나고 있어.")
    };
}
