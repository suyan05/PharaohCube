using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 보드 칸 하나의 클릭 영역. 좌클릭/우클릭을 구분
public class BoardCellClick : MonoBehaviour, IPointerClickHandler
{
    public Vector2Int tile;
    public Action<Vector2Int, bool> onClick; // bool = 우클릭 여부

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            onClick?.Invoke(tile, false);
        else if (eventData.button == PointerEventData.InputButton.Right)
            onClick?.Invoke(tile, true);
    }
}