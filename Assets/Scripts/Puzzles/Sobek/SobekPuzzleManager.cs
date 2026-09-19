using System;
using System.Collections.Generic;
using UnityEngine;

public enum PipeType { Empty, Straight, Corner, T, Cross }
public enum Direction { North = 0, East = 1, South = 2, West = 3 }

[System.Serializable]
public class CanalCellData
{
    public PipeType type;
    [HideInInspector] public int rotation; // 0~3, 90도 단위 회전 상태
}

public class SobekPuzzleManager : MonoBehaviour
{
    private const int GridSize = 3;

    [Header("3x3 타일 배치 (Inspector에서 각 칸 타입 지정, index 0~8)")]
    [SerializeField] private CanalCellData[] cells = new CanalCellData[9];

    [Header("물 시작/도착 지점")]
    [SerializeField] private Vector2Int entryCell = new Vector2Int(0, 0); // (row, col)
    [SerializeField] private Direction entryDirection = Direction.West;
    [SerializeField] private Vector2Int exitCell = new Vector2Int(2, 2);
    [SerializeField] private Direction exitDirection = Direction.East;

    // 각 파이프 타입이 회전 0도일 때 뚫려있는 방향 (상,우,하,좌 순서)
    private static readonly Dictionary<PipeType, bool[]> BaseOpenings = new Dictionary<PipeType, bool[]>
    {
        { PipeType.Empty,    new bool[] { false, false, false, false } },
        { PipeType.Straight, new bool[] { true,  false, true,  false } }, // 상-하
        { PipeType.Corner,   new bool[] { true,  true,  false, false } }, // 상-우 (ㄱ자)
        { PipeType.T,        new bool[] { true,  true,  true,  false } }, // 상-우-하
        { PipeType.Cross,    new bool[] { true,  true,  true,  true  } },
    };

    public event Action OnPuzzleCleared;
    public event Action<int> OnCellRotated; // 회전된 칸의 인덱스 전달 (UI 갱신용)
    public event Action<bool> OnConnectionChecked; // 회전할 때마다 연결 성공 여부 전달

    private bool isCleared = false;

    private int Index(int row, int col) => row * GridSize + col;

    public CanalCellData GetCell(int index) => cells[index];

    public bool[] GetCurrentOpenings(int index)
    {
        bool[] openings = (bool[])BaseOpenings[cells[index].type].Clone();
        for (int r = 0; r < cells[index].rotation; r++)
        {
            openings = RotateClockwise(openings);
        }
        return openings;
    }

    private bool[] RotateClockwise(bool[] openings)
    {
        bool[] result = new bool[4];
        for (int i = 0; i < 4; i++)
        {
            result[(i + 1) % 4] = openings[i];
        }
        return result;
    }

    public void RotateCell(int index)
    {
        if (isCleared) return;

        cells[index].rotation = (cells[index].rotation + 1) % 4;
        OnCellRotated?.Invoke(index);

        bool connected = CheckConnection();
        OnConnectionChecked?.Invoke(connected);

        if (connected)
        {
            isCleared = true;
            Debug.Log("[SobekPuzzle] 물이 제단까지 연결됨! 퍼즐 클리어!");
            OnPuzzleCleared?.Invoke();
        }
    }

    private bool CheckConnection()
    {
        bool[,] visited = new bool[GridSize, GridSize];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        int entryIndex = Index(entryCell.x, entryCell.y);
        bool[] entryOpenings = GetCurrentOpenings(entryIndex);

        // 시작 칸이 진입 방향으로 안 뚫려있으면 애초에 물이 못 들어옴
        if (!entryOpenings[(int)entryDirection])
        {
            return false;
        }

        visited[entryCell.x, entryCell.y] = true;
        queue.Enqueue(entryCell);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            bool[] openings = GetCurrentOpenings(Index(current.x, current.y));

            for (int d = 0; d < 4; d++)
            {
                if (!openings[d]) continue;

                Vector2Int next = GetNeighbor(current, (Direction)d);
                if (!IsInsideGrid(next)) continue;
                if (visited[next.x, next.y]) continue;

                bool[] nextOpenings = GetCurrentOpenings(Index(next.x, next.y));
                int oppositeDir = (d + 2) % 4; // 내가 오른쪽으로 열렸으면, 옆 칸은 왼쪽이 열려야 연결됨

                if (nextOpenings[oppositeDir])
                {
                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }
        }

        if (!visited[exitCell.x, exitCell.y]) return false;

        bool[] exitOpenings = GetCurrentOpenings(Index(exitCell.x, exitCell.y));
        return exitOpenings[(int)exitDirection];
    }

    private Vector2Int GetNeighbor(Vector2Int cell, Direction dir)
    {
        switch (dir)
        {
            case Direction.North: return new Vector2Int(cell.x - 1, cell.y);
            case Direction.East: return new Vector2Int(cell.x, cell.y + 1);
            case Direction.South: return new Vector2Int(cell.x + 1, cell.y);
            case Direction.West: return new Vector2Int(cell.x, cell.y - 1);
            default: return cell;
        }
    }

    private bool IsInsideGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < GridSize && cell.y >= 0 && cell.y < GridSize;
    }

    [ContextMenu("테스트: 현재 연결 상태 확인")]
    void TestCheckConnection()
    {
        Debug.Log($"[SobekPuzzle] 현재 연결 상태: {CheckConnection()}");
    }
}