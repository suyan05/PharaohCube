using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 호루스의 방 다듬기 - 씬이 열릴 때 자동으로 적용 (빌더 메뉴를 다시 안 돌려도 됨. HorusPixelFlameAutoAttach가 부름)
// 1. 단서 비석마다 은은한 빛 + 금빛 표식 (HorusSteleBeacon)
// 2. 본당 가운데(제단 둘레)가 물건으로 꽉 차 길이 막히던 것을 정리
//    - 매 석상 2개: 제단 바로 위 -> 북쪽 벽에 붙임. 왼쪽 석상은 서쪽(숨은 회랑 쪽)을 보게 뒤집음
//    - 경고 석판: 제단 남쪽 가운데 길 옆 -> 서쪽 빈 곳으로
//    - 달의 눈 장착대: 길 한가운데 -> 북쪽 벽 동쪽 끝으로 ("벽에 난 홈"처럼)
//    - 제단 레버 4개는 HorusSkyRings에서 남쪽으로 모음
// 3. 돌배 방(동쪽) 벽 횃대는 꺼진 채로 시작, 돌배 퍼즐(H4)을 풀면 켜짐
// 4. 어두워서 물건에 계속 부딪히던 문제: 방 전체 밝기를 올리고, 플레이어 둘레에 은은한 빛을 달아 발밑 물건이 보이게
// 위치/밝기 숫자는 씬 화면 캡처를 보고 정한 값 -> 어색하면 아래 표의 숫자만 고치면 됨
// 물건은 "빌더가 놓은 원래 자리"에 있을 때만 옮김 (에디터에서 손으로 옮겨 둔 것은 건드리지 않음)
public static class HorusRuntimeTune
{
    private const string RoomSconce = "HO_RoomSconce";
    private const string BoatDoneFlag = "F_HO_H4_DONE";
    private const float EastRoomX = 26f;                     // 이보다 동쪽의 방 횃대 = 돌배 방
    private const float GlobalBoost = 1.6f;
    private const string BoostMark = "HorusBrightened";
    private const string PlayerGlow = "HorusPlayerGlow";

    // 이름, 원래 자리, 새 자리
    private static readonly (string name, Vector2 src, Vector2 dst)[] Moves =
    {
        ("OBJ_H12_FalconStatue_L", new Vector2(9.2f, 14.4f), new Vector2(8.2f, 15.25f)),
        ("OBJ_H12_FalconStatue_R", new Vector2(14.8f, 14.4f), new Vector2(15.8f, 15.25f)),
        ("OBJ_H11_WarningSlab", new Vector2(10.0f, 8.4f), new Vector2(6.2f, 9.2f)),
        ("OBJ_H04_MoonEyeMount", new Vector2(19.5f, 7.6f), new Vector2(20.8f, 15.2f)),
    };

    public static void Apply()
    {
        try
        {
            if (UnityEngine.Object.FindFirstObjectByType<HorusDirector>(FindObjectsInactive.Include) == null) return; // 호루스 씬이 아님
            foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Tune(t);
            Brighten();
            GlowAroundPlayer();
        }
        catch (Exception e)
        {
            Debug.LogError($"[호루스 방 다듬기] 오류: {e}");
        }
    }

    private static void Tune(Transform t)
    {
        string n = t.name;
        Relocate(t, n);
        if (n.StartsWith("OBJ_HS", StringComparison.Ordinal) || n == "OBJ_H11_WarningSlab") AddBeacon(t);
        else if (n == "OBJ_H12_FalconStatue_L") FaceWest(t);
        else if (n == RoomSconce && t.position.x > EastRoomX) SleepSconce(t);
    }

    private static void Relocate(Transform t, string n)
    {
        foreach ((string name, Vector2 src, Vector2 dst) m in Moves)
        {
            if (m.name != n || Vector2.Distance(t.position, m.src) > 0.6f) continue;
            t.position = new Vector3(m.dst.x, m.dst.y, t.position.z);
            return;
        }
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

    // 플레이어를 따라다니는 넓고 옅은 빛 (횃불이 닿지 않는 곳에서도 발밑 물건이 보이게)
    private static void GlowAroundPlayer()
    {
        Transform player = CineActor.Player();
        if (player == null || player.Find(PlayerGlow) != null) return;
        var go = new GameObject(PlayerGlow, typeof(Light2D));
        go.transform.SetParent(player, false);
        Light2D l = go.GetComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = new Color(0.78f, 0.84f, 1f);
        l.pointLightOuterRadius = 7.5f;
        l.pointLightInnerRadius = 1.5f;
        l.falloffIntensity = 0.9f;
        l.intensity = 0.5f;
    }
}
