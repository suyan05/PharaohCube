using System.Collections.Generic;
using UnityEngine;

// 토트의 방 벽화 조각표 (그림 만드는 스크립트 th_fresco.py가 같이 뽑은 값 -> 그림을 다시 뽑으면 이 표도 같이 바꿔야 함)
// - 판 960 x 540, 조각 가운데 위치는 판 가운데 기준 (x 오른쪽 +, y 위쪽 +), 크기는 조각 그림 픽셀 크기
// - 조각 그림: Resources/Thoth/Fresco/ThF{A,B,C}_p0 ~ p8, 빈 판: ThF?_board
public static class ThothFrescoData
{
    public const float BoardW = 960f;
    public const float BoardH = 540f;
    public const int PieceCount = 9;

    private static readonly Dictionary<string, Vector4[]> Pieces = new Dictionary<string, Vector4[]>
    {
        { "A", new[]
            {
                new Vector4(-318.5f, 161.0f, 323f, 218f),
                new Vector4(-17.5f, 161.0f, 361f, 218f),
                new Vector4(301.0f, 161.0f, 358f, 218f),
                new Vector4(-318.5f, 0.0f, 323f, 186f),
                new Vector4(0.0f, -17.5f, 396f, 221f),
                new Vector4(318.5f, 0.0f, 323f, 186f),
                new Vector4(-301.0f, -161.0f, 358f, 218f),
                new Vector4(0.0f, -178.5f, 326f, 183f),
                new Vector4(301.0f, -161.0f, 358f, 218f),
            }
        },
        { "B", new[]
            {
                new Vector4(-301.0f, 161.0f, 358f, 218f),
                new Vector4(0.0f, 161.0f, 326f, 218f),
                new Vector4(301.0f, 178.5f, 358f, 183f),
                new Vector4(-301.0f, -17.5f, 358f, 221f),
                new Vector4(0.0f, -17.5f, 326f, 221f),
                new Vector4(301.0f, 17.5f, 358f, 221f),
                new Vector4(-301.0f, -178.5f, 358f, 183f),
                new Vector4(17.5f, -178.5f, 361f, 183f),
                new Vector4(318.5f, -161.0f, 323f, 218f),
            }
        },
        { "C", new[]
            {
                new Vector4(-318.5f, 161.0f, 323f, 218f),
                new Vector4(0.0f, 161.0f, 396f, 218f),
                new Vector4(318.5f, 178.5f, 323f, 183f),
                new Vector4(-301.0f, -17.5f, 358f, 221f),
                new Vector4(17.5f, -17.5f, 361f, 221f),
                new Vector4(318.5f, 0.0f, 323f, 256f),
                new Vector4(-301.0f, -178.5f, 358f, 183f),
                new Vector4(0.0f, -178.5f, 326f, 183f),
                new Vector4(301.0f, -178.5f, 358f, 183f),
            }
        },
    };

    // 벽화 앞에서 처음 주는 6개 말고, 방 곳곳에 흩어진 3개 (조각 번호)
    private static readonly Dictionary<string, int[]> Scattered = new Dictionary<string, int[]>
    {
        { "A", new[] { 1, 5, 6 } },
        { "B", new[] { 2, 3, 7 } },
        { "C", new[] { 0, 5, 7 } }
    };

    public static Vector4 Piece(string fresco, int index)
    {
        Vector4[] list;
        if (!Pieces.TryGetValue(fresco, out list) || index < 0 || index >= list.Length) return new Vector4(0f, 0f, 320f, 180f);
        return list[index];
    }

    public static bool IsScattered(string fresco, int index)
    {
        int[] list;
        if (!Scattered.TryGetValue(fresco, out list)) return false;
        return System.Array.IndexOf(list, index) >= 0;
    }

    public static int[] ScatteredOf(string fresco)
    {
        int[] list;
        return Scattered.TryGetValue(fresco, out list) ? list : new int[0];
    }

    public static string Art(string fresco) => "ThF" + fresco;

    public static string Title(string fresco)
    {
        if (fresco == "A") return "글자를 만든 토트";
        if (fresco == "B") return "달과의 내기";
        if (fresco == "C") return "되찾은 눈";
        return "부서진 벽화";
    }
}
