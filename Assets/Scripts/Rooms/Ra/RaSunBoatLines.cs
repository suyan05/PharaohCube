// 태양의 배 모형 대사 (RaSunBoat 기본값. 씬에 저장된 뒤에는 Inspector에서 고치기)
public static class RaSunBoatLines
{
    // 낮에 조사 (복선)
    public static DialogueLine[] Day => new[]
    {
        new DialogueLine("나", "작은 배 모형이다. 뱃머리가 연꽃처럼 말려 올라가 있어."),
        new DialogueLine("나", "라가 매일 태양을 싣고 하늘을 건넌다는 '태양의 배'인가 봐."),
        new DialogueLine("나", "받침대에 글이 새겨져 있다..."),
        new DialogueLine("", "'라의 불은 해가 진 뒤에도 배 안에서 꺼지지 않는다.'"),
        new DialogueLine("나", "선실 안은 그냥 깜깜한데. 무슨 뜻이지?")
    };

    // 불씨를 가져간 뒤
    public static DialogueLine[] Empty => new[]
    {
        new DialogueLine("나", "배 안이 비었다. 불씨는 이제 내 횃불 속에서 타고 있어.")
    };

    // 불씨를 얻은 직후
    public static DialogueLine[] Found => new[]
    {
        new DialogueLine("나", "...따뜻해. 횃불 빛이 훨씬 넓어졌어."),
        new DialogueLine("나", "이 불은 시간이 지나도 꺼지지 않을 것 같아."),
        new DialogueLine("나", "혹시 꺼져도, E를 길게 누르면 불씨가 다시 붙여 줄 거야.")
    };

    // 밤이 되어 불이 다 꺼졌을 때, 배 글귀를 읽은 적이 있으면 덧붙는 혼잣말 (RaTorchManager)
    public static DialogueLine[] NightHint => new[]
    {
        new DialogueLine("나", "...잠깐. 서쪽 방 배 모형에 그런 글이 있었지."),
        new DialogueLine("나", "'해가 진 뒤에도 배 안에서 꺼지지 않는다'...")
    };
}
