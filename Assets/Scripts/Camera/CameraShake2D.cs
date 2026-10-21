using System;
using UnityEngine;

// 카메라 흔들림 (붕괴, 계단 열림 등)
// - CameraShake2D.Set(0.1f) = 흔들기 시작/세기 변경 (유닛), Set(0f) = 멈춤
// - Main Camera에 자동으로 붙음 (씬에 놓을 필요 없음)
// - CameraFollow2D와 안 싸우게: 매 프레임 Update에서 지난 흔들림을 빼고,
//   CameraFollow2D가 위치를 정한 "뒤"(실행 순서 1000) LateUpdate에서 새 흔들림을 더함
[DefaultExecutionOrder(1000)]
public class CameraShake2D : MonoBehaviour
{
    private static CameraShake2D instance;

    private Vector3 applied;
    private float amplitude;

    public static void Set(float newAmplitude)
    {
        try
        {
            CameraShake2D shake = Find();
            if (shake != null) shake.amplitude = Mathf.Max(0f, newAmplitude);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CameraShake2D] 흔들림 설정 오류: {e}");
        }
    }

    private static CameraShake2D Find()
    {
        if (instance != null) return instance;

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[CameraShake2D] Main Camera가 없음 -> 흔들림 생략");
            return null;
        }

        instance = cam.GetComponent<CameraShake2D>();
        if (instance == null) instance = cam.gameObject.AddComponent<CameraShake2D>();
        return instance;
    }

    private void Update()
    {
        RemoveOffset();
    }

    private void LateUpdate()
    {
        if (amplitude <= 0f) return;

        Vector2 r = UnityEngine.Random.insideUnitCircle * amplitude;
        applied = new Vector3(r.x, r.y, 0f);
        transform.position += applied;
    }

    private void RemoveOffset()
    {
        if (applied == Vector3.zero) return;
        transform.position -= applied;
        applied = Vector3.zero;
    }

    private void OnDisable()
    {
        RemoveOffset();
        amplitude = 0f;
    }
}
