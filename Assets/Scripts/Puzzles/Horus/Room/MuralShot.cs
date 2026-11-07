using System.Collections.Generic;
using UnityEngine;

// 벽화 컷신 한 장면의 데이터 (MuralStage가 재생)
// - art: 그림 묶음 이름 (Resources/Horus/Mural/MU_<art>_<layer>), id: 음성 파일 이름에 씀
// - Layer(): 움직일 조각 추가 (위치는 MuralArt 표에서 자동으로 찾음)
// - Move(): 조각을 장면 진행도 t0 -> t1 (0~1) 동안 from -> to 로 움직임 (위치 차이, 회전, 투명도)
// - Say(): 아래 검은 띠에 나올 자막
public struct MuralPose
{
    public Vector2 offset;
    public float rot;
    public float alpha;

    public MuralPose(float x, float y, float rot = 0f, float alpha = 1f)
    {
        offset = new Vector2(x, y);
        this.rot = rot;
        this.alpha = alpha;
    }

    public static MuralPose Rest => new MuralPose(0f, 0f);
    public static MuralPose Hidden => new MuralPose(0f, 0f, 0f, 0f);

    public static MuralPose Lerp(MuralPose a, MuralPose b, float t)
    {
        return new MuralPose(
            Mathf.Lerp(a.offset.x, b.offset.x, t), Mathf.Lerp(a.offset.y, b.offset.y, t),
            Mathf.Lerp(a.rot, b.rot, t), Mathf.Lerp(a.alpha, b.alpha, t));
    }
}

public class MuralMove
{
    public string layer;
    public MuralPose from, to;
    public float t0, t1;
}

public class MuralLayer
{
    public string name;
    public Vector2 pos;      // 벽화 그림 기준 픽셀 (왼쪽 위 0,0)
    public float bobAmp;     // 둥실둥실 (픽셀)
    public float bobSpeed;
    public readonly List<MuralMove> moves = new List<MuralMove>();
}

public class MuralShot
{
    public string id, art;
    public float duration = 7f;            // 움직임이 다 끝나는 시간(초)
    public float zoomFrom = 1f, zoomTo = 1f;
    public Vector2 panFrom, panTo;         // 화면 가운데가 볼 곳 (벽화 픽셀, 가운데 기준 차이)
    public readonly List<MuralLayer> layers = new List<MuralLayer>();
    public readonly List<DialogueLine> lines = new List<DialogueLine>();

    public MuralShot(string id, string art, float duration)
    {
        this.id = id;
        this.art = art;
        this.duration = duration;
    }

    public MuralShot Camera(float zoomFrom, float zoomTo, Vector2 panFrom, Vector2 panTo)
    {
        this.zoomFrom = zoomFrom;
        this.zoomTo = zoomTo;
        this.panFrom = panFrom;
        this.panTo = panTo;
        return this;
    }

    public MuralShot Layer(string name, float bobAmp = 0f, float bobSpeed = 0f)
    {
        layers.Add(new MuralLayer { name = name, pos = MuralArt.Pos(art, name), bobAmp = bobAmp, bobSpeed = bobSpeed });
        return this;
    }

    public MuralShot Move(string layer, MuralPose from, MuralPose to, float t0, float t1)
    {
        MuralLayer l = layers.Find(x => x.name == layer);
        if (l == null)
        {
            Debug.LogWarning($"[벽화] {id}: 조각 '{layer}'을 먼저 Layer()로 넣어야 함");
            return this;
        }
        l.moves.Add(new MuralMove { layer = layer, from = from, to = to, t0 = t0, t1 = t1 });
        return this;
    }

    public MuralShot Say(string speaker, string text)
    {
        lines.Add(new DialogueLine(speaker, text));
        return this;
    }

    // 진행도 u(0~1)일 때 조각의 모습: 시작 전엔 첫 움직임의 from, 이후엔 마지막으로 시작한 움직임 기준
    public static MuralPose Evaluate(MuralLayer l, float u)
    {
        if (l.moves.Count == 0) return MuralPose.Rest;
        MuralPose pose = l.moves[0].from;
        foreach (MuralMove m in l.moves)
        {
            if (u < m.t0) break;
            float p = Mathf.Clamp01((u - m.t0) / Mathf.Max(0.0001f, m.t1 - m.t0));
            pose = MuralPose.Lerp(m.from, m.to, Mathf.SmoothStep(0f, 1f, p));
        }
        return pose;
    }
}
