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
    private bool shown = true;
    private float poll;

    private void Start() => Refresh();

    // [추가] 다른 길(치트 "아이템 전부 지급" 등)로 이빨을 받았어도 바닥의 이빨이 바로 사라지게 (0.5초마다 확인)
    private void Update()
    {
        if (!shown || taking) return;
        poll -= Time.deltaTime;
        if (poll > 0f) return;
        poll = 0.5f;
        if (Got) { Refresh(); Debug.Log("[SobekHiddenPickup] 이미 가진 이빨 -> 바닥의 이빨 숨김", this); }
    }

    private bool Got => SobekStory.HasFlag(gotFlag) || (ItemInventory.Instance != null && ItemInventory.Instance.Has(itemId));

    private void Refresh()
    {
        shown = !Got;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = shown;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = shown;
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
            AnubisToast.Show("소베크의 이빨을 얻었다.", 2.5f); // [추가] 얻은 것을 알 수 있게
            Debug.Log("[SobekHiddenPickup] 히든 아이템 획득: 소베크의 이빨", this);
            SobekStory.Say(afterLines);
        });
    }
}
