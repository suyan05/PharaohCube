using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 드래그 가능한 카드 한 장. 드래그 시작/이동/끝을 스테이션에 알려줌
public class DragCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TimeSymbol symbol;

    public Action<DragCard> onBeginDrag;
    public Action<DragCard, Vector2> onDrag;   // 마우스 이동량 전달
    public Action<DragCard> onEndDrag;

    public void OnBeginDrag(PointerEventData eventData) => onBeginDrag?.Invoke(this);
    public void OnDrag(PointerEventData eventData) => onDrag?.Invoke(this, eventData.delta);
    public void OnEndDrag(PointerEventData eventData) => onEndDrag?.Invoke(this);
}