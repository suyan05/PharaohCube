#if UNITY_EDITOR
// 착시 퍼즐 7판의 배치 (오프라인 설계 도구가 무작위 배치 -> BFS로 골라낸 값)
// 조건: 풀림 + 착시 길 2번 이상 + 카메라 회전 2번 이상 + 장치 하나라도 멈추면 못 풂
// 줄 형식: seg x y z alongX len pillar / mech kind count start pivot x y z cells .. offs .. / plate x y z mech / start / goal / yaw / water
public static class SobekIllusionDesigns
{
    public const string S1 = @"
seg 2 5 0 0 5 1
seg -5 2 0 1 4 1
seg 2 3 -4 0 5 1
seg 5 4 3 0 5 1
seg 1 5 -4 1 4 1
seg -3 6 -4 0 5 1
seg -5 3 1 1 0 1
mech Rotate 4 0 pivot -5 4 1 cells -5,4,1 -4,4,1 -3,4,1 offs
plate 2 3 -1 0
start -5 2 0
goal -3 6 -2
yaw 0
water -1.45";

    public const string S2 = @"
seg 3 4 0 1 3 1
seg -1 5 -5 1 2 1
seg 0 4 -2 0 4 1
seg 1 6 2 1 2 1
seg -4 2 -2 0 2 1
seg 4 1 3 1 2 1
seg 5 6 0 0 4 1
mech Slide 3 0 pivot 0 0 0 cells 3,3,4 3,3,5 3,3,6 offs 0,0,0 -2,0,0 -4,0,0
plate 0 5 -5 0
start 5 1 3
goal 5 6 0
yaw 3
water -1.45";

    public const string S3 = @"
seg -4 3 -4 1 4 1
seg 2 1 2 1 3 1
seg 2 4 -4 0 4 1
seg 0 1 -1 1 3 1
seg -1 3 4 1 2 1
seg -5 3 -1 0 4 1
seg -4 2 4 0 5 1
mech Water 3 0 pivot 0 0 0 cells -2,-1,1 -1,-1,1 0,-1,1 offs
plate -4 2 8 0
start 0 1 -1
goal 2 4 -4
yaw 2
water -0.55";

    public const string S4 = @"
seg 4 0 -1 1 2 1
seg 2 2 2 0 3 1
seg 0 2 -2 1 2 1
seg 0 3 -4 1 2 1
seg -4 4 1 1 4 1
seg 4 5 -3 0 4 1
start 4 0 -1
goal 4 5 0
yaw 2
water -1.45";

    public const string M1 = @"
seg 4 1 -3 1 2 1
seg 4 3 2 0 2 1
seg -2 0 5 1 4 1
seg -5 2 4 1 2 1
seg 0 2 -1 1 5 1
seg -2 3 -5 1 2 1
seg -1 3 1 1 0 1
mech Rotate 4 0 pivot -1 4 1 cells -1,4,1 0,4,1 1,4,1 2,4,1 offs
mech Water 3 0 pivot 0 0 0 cells -3,-1,-3 -3,-1,-2 -3,-1,-1 offs
plate 5 1 -3 0
plate -5 2 4 1
start 0 0 5
goal -2 3 -5
yaw 0
water -0.55";

    public const string M2 = @"
seg -1 4 -3 0 5 1
seg 0 0 1 1 3 1
seg -5 5 -3 1 2 1
seg 4 2 -4 0 5 1
seg -5 5 5 0 4 1
seg 2 5 4 1 4 1
seg -3 3 -4 1 0 1
mech Slide 2 0 pivot 0 0 0 cells -1,4,3 0,4,3 1,4,3 offs 0,0,0 0,0,-1
mech Rotate 4 1 pivot -3 4 -4 cells -3,4,-4 -2,4,-4 -1,4,-4 0,4,-4 offs
plate -5 5 7 0
plate 4 5 4 1
start 4 2 -4
goal -4 5 -3
yaw 1
water -1.45";

    public const string M3 = @"
seg 1 0 -4 1 2 1
seg -1 4 0 1 5 1
seg 5 1 0 0 2 1
seg -3 4 1 0 5 1
seg -4 1 -5 0 4 1
seg 0 2 -5 1 0 1
mech Rotate 4 0 pivot 0 3 -5 cells 0,3,-5 1,3,-5 2,3,-5 3,3,-5 offs
mech Slide 3 0 pivot 0 0 0 cells -1,4,2 0,4,2 offs 0,0,0 1,0,0 2,0,0
mech Water 3 0 pivot 0 0 0 cells 1,-1,-1 2,-1,-1 3,-1,-1 offs
plate -3 4 4 0
plate 5 1 1 1
plate -3 4 5 2
start 1 0 -4
goal -1 4 0
yaw 2
water -0.55";

}
#endif
