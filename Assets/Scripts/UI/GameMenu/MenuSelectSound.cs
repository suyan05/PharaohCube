using UnityEngine;
using UnityEngine.EventSystems;

// [추가] 버튼에 마우스를 올리거나 키보드로 고르면 이동 소리
public class MenuSelectSound : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    public void OnSelect(BaseEventData e) => MenuUI.Move();
    public void OnPointerEnter(PointerEventData e) => MenuUI.Move();
}
