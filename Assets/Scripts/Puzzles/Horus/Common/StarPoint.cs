using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum StarState { Normal, Dark, Cloud }

// 별 하나. 누르면 StarLineDrawer에 "여기서 획 시작"을 알림
public class StarPoint : MonoBehaviour, IPointerDownHandler
{
    public Vector2Int Coord { get; private set; }
    public StarState State { get; private set; }
    public RectTransform Rect { get; private set; }
    public bool IsPassable => State == StarState.Normal;

    private StarLineDrawer drawer;
    private Image image;

    public void Init(Vector2Int coord, StarLineDrawer owner)
    {
        Coord = coord;
        drawer = owner;
        Rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public void SetState(StarState state, Color color)
    {
        State = state;
        if (image != null) image.color = color;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 디버그용: 어떤 별이 눌렸는지 확인 (문제 해결 후 지워도 됨)
        Debug.Log($"[StarPoint] 클릭: ({Coord.x},{Coord.y}) 상태 {State}");
        if (drawer == null)
        {
            Debug.LogWarning($"[StarPoint] {Coord} 별에 Drawer가 연결되지 않음");
            return;
        }
        drawer.BeginStroke(this);
    }
}