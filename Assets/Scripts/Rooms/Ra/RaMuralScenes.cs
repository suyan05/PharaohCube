using UnityEngine;

// [추가] 라의 방 벽화 컷 (서브 스토리). 그림은 프롤로그 벽화(Story1: 라의 배)를 다시 씀 -> 새 그림 없이 MuralStage로 재생
// - 밤 시작: 해 질 녘 아툼이 저승으로 내려가 양의 머리를 한 아우프가 되어 밤의 배에 누움 (Rashrine이 밤 전환 전에 부름)
public static class RaMuralScenes
{
    private const string Ra = "라";
    private const string Me = "나";

    public static MuralShot[] NightFalls()
    {
        MuralShot s = new MuralShot("RaNight", "Story1", 9f)
            .Camera(1.05f, 1.2f, new Vector2(120f, 0f), new Vector2(-80f, 40f))
            .Layer("boat", 3f, 1.2f)
            .Move("boat", new MuralPose(160f, -20f), new MuralPose(-260f, 10f), 0f, 1f) // 서쪽(왼쪽)으로 저물어 감
            .Dust(34)
            .Say(Ra, "해가 진다. 늙은 아툼의 몸으로, 나는 서쪽 문을 지나 저승의 강에 내린다.")
            .Say(Ra, "밤의 배 위에서 나는 양의 머리를 한 아우프. 빛을 잃은 태양의 몸이다.")
            .Say(Ra, "한밤에 오시리스와 하나가 되어야, 나는 새벽에 다시 태어난다.")
            .Say(Me, "그래서 밤에는 모든 게 거꾸로... 끝에서 시작으로 돌아가는 거구나.");
        return new[] { s };
    }
}
