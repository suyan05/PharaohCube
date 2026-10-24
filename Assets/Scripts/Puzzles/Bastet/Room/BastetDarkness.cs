using UnityEngine;
using UnityEngine.Rendering.Universal;

// [추가] 바스테트 신전 어둠 (씬에 1개, BastetSystems 오브젝트)
// - 시작할 때 전역 조명(Global Light 2D)을 "어둡지만 바닥·벽 윤곽은 보이는" 밝기로 맞춤
//   (라의 방 0.03보다 조금 밝게. 방은 플레이어가 벽 횃불에 불을 붙여야 밝아짐)
// - 한 번만 맞춤 -> B5 해와 달 퍼즐의 밤 연출(전역 조명을 더 어둡게)은 그대로 동작
[DefaultExecutionOrder(-200)]
public class BastetDarkness : MonoBehaviour
{
    [Header("밝기 (0 = 완전 깜깜, 라의 방 낮 = 0.03)")]
    [Range(0f, 1f)] [SerializeField] private float intensity = 0.12f;
    [Tooltip("어둠 색 (푸르스름하면 밤 신전 느낌, 횃불 주황빛과 대비)")]
    [SerializeField] private Color color = new Color(0.62f, 0.62f, 0.82f);

    private void Awake() => Apply();

    [ContextMenu("테스트: 지금 밝기 다시 적용")]
    public void Apply()
    {
        int count = 0;
        foreach (Light2D l in FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l.lightType != Light2D.LightType.Global) continue;
            l.intensity = intensity;
            l.color = color;
            count++;
        }
        if (count == 0) Debug.LogWarning("[BastetDarkness] Global Light 2D가 없음 -> 씬에 하나 추가 (Light 2D > Global Light 2D)", this);
        else Debug.Log($"[BastetDarkness] 전역 조명 {count}개 밝기 {intensity}", this);
    }

#if UNITY_EDITOR
    // 에디터에서 슬라이더를 움직이면 Play 중에 바로 보이게
    private void OnValidate()
    {
        if (Application.isPlaying) Apply();
    }
#endif
}
