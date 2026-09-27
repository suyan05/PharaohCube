using System;
using UnityEngine;

public class RaPuzzleManager : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform lightEmitter;
    [SerializeField] private Transform altarTarget;

    [Header("광선 설정")]
    [SerializeField] private int maxBounces = 5;
    [SerializeField] private float maxDistance = 30f;

    public event Action OnPuzzleCleared;
    public event Action<bool> OnLightStateChanged;

    private bool isCleared = false;

    private void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        CalculateLightPath();
    }

    private void CalculateLightPath()
    {
        if (lightEmitter == null) return;

        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, lightEmitter.position);

        Vector3 rayOrigin = lightEmitter.position;
        Vector3 rayDirection = lightEmitter.forward;
        bool hitAltarThisFrame = false;

        for (int i = 0; i < maxBounces; i++)
        {
            Ray ray = new Ray(rayOrigin, rayDirection);

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, hit.point);

                // 1. 목표 제단에 빛이 닿았는지 판정
                if (hit.transform == altarTarget)
                {
                    hitAltarThisFrame = true;
                    if (!isCleared)
                    {
                        isCleared = true;
                        Debug.Log("<color=yellow>[RaPuzzle] 제단에 빛이 도달했습니다! 라의 방 클리어!</color>");
                        OnPuzzleCleared?.Invoke();
                    }
                    break;
                }

                // 2. 거울 충돌 및 복구 상태 확인
                RaMirror mirror = hit.collider.GetComponentInParent<RaMirror>();
                if (mirror != null && mirror.IsRepaired)
                {
                    rayDirection = Vector3.Reflect(rayDirection, hit.normal);
                    rayOrigin = hit.point + (rayDirection * 0.01f); 
                }
                else
                {
                    break;
                }
            }
            else
            {
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, rayOrigin + rayDirection * maxDistance);
                break;
            }
        }

        OnLightStateChanged?.Invoke(hitAltarThisFrame);
    }
}