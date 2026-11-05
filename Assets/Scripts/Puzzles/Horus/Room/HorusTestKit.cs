using System;
using UnityEngine;

// [테스트용] 호루스 씬을 바로 Play했을 때만, 이 방에 오기 전까지 원래 들고 있어야 할 도구를 줌
// - 프롤로그(스핑크스)부터 쭉 플레이하면 돋보기/붓/횃불이 이미 있으니 아무것도 안 함
// - "바로 Play"인지는 라의 방을 끝냈는지(F_RA_PLATE_GIVEN)로 판단. 빌드(실제 게임)에서는 동작 안 함
public class HorusTestKit : MonoBehaviour
{
    [SerializeField] private bool grantInEditor = true;
    [SerializeField] private string progressFlag = "F_RA_PLATE_GIVEN";
    [SerializeField] private string[] items = { "TOOL_LENS", "TOOL_BRUSH", "ITEM_TORCH" };

    private void Start()
    {
        try
        {
            if (!Application.isEditor || !grantInEditor) return;
            if (HorusDirector.HasFlag(progressFlag) || ItemInventory.Instance == null) return;
            foreach (string id in items)
            {
                if (!ItemInventory.Instance.Has(id)) ItemInventory.Instance.Grant(id);
            }
            Debug.Log("[HorusTestKit] 호루스 씬 바로 테스트 -> 돋보기, 붓, 횃불 지급 (에디터에서만)");
        }
        catch (Exception e)
        {
            Debug.LogError($"[HorusTestKit] 오류: {e}");
        }
    }
}
