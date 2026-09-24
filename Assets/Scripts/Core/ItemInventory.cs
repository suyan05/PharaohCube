using System;
using System.Collections.Generic;
using UnityEngine;

// 아이템 ID 모음 (기획서 4.1 / 10.2 기준). 문자열 오타 방지용
public static class ItemIds
{
    public const string LensSun = "ITEM_LENS_SUN";      // 주황 렌즈 '태양의 눈' (P2-1 보상)
    public const string Frag1 = "FRAG_1";             // 거울 조각 ① (P1 보상)
    public const string PigmentNight = "ITEM_PIGMENT_NIGHT"; // 청색 안료 '밤의 먹물' (P2-2 보상)
    public const string Frag2 = "FRAG_2";             // 거울 조각 ② (P3 보상)
    public const string Prism = "PRISM";              // 프리즘 (P3 보상)
    public const string KeyBrass = "ITEM_KEY_BRASS";     // 황동 열쇠 (P2-3 보상)
    public const string Frag3 = "FRAG_3";             // 거울 조각 ③ (P4 보상)

    public static readonly string[] All =
    {
        LensSun, Frag1, PigmentNight, Frag2, Prism, KeyBrass, Frag3
    };
}

public class ItemInventory : MonoBehaviour
{
    public static ItemInventory Instance { get; private set; }

    private readonly HashSet<string> items = new HashSet<string>();

    public event Action<string> OnItemGranted;   // 획득 시 (나중에 토스트 UI 연결)
    public event Action<string> OnItemConsumed;  // 사용 시

    private void Awake()
    {
        // 중복 Managers는 GameManager가 통째로 지워주니까, 여기선 등록만 막음
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // 기획서 2.1 개발자 치트: F5 = 아이템 전량 지급 (리허설용)
        if (Input.GetKeyDown(KeyCode.F5))
        {
            GrantAll();
        }
    }

    public void Grant(string itemId)
    {
        if (items.Contains(itemId))
        {
            Debug.Log($"[Item] 이미 가지고 있음: {itemId}");
            return;
        }

        items.Add(itemId);
        Debug.Log($"[Item] 획득: {itemId}");
        OnItemGranted?.Invoke(itemId);
    }

    public bool Has(string itemId)
    {
        return items.Contains(itemId);
    }

    // 성공하면 true, 없는 아이템이면 false
    public bool Consume(string itemId)
    {
        if (!items.Contains(itemId))
        {
            Debug.Log($"[Item] 사용 실패, 없는 아이템: {itemId}");
            return false;
        }

        items.Remove(itemId);
        Debug.Log($"[Item] 사용: {itemId}");
        OnItemConsumed?.Invoke(itemId);
        return true;
    }

    public void GrantAll()
    {
        foreach (var id in ItemIds.All)
        {
            Grant(id);
        }
        Debug.Log("[Item] 치트: 아이템 전량 지급");
    }

    [ContextMenu("테스트: 주황 렌즈 지급")]
    void TestGrantLens() => Grant(ItemIds.LensSun);

    [ContextMenu("테스트: 주황 렌즈 사용")]
    void TestConsumeLens() => Consume(ItemIds.LensSun);

    [ContextMenu("테스트: 보유 아이템 전체 출력")]
    void TestPrintItems()
    {
        Debug.Log($"[Item] 보유 수: {items.Count}");
        foreach (var id in items)
        {
            Debug.Log($" - {id}");
        }
    }
}
/*HashSet 을 ㅎ쓴 이유 :
 기획서 아이템은 전부 1개씩만 존재. 거울 조각도 회로판에 꽂으면 인벤토리에서 ㅂㅂ(Consume),
다시 회수하면 돌아오는(Grant) 구조라서 있다/없다만 알면 충분

 ItemIds를 따로 둔 이유 :
"FRAG_1" 을 코드 여기저기 직접 쓰다가 "FARG_1" 처럼 오타 나면 에러도 안 나고 조용히 안돌아감.
상수로 모아두면 오타가 컴파일 에러로 바로 잡힘.*/