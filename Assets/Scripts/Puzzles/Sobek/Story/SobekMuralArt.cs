using System.Collections.Generic;
using UnityEngine;

// [자동 생성] Tools/SobekArt/sobek_murals.py 가 만든 소베크 벽화 조각 위치표 (직접 고치지 말 것)
// 벽화 1280x720 픽셀, 왼쪽 위 0,0 / 조각 그림의 가운데. 그림을 다시 뽑으면 이 파일도 같이 바뀜
public static class SobekMuralArt
{
    private static readonly Dictionary<string, Vector2> Table = new Dictionary<string, Vector2>
    {
        { "SobekE1/boat", new Vector2(560.0f, 506.0f) },
        { "SobekE1/croc", new Vector2(1000.0f, 596.5f) },
        { "SobekE1/reeds", new Vector2(177.0f, 484.5f) },
        { "SobekE1/river", new Vector2(646.0f, 615.0f) },
        { "SobekE1/sun", new Vector2(760.0f, 191.5f) },
        { "SobekE2/anubis", new Vector2(1006.5f, 391.0f) },
        { "SobekE2/door", new Vector2(285.5f, 425.0f) },
        { "SobekE2/glow", new Vector2(760.5f, 330.5f) },
        { "SobekE2/scale", new Vector2(760.0f, 436.0f) },
        { "SobekS1/god", new Vector2(337.5f, 360.5f) },
        { "SobekS1/grain", new Vector2(926.5f, 456.0f) },
        { "SobekS1/river", new Vector2(810.5f, 537.5f) },
        { "SobekS1/sun", new Vector2(900.0f, 160.5f) },
        { "SobekS2/croc", new Vector2(300.5f, 538.5f) },
        { "SobekS2/egg", new Vector2(780.0f, 436.0f) },
        { "SobekS2/light", new Vector2(780.0f, 430.0f) },
        { "SobekS2/mound", new Vector2(780.5f, 549.5f) },
        { "SobekS2/sun", new Vector2(780.0f, 170.5f) },
        { "SobekS2/water", new Vector2(649.5f, 630.0f) },
        { "SobekS3/apep", new Vector2(1064.5f, 386.5f) },
        { "SobekS3/boat", new Vector2(380.0f, 527.5f) },
        { "SobekS3/head", new Vector2(837.0f, 519.0f) },
        { "SobekS3/river", new Vector2(646.0f, 590.0f) },
        { "SobekS3/sun", new Vector2(380.0f, 420.0f) },
        { "SobekS4/flood", new Vector2(540.0f, 519.0f) },
        { "SobekS4/gate", new Vector2(820.0f, 391.5f) },
        { "SobekS4/god", new Vector2(285.5f, 372.5f) },
        { "SobekS4/sockets", new Vector2(820.0f, 250.0f) },
        { "SobekW1/player", new Vector2(1120.0f, 474.5f) },
        { "SobekW1/rays", new Vector2(807.0f, 360.0f) },
        { "SobekW2/player", new Vector2(1130.0f, 474.5f) },
        { "SobekW2/rays", new Vector2(807.0f, 360.0f) },
        { "SobekW3/player", new Vector2(130.0f, 474.5f) },
        { "SobekW3/rays", new Vector2(807.0f, 360.0f) },
        { "SobekW4/player", new Vector2(1170.0f, 474.5f) },
        { "SobekW4/rays", new Vector2(807.0f, 360.0f) },
    };

    public static Vector2 Pos(string art, string layer)
    {
        if (Table.TryGetValue($"{art}/{layer}", out Vector2 p)) return p;
        Debug.LogWarning($"[소베크 벽화] 위치표에 없음: {art}/{layer}");
        return new Vector2(640f, 360f);
    }
}
