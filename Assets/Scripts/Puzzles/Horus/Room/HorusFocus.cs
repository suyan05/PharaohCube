using UnityEngine;

// 지금 화면 가운데에 무언가 떠 있는지 (단서 패널, 상형문자 해독, 퍼즐 화면, 연출)
// -> 목표 문구 / 매 길잡이 화살표를 숨겨서 집중하게 함 (HorusObjectiveGuide, HorusFalconGuide가 봄)
public static class HorusFocus
{
    private static CluePanel panel;
    private static GameObject[] overlays;
    private static float nextFind;

    // HorusDirector가 퍼즐 화면 목록을 알려줌
    public static void SetOverlays(GameObject[] list) => overlays = list;

    public static bool PanelOpen
    {
        get
        {
            if (Cine.Running || PlayerLock.IsLocked || PuzzleOverlayFocus.IsActive) return true;
            if (ClueOpen()) return true;
            if (overlays == null) return false;
            foreach (GameObject o in overlays) if (o != null && o.activeInHierarchy) return true;
            return false;
        }
    }

    private static bool ClueOpen()
    {
        if (panel == null && Time.unscaledTime >= nextFind)
        {
            panel = Object.FindFirstObjectByType<CluePanel>(FindObjectsInactive.Include);
            nextFind = Time.unscaledTime + 2f;
        }
        return panel != null && panel.IsOpen;
    }
}
