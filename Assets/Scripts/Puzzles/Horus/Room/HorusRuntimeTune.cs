using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 호루스의 방 다듬기 - 씬이 열릴 때 자동으로 적용 (빌더 메뉴를 다시 안 돌려도 됨. HorusPixelFlameAutoAttach가 부름)
// 1. 단서 비석마다 은은한 빛 + 금빛 표식 (HorusSteleBeacon)
// 2. 매 석상이 동쪽을 보고 있던 것 -> 왼쪽 석상을 서쪽(숨은 회랑 쪽)으로 뒤집음
// 3. 길 한가운데 있던 달의 눈 장착대 -> 동쪽 벽에 붙임
// 4. 돌배 방(동쪽) 벽 횃대는 꺼진 채로 시작, 돌배 퍼즐(H4)을 풀면 켜짐
// 5. 방 전체 밝기를 조금 올림 (너무 어두워 물건이 안 보이던 문제)
// 위치/밝기 숫자는 플레이를 못 해보고 정한 값 -> 어색하면 아래 상수만 고치면 됨
public static class HorusRuntimeTune
{
    private const string LeftStatue = "OBJ_H12_FalconStatue_L";
    private const string MoonMount = "OBJ_H04_MoonEyeMount";
    private const string RoomSconce = "HO_RoomSconce";
    private const string BoatDoneFlag = "F_HO_H4_DONE";
    private const float EastRoomX = 26f;                     // 이보다 동쪽의 방 횃대 = 돌배 방
    private static readonly Vector2 MoonOld = new Vector2(19.5f, 7.6f);
    private static readonly Vector2 MoonNew = new Vector2(23.4f, 4.4f); // 본당 동쪽 벽 바로 앞 (성도판 고리 아래쪽)
    private const float GlobalBoost = 1.35f;
    private const string BoostMark = "HorusBrightened";

    public static void Apply()
    {
        try
        {
            if (UnityEngine.Object.FindFirstObjectByType<HorusDirector>(FindObjectsInactive.Include) == null) return; // 호루스 씬이 아님
            foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Tune(t);
            Brighten();
        }
        catch (Exception e)
        {
            Debug.LogError($"[호루스 방 다듬기] 오류: {e}");
        }
    }

    private static void Tune(Transform t)
    {
        string n = t.name;
        if (n.StartsWith("OBJ_HS", StringComparison.Ordinal) || n == "OBJ_H11_WarningSlab") AddBeacon(t);
        else if (n == LeftStatue) FaceWest(t);
        else if (n == MoonMount) MoveMoonMount(t);
        else if (n == RoomSconce && t.position.x > EastRoomX) SleepSconce(t);
    }

    private static void AddBeacon(Transform t)
    {
        if (t.GetComponent<HorusSteleBeacon>() == null && t.GetComponent<SpriteRenderer>() != null) t.gameObject.AddComponent<HorusSteleBeacon>();
    }

    private static void FaceWest(Transform t)
    {
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = true;
    }

    // 빌더가 놓은 자리 그대로일 때만 옮김 (손으로 옮겨 둔 경우는 존중)
    private static void MoveMoonMount(Transform t)
    {
        if (Vector2.Distance(t.position, MoonOld) > 0.6f) return;
        t.position = new Vector3(MoonNew.x, MoonNew.y, t.position.z);
    }

    private static void SleepSconce(Transform t)
    {
        HorusPixelFlame flame = t.GetComponent<HorusPixelFlame>();
        if (flame != null) flame.RequireFlag(BoatDoneFlag);
    }

    private static void Brighten()
    {
        foreach (Light2D l in UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l.lightType != Light2D.LightType.Global || l.transform.Find(BoostMark) != null) continue;
            l.intensity *= GlobalBoost;
            new GameObject(BoostMark).transform.SetParent(l.transform, false);
        }
    }
}
