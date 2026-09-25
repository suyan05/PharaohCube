using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AnubisUIScalePan : MonoBehaviour, IDropHandler
{
    private readonly List<AnubisUIDragItem> items = new List<AnubisUIDragItem>();

    // [추가] 외부에서 접시 위의 아이템 목록을 읽을 수 있게 함
    public List<AnubisUIDragItem> Items => items;

    public float TotalWeight { get; private set; } = 0f;
    public event Action OnWeightChanged;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        AnubisUIDragItem dragItem = eventData.pointerDrag.GetComponent<AnubisUIDragItem>();
        if (dragItem != null)
        {
            AddItem(dragItem);
        }
    }

    public void AddItem(AnubisUIDragItem item)
    {
        if (!items.Contains(item))
        {
            items.Add(item);
        }
        UpdateItemsPosition();
        RecalculateWeight();
    }

    public void RemoveItem(AnubisUIDragItem item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
            UpdateItemsPosition();
            RecalculateWeight();
        }
    }

    private void UpdateItemsPosition()
    {
        int count = items.Count;
        float landY = 65f;

        for (int i = 0; i < count; i++)
        {
            float offsetX = (i - (count - 1) * 0.5f) * 25f;
            Vector2 targetPos = new Vector2(offsetX, landY);

            items[i].FallToPan(this, targetPos);
        }
    }

    private void RecalculateWeight()
    {
        TotalWeight = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
                TotalWeight += items[i].weightValue;
        }
        OnWeightChanged?.Invoke();
    }
}