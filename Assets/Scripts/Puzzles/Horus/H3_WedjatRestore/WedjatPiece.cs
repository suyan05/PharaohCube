using UnityEngine;
using UnityEngine.EventSystems;

// 우자트 조각 하나: 드래그하면 옮기고, 클릭하면 회전 요청
public class WedjatPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private WedjatRestoreView view;
    private int index;
    private bool dragging;

    public void Init(WedjatRestoreView owner, int pieceIndex)
    {
        view = owner;
        index = pieceIndex;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (view == null || !view.CanDrag(index)) return;
        dragging = true;
        view.BeginDrag(index);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragging) view.DragTo(index, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;
        view.Drop(index, eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging || view == null) return;
        view.Click(index);
    }
}