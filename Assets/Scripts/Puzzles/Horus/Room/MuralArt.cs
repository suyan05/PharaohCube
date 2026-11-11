using System.Collections.Generic;
using UnityEngine;

// 벽화 조각의 처음 위치표 (벽화 1280x720 픽셀, 왼쪽 위 0,0 / 조각 그림의 가운데)
// 그림 만드는 스크립트(scenes.py)가 같이 뽑아낸 값 -> 그림을 다시 뽑으면 이 표도 같이 바꿔야 함
public static class MuralArt
{
    private static readonly Dictionary<string, Vector2> Table = new Dictionary<string, Vector2>
    {
        { "IntroA/osiris", new Vector2(435.0f, 413.5f) },
        { "IntroA/set", new Vector2(850.5f, 402.5f) },
        { "IntroB/isis", new Vector2(559.0f, 460.0f) },
        { "IntroB/child", new Vector2(845.0f, 507.5f) },
        { "EyeA/horus", new Vector2(493.0f, 382.0f) },
        { "EyeA/set", new Vector2(787.5f, 402.5f) },
        { "EyeA/eye_white", new Vector2(640.0f, 248.5f) },
        { "EyeA/eye_pupil", new Vector2(640.0f, 248.0f) },
        { "EyeA/eye_brow", new Vector2(641.5f, 215.5f) },
        { "EyeA/eye_line", new Vector2(708.5f, 250.0f) },
        { "EyeA/eye_tail", new Vector2(616.5f, 280.5f) },
        { "EyeA/eye_tear", new Vector2(641.0f, 287.0f) },
        { "EyeB/thoth", new Vector2(379.5f, 437.0f) },
        { "EyeB/eye_white", new Vector2(640.0f, 327.5f) },
        { "EyeB/eye_pupil", new Vector2(640.0f, 327.0f) },
        { "EyeB/eye_brow", new Vector2(642.5f, 273.0f) },
        { "EyeB/eye_line", new Vector2(751.5f, 330.0f) },
        { "EyeB/eye_tail", new Vector2(602.0f, 379.0f) },
        { "EyeB/eye_tear", new Vector2(641.5f, 390.0f) },
        { "Race/horus", new Vector2(352.5f, 395.5f) },
        { "Race/boatH", new Vector2(330.0f, 553.5f) },
        { "Race/set", new Vector2(892.5f, 407.5f) },
        { "Race/boatS", new Vector2(870.0f, 575.0f) },
        { "Race/water", new Vector2(640.0f, 645.0f) },
        { "EndA/ennead", new Vector2(844.5f, 423.5f) },
        { "EndA/horus", new Vector2(258.0f, 382.0f) },
        { "EndB/isis", new Vector2(459.0f, 460.0f) },
        { "EndB/horus", new Vector2(843.0f, 395.5f) },
        { "EndB/sun", new Vector2(640.0f, 167.5f) },
        { "Wall1/rays", new Vector2(807.0f, 360.0f) },
        { "Wall1/player", new Vector2(790.0f, 474.5f) },
        { "Wall2/rays", new Vector2(807.0f, 360.0f) },
        { "Wall2/player", new Vector2(1060.0f, 474.5f) },
        { "Wall3/rays", new Vector2(807.0f, 360.0f) },
        { "Wall3/player", new Vector2(700.0f, 474.5f) },
        { "Wall4/rays", new Vector2(807.0f, 360.0f) },
        { "Wall4/player", new Vector2(260.0f, 474.5f) },
        { "Story1/boat", new Vector2(657.5f, 480.0f) },
        { "Story2/apep", new Vector2(312.0f, 393.0f) },
        { "Story2/piece1", new Vector2(872.5f, 245.0f) },
        { "Story2/piece2", new Vector2(875.5f, 327.0f) },
        { "Story2/piece3", new Vector2(820.0f, 355.5f) },
        { "Story2/piece4", new Vector2(765.0f, 327.0f) },
        { "Story2/piece5", new Vector2(767.5f, 245.0f) },
        { "Story3/frag1", new Vector2(177.0f, 312.0f) },
        { "Story3/frag2", new Vector2(418.5f, 339.0f) },
        { "Story3/frag3", new Vector2(640.0f, 348.5f) },
        { "Story3/frag4", new Vector2(862.0f, 339.0f) },
        { "Story3/frag5", new Vector2(1103.0f, 312.0f) },
        { "Story4/anubis", new Vector2(267.5f, 440.0f) },
        { "Story4/scale", new Vector2(700.0f, 438.0f) },
    };

    public static Vector2 Pos(string art, string layer)
    {
        if (Table.TryGetValue($"{art}/{layer}", out Vector2 p)) return p;
        Debug.LogWarning($"[벽화] 위치표에 없음: {art}/{layer}");
        return new Vector2(640f, 360f);
    }
}
