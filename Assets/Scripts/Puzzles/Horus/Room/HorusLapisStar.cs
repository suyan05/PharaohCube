using System;
using UnityEngine;

// 청금석 별 (기획서 FUN-4 수집 요소, OBJ_H16 x3). 방 구석에 숨은 파란 별
// - E로 줍기 -> 반짝 + "청금석 별 n / 3" 알림. 3개 다 모으면 F_HO_LAPIS_ALL -> 호루스가 인정하는 대사 (HorusCutscenes)
// - 주운 별은 플래그로 저장 -> 다시 들어와도 사라진 채
[RequireComponent(typeof(Collider2D))]
public class HorusLapisStar : MonoBehaviour, IInteractable
{
    [SerializeField] private int starId = 1;
    [SerializeField] private int total = 3;
    [SerializeField] private string allFlag = "F_HO_LAPIS_ALL";

    private string MyFlag => $"F_HO_LAPIS_{starId}";

    private void Start()
    {
        if (HorusDirector.HasFlag(MyFlag)) gameObject.SetActive(false);
    }

    public string GetPrompt() => "E: 청금석 별 줍기";

    public void Interact(GameObject player)
    {
        try
        {
            HorusDirector.SetFlag(MyFlag);
            int got = Count();
            GameAudio.Play("sfx_item_get", 0.6f);
            HorusFx.MatchPlayer();
            HorusFx.StarBurst(transform.position, new Vector2(0.3f, 0.3f), 14, new Color(0.55f, 0.75f, 1f));
            AnubisToast.Show($"청금석 별 {got} / {total}", 2.5f);
            if (got >= total) HorusDirector.SetFlag(allFlag);
            gameObject.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"[청금석 별] 줍기 오류: {e}");
        }
    }

    private int Count()
    {
        int n = 0;
        for (int i = 1; i <= total; i++) if (HorusDirector.HasFlag($"F_HO_LAPIS_{i}")) n++;
        return n;
    }
}
