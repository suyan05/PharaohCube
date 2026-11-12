using System.Collections.Generic;
using UnityEngine;

// 옆에서 본 통로 장면 하나의 데이터 (SideWalkStage가 재생)
// - 좌표는 도트 그림 기준 px (화면 한 장 = 가로 320, 세로 180). length = 통로 전체 길이
//   new SideWalkScene("밤의 뱃길", "라의 신전 아래", 900f)
//       .Tint(벽 색, 불빛 색)
//       .Start(new DialogueLine("나", "..."))                         // 들어서자마자 하는 말
//       .Gate(300f, "다섯째 시간", false)                             // 문 장식 (dark = 검게 칠해진 문)
//       .Spot(230f, 92f, null, "E: 벽의 글귀 읽기", 대사들...)         // E로 조사하는 지점 (그림은 없어도 됨)
//       .Exit("E: 안으로 들어가기");                                  // 오른쪽 끝 문 앞에서 뜨는 문구
public class SideWalkSpot
{
    public float x, y;
    public string sprite, prompt;
    public DialogueLine[] lines;
    public bool seen;
}

public class SideWalkGate
{
    public float x;
    public string label;
    public bool dark;
}

public class SideWalkScene
{
    public string title, sub;
    public float length;
    public Color wallTint = Color.white;
    public Color glowTint = new Color(1f, 0.7f, 0.35f, 1f);
    public string exitPrompt = "E: 안으로 들어가기";
    public DialogueLine[] startLines;
    public readonly List<SideWalkSpot> spots = new List<SideWalkSpot>();
    public readonly List<SideWalkGate> gates = new List<SideWalkGate>();

    public SideWalkScene(string title, string sub, float length)
    {
        this.title = title;
        this.sub = sub;
        this.length = Mathf.Max(360f, length);
    }

    public SideWalkScene Tint(Color wall, Color glow)
    {
        wallTint = wall;
        glowTint = glow;
        return this;
    }

    public SideWalkScene Start(params DialogueLine[] lines)
    {
        startLines = lines;
        return this;
    }

    public SideWalkScene Gate(float x, string label, bool dark)
    {
        gates.Add(new SideWalkGate { x = x, label = label, dark = dark });
        return this;
    }

    public SideWalkScene Spot(float x, float y, string sprite, string prompt, params DialogueLine[] lines)
    {
        spots.Add(new SideWalkSpot { x = x, y = y, sprite = sprite, prompt = prompt, lines = lines });
        return this;
    }

    public SideWalkScene Exit(string prompt)
    {
        exitPrompt = prompt;
        return this;
    }
}
