using System;
using UnityEngine;

// H5 클리어 보상: 호루스 형판 + '하늘' 키워드 (라의 방 SunCyclePuzzle 보상과 같은 방식)
public class HorusAltarReward : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private SkyAlignPuzzle puzzle;

    [Header("보상")]
    [SerializeField] private string plateItem = HorusItemIds.PlateHorus;
    [SerializeField] private string keyword = "하늘";

    [Header("클리어 연출 (선택: 제단 색 변경)")]
    [SerializeField] private SpriteRenderer altarRenderer;
    [SerializeField] private Color clearedColor = new Color(0.96f, 0.95f, 0.91f);

    private void OnEnable()
    {
        if (puzzle == null)
        {
            Debug.LogError("[제단 보상] Puzzle 연결 필요");
            return;
        }
        puzzle.OnCompleted += GrantReward;
    }

    private void OnDisable()
    {
        if (puzzle != null) puzzle.OnCompleted -= GrantReward;
    }

    private void GrantReward()
    {
        try
        {
            if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(plateItem);
            else Debug.LogWarning($"[제단 보상] ItemInventory가 없어 형판 지급 실패: {plateItem}");
            Debug.Log($"[H5] 호루스 형판({plateItem}) 획득 + '{keyword}' 키워드");
            if (altarRenderer != null) altarRenderer.color = clearedColor;
        }
        catch (Exception e)
        {
            Debug.LogError($"[제단 보상] 지급 오류: {e}");
        }
    }
}