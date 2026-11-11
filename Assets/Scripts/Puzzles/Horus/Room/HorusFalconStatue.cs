using System;
using UnityEngine;

// 한쪽 눈이 빈 매 석상 (기획서 OBJ_H12 왼쪽). 매의 회랑을 여는 열쇠 자리
// - 우자트 부적이 없으면: "한쪽 눈이 비어 있다" (복선)
// - 있으면: 부적을 쓰고 회랑 여는 연출 (HorusHiddenGallery)
// - 열린 뒤: 눈이 빛나고 있다는 대사만
// [확인 필요] 우자트 부적은 토트의 방 메인 보상. 아이템 ID는 토트 담당 쪽과 맞추기 (지금은 HorusItemIds.WedjatAmulet)
[RequireComponent(typeof(Collider2D))]
public class HorusFalconStatue : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [SerializeField] private HorusHiddenGallery gallery;
    [SerializeField] private string amuletItem = HorusItemIds.WedjatAmulet;

    [Header("대사")]
    [SerializeField] private DialogueLine[] emptyEyeLines =
    {
        new DialogueLine("나", "매 석상의 한쪽 눈이 비어 있다."),
        new DialogueLine("나", "눈 모양의 무언가를 끼울 수 있을 것 같은데... 지금은 가진 게 없어."),
        new DialogueLine("나", "그런데 이 석상... 고개가 서쪽 벽 쪽으로 돌아가 있네.")
    };
    [SerializeField] private DialogueLine[] openedLines =
    {
        new DialogueLine("나", "석상의 두 눈이 은은하게 빛나고 있다. 서쪽 벽 너머를 바라보는 것 같다.")
    };

    // [수정] 우자트 부적은 토트의 방 보상이라 지금은 얻을 길이 없어 회랑이 영영 안 열렸음
    //        -> 이 방의 우자트 석탁 퍼즐(H3)로 "호루스의 눈"을 되찾으면 그것으로도 열림
    [SerializeField] private string eyeRestoredFlag = "F_HO_H3_DONE";

    private bool HasAmulet => ItemInventory.Instance != null && ItemInventory.Instance.Has(amuletItem);
    private bool EyeRestored => GameManager.Instance != null && GameManager.Instance.HasFlag(eyeRestoredFlag);
    private bool CanOpen => HasAmulet || EyeRestored;
    private bool IsOpen => gallery != null && gallery.IsOpen;

    public string GetPrompt()
    {
        if (IsOpen) return "E: 매 석상 보기";
        return CanOpen ? "E: 되찾은 눈을 석상에 끼우기" : "E: 매 석상 조사";
    }

    public void Interact(GameObject player)
    {
        try
        {
            if (DialogueBox.IsOpen || Cine.Running) return;
            if (IsOpen)
            {
                DialogueBox.Play(openedLines);
                return;
            }
            if (!CanOpen || gallery == null)
            {
                DialogueBox.Play(emptyEyeLines);
                return;
            }
            if (HasAmulet) ItemInventory.Instance.Consume(amuletItem);
            gallery.PlayOpenCutscene();
        }
        catch (Exception e)
        {
            Debug.LogError($"[매 석상] Interact 오류: {e}");
        }
    }

    [ContextMenu("치트: 우자트 부적 지급")]
    private void CheatAmulet()
    {
        if (ItemInventory.Instance != null && !ItemInventory.Instance.Has(amuletItem)) ItemInventory.Instance.Grant(amuletItem);
    }
}
