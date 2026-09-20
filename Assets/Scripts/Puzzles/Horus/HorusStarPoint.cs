using UnityEngine;
using UnityEngine.EventSystems;

public class HorusStarPoint : MonoBehaviour, IPointerDownHandler
{
    [Header("이 별의 번호 (0부터 시작)")]
    public int index;

    [SerializeField] private HorusPuzzleUI puzzleUI;

    public void OnPointerDown(PointerEventData eventData)
    {
        puzzleUI.BeginDrag(index);
    }
}