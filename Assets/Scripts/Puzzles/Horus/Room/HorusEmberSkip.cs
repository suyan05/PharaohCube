using UnityEngine;

// 라의 방 히든 아이템 '라의 불씨'(태양의 배 모형에서 얻음)를 가져왔으면 이 오브젝트는 필요 없음 -> 숨김
// - 입구 전실 횃불 제단에 붙임. 불씨가 있으면 횃불이 꺼지지 않고, 꺼져도 E 길게로 다시 붙음 (PlayerTorchTemp)
// - 불씨가 없으면 그대로 횃불 제단에서 불을 얻음
public class HorusEmberSkip : MonoBehaviour
{
    [SerializeField] private string emberItem = "ITEM_RA_EMBER";

    private void Start()
    {
        if (ItemInventory.Instance != null && ItemInventory.Instance.Has(emberItem)) gameObject.SetActive(false);
    }
}
