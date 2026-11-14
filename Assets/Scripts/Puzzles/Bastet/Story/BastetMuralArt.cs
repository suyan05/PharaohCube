using System.Collections.Generic;
using UnityEngine;

// [자동 생성] Tools/BastetMural/bastet_murals.py 가 만든 바스테트 벽화 조각 위치표 (직접 고치지 말 것)
// 벽화 1280x720 픽셀, 왼쪽 위 0,0 / 조각 그림의 가운데. 그림을 다시 뽑으면 이 파일도 같이 바뀜
public static class BastetMuralArt
{
    private static readonly Dictionary<string, Vector2> Table = new Dictionary<string, Vector2>
    {
        { "BastetS1/beam", new Vector2(909.5f, 429.5f) },
        { "BastetS1/cat", new Vector2(1030.5f, 526.5f) },
        { "BastetS1/eye", new Vector2(636.5f, 333.5f) },
        { "BastetS1/goddess", new Vector2(929.5f, 448.0f) },
        { "BastetS1/sun", new Vector2(640.0f, 150.5f) },
        { "BastetS2/apep", new Vector2(1030.5f, 500.0f) },
        { "BastetS2/cat", new Vector2(623.5f, 478.0f) },
        { "BastetS2/head", new Vector2(786.5f, 416.5f) },
        { "BastetS2/knife", new Vector2(720.0f, 395.0f) },
        { "BastetS3/cat", new Vector2(1047.0f, 533.0f) },
        { "BastetS3/flame", new Vector2(520.0f, 493.0f) },
        { "BastetS3/snake", new Vector2(1221.5f, 577.0f) },
        { "BastetS4/goddess", new Vector2(332.0f, 433.0f) },
        { "BastetS4/mummy1", new Vector2(650.0f, 500.0f) },
        { "BastetS4/mummy2", new Vector2(778.0f, 493.0f) },
        { "BastetS4/mummy3", new Vector2(906.0f, 500.0f) },
        { "BastetS4/mummy4", new Vector2(1034.0f, 493.0f) },
        { "BastetS4/mummy5", new Vector2(1162.0f, 500.0f) },
        { "BastetW1/player", new Vector2(250.0f, 474.5f) },
        { "BastetW1/rays", new Vector2(807.0f, 360.0f) },
        { "BastetW2/player", new Vector2(200.0f, 474.5f) },
        { "BastetW2/rays", new Vector2(807.0f, 360.0f) },
        { "BastetW3/player", new Vector2(250.0f, 474.5f) },
        { "BastetW3/rays", new Vector2(807.0f, 360.0f) },
        { "BastetW4/player", new Vector2(1185.0f, 474.5f) },
        { "BastetW4/rays", new Vector2(807.0f, 360.0f) },
    };

    public static Vector2 Pos(string art, string layer)
    {
        if (Table.TryGetValue($"{art}/{layer}", out Vector2 p)) return p;
        Debug.LogWarning($"[바스테트 벽화] 위치표에 없음: {art}/{layer}");
        return new Vector2(640f, 360f);
    }
}
