using UnityEngine;

// 숨은 방의 히든 아이템 (소베크의 이빨) + 마지막 문장
// - E: 획득 팝업(ItemPopup) -> 케프리 대사 -> 다음 신전 예고 문장
// - 저장: ITEM_SOBEK_TOOTH(인벤토리, 업적 히든 목록) + HIDDEN_SobekTooth_GOT(HiddenItemService 규칙)
// - 효과: 아누비스 심판에서 실패해도 메인 퍼즐 재도전 1회 (아누비스 담당 HiddenEffects가 이 아이템을 확인)
[RequireComponent(typeof(Collider2D))]
public class SobekHiddenPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemId = SobekIds.Tooth;
    [SerializeField] private string gotFlag = SobekIds.FlagToothGot;
    [SerializeField] private string[] afterLines =
    {
        SobekStory.Khepri, "소베크의 이빨이다. 강의 악어가 너를 한 번 더 지켜 줄 거야.",
        SobekStory.Me, "벽에 마지막 문장이 새겨져 있다. 「강은 다시 흐른다. 이제 자칼이 마지막 길을 연다.」",
    };

    private bool taking;

    private void Start() => Refresh();

    private bool Got => SobekStory.HasFlag(gotFlag) || (ItemInventory.Instance != null && ItemInventory.Instance.Has(itemId));

    private void Refresh()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = !Got;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = !Got;
    }

    public string GetPrompt() => Got || taking ? "" : "E: 빛나는 이빨 줍기";

    public void Interact(GameObject player)
    {
        if (Got || taking || SobekStory.Busy) return;
        taking = true;
        ItemPopup.Grant(itemId, () =>
        {
            SobekStory.SetFlag(gotFlag);
            taking = false;
            Refresh();
            Debug.Log("[SobekHiddenPickup] 히든 아이템 획득: 소베크의 이빨", this);
            SobekStory.Say(afterLines);
        });
    }
}
