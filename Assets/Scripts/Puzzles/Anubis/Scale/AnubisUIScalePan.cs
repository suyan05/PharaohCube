using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 저울 접시. 유물을 올리면 무게를 더함 (LeftPan / RightPan에 부착)
// [수정] 유물이 접시가 아니라 화면 위쪽(하늘)으로 날아가던 문제
//  - 원인 1: 접시 그림을 만들려고 LeftPan을 복사해 LeftPan (1)을 만들면 이 컴포넌트까지 복사됨
//            -> 유물이 그림용 복사본에 올라가서 위치가 엉뚱하고, 심판은 진짜 접시(무게 0)를 봄
//            -> 복수 정답을 맞춰도 실패, 모두 내리기도 진짜 접시만 비우니 안 내려감
//            => 다른 접시 안에 들어 있는 접시 컴포넌트는 스스로 꺼짐
//  - 원인 2: 고정 좌표(접시 기준 위로 65px)에 놓아서 접시 크기/기준점이 바뀌면 어긋남
//            => 접시 그림(자식 포함)의 실제 화면 범위 아래쪽, 그릇 안에 놓음
public class AnubisUIScalePan : MonoBehaviour, IDropHandler
{
    [Header("유물이 놓이는 위치")]
    [Tooltip("(선택) 유물이 놓일 점. 비우면 접시 그림 범위의 아래쪽에 자동으로 놓음")]
    [SerializeField] private RectTransform itemAnchor;
    [Tooltip("그릇 바닥에서 얼마나 위에 유물 바닥을 둘지 (접시 그림 높이 대비 비율)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float landHeightRatio = 0.08f;
    [Tooltip("유물 여러 개를 나란히 놓을 때 간격 (px)")]
    [SerializeField] private float itemSpacing = 60f;

    [Header("수평 유지")]
    [Tooltip("켜면 저울대가 기울어도 접시는 항상 수평 (매달린 접시처럼)")]
    [SerializeField] private bool keepLevel = true;

    private readonly List<AnubisUIDragItem> items = new List<AnubisUIDragItem>();
    private readonly List<RectTransform> rectBuffer = new List<RectTransform>(16); // 범위 계산용 재사용
    private readonly Vector3[] corners = new Vector3[4];

    // 외부에서 접시 위의 아이템 목록을 읽을 수 있게 함
    public List<AnubisUIDragItem> Items => items;

    public float TotalWeight { get; private set; } = 0f;
    public event Action OnWeightChanged;

    private void Awake()
    {
        // [추가] 다른 접시 안에 들어 있는 복사본이면 꺼짐 (그림용으로 복사한 LeftPan (1) 등)
        Transform p = transform.parent;
        if (p != null && p.GetComponentInParent<AnubisUIScalePan>() != null)
        {
            Debug.LogWarning($"[{name}] 다른 접시 안에 있는 접시 컴포넌트라 꺼짐 (그림용 복사본이면 이 컴포넌트를 지워도 됨)", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (keepLevel) transform.rotation = Quaternion.identity;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || eventData.pointerDrag == null) return;

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
        Debug.Log($"[{name}] {item.itemName} 올림 -> 접시 무게 {TotalWeight}kg ({items.Count}개)", this);
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

    // [수정] 화면(월드) 좌표로 그릇 안 자리를 계산해서 넘김
    private void UpdateItemsPosition()
    {
        int count = items.Count;
        if (count == 0) return;

        Vector3 baseWorld = GetLandingPoint(out float scale);
        for (int i = 0; i < count; i++)
        {
            float offsetX = (i - (count - 1) * 0.5f) * itemSpacing * scale;
            AnubisUIDragItem item = items[i];
            float halfH = item.WorldHalfHeight;
            items[i].FallToPan(this, baseWorld + new Vector3(offsetX, halfH, 0f));
        }
    }

    // 유물 바닥이 놓일 점 (월드). scale = 캔버스 배율 (px -> 월드)
    private Vector3 GetLandingPoint(out float scale)
    {
        scale = transform.lossyScale.x != 0f ? Mathf.Abs(transform.lossyScale.x) : 1f;
        if (itemAnchor != null) return itemAnchor.position;

        // 접시 + 자식 그림(사슬, 그릇)의 범위. 올라가 있는 유물은 빼고 계산
        GetComponentsInChildren(false, rectBuffer);
        float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
        bool any = false;
        foreach (RectTransform rt in rectBuffer)
        {
            if (rt.GetComponentInParent<AnubisUIDragItem>() != null) continue;
            rt.GetWorldCorners(corners);
            for (int c = 0; c < 4; c++)
            {
                xMin = Mathf.Min(xMin, corners[c].x); xMax = Mathf.Max(xMax, corners[c].x);
                yMin = Mathf.Min(yMin, corners[c].y); yMax = Mathf.Max(yMax, corners[c].y);
                any = true;
            }
        }
        if (!any) return transform.position;

        float height = yMax - yMin;
        return new Vector3((xMin + xMax) * 0.5f, yMin + height * landHeightRatio, transform.position.z);
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

    // Scene 뷰 (Play 중, 선택 시): 유물이 놓일 자리
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Vector3 p = GetLandingPoint(out float s);
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(p, 10f * s);
    }
}
