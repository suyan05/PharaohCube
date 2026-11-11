using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// [히든] 라의 방 숨은 길 입구 - 태양의 배 모형이 있던 자리 밑의 뚜껑돌
// - 라의 불씨를 얻어 배 모형이 사라지면(F_RA_EMBER_FOUND) 그 자리에 뚜껑돌과 새어 나오는 빛이 드러남
// - E -> 카메라가 확 다가가며 땅이 울림 -> 옆모습 통로 "밤의 뱃길" -> 끝 방 벽화 컷 (HiddenVault)
// - HiddenVaultSpawner가 씬이 열릴 때 자동으로 만들어 둠 (배치 메뉴를 다시 안 돌려도 됨)
[RequireComponent(typeof(BoxCollider2D))]
public class RaHiddenHatch : MonoBehaviour, IInteractable
{
    private const string EmberFlag = "F_RA_EMBER_FOUND";
    private const string Art = "Horus/PixelFx/RA_Hatch";

    private SpriteRenderer sr;
    private Light2D glow;
    private BoxCollider2D box;
    private bool open, busy;
    private float pollTimer, clock;

    private void Awake()
    {
        try
        {
            box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.9f, 0.9f);
            BuildVisual();
            Show(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"[라 숨은 길] 준비 오류: {e}");
        }
    }

    private void BuildVisual()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>(Art);
        Transform player = CineActor.Player();
        SpriteRenderer p = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        if (p != null)
        {
            sr.sortingLayerID = p.sortingLayerID;
            sr.sortingOrder = p.sortingOrder - 1; // 바닥에 깔린 돌이라 플레이어보다 뒤
            sr.sharedMaterial = p.sharedMaterial; // 방의 어둠/조명을 똑같이 받게
        }
        var lightGo = new GameObject("HatchGlow", typeof(Light2D));
        lightGo.transform.SetParent(transform, false);
        glow = lightGo.GetComponent<Light2D>();
        glow.lightType = Light2D.LightType.Point;
        glow.color = new Color(1f, 0.72f, 0.35f);
        glow.pointLightOuterRadius = 1.8f;
        glow.pointLightInnerRadius = 0.2f;
        glow.intensity = 0.8f;
    }

    private void Show(bool on)
    {
        open = on;
        if (sr != null) sr.enabled = on;
        if (glow != null) glow.gameObject.SetActive(on);
        if (box != null) box.enabled = on; // 배 모형이 있는 동안엔 E 조사를 가로채지 않게 꺼둠
    }

    private void Update()
    {
        clock += Time.deltaTime;
        pollTimer -= Time.deltaTime;
        if (pollTimer <= 0f)
        {
            pollTimer = 0.5f;
            bool found = HiddenVault.Seen(EmberFlag);
            if (found != open) Show(found);
        }
        if (open && glow != null) glow.intensity = 0.6f + 0.35f * Mathf.PerlinNoise(Mathf.Floor(clock * 10f) * 0.37f, 0.5f);
    }

    public string GetPrompt()
    {
        if (!open || busy || Cine.Running) return null;
        return HiddenVault.Seen(HiddenVault.RaFlag) ? "E: 밤의 뱃길로 다시 내려가기" : "E: 바닥의 뚜껑돌 살펴보기";
    }

    public void Interact(GameObject who)
    {
        if (!open || busy || Cine.Running || DialogueBox.IsOpen) return;
        busy = true;
        Cine.Run(Sequence());
    }

    private IEnumerator Sequence()
    {
        Cine.Begin(true);
        if (!HiddenVault.Seen(HiddenVault.RaFlag))
        {
            yield return Cine.Say("나", "배 모형이 있던 자리 밑에... 뚜껑돌?", "틈으로 따뜻한 바람이 올라온다. 밀어 보자.");
        }
        GameAudio.Play("sfx_rumble", 0.6f);
        yield return CineShots.Vertigo(transform.position, 3f, 1f);
        yield return HiddenVault.Play(HiddenVaultScenes.RaWalk(), HiddenVaultScenes.RaReveal(), HiddenVault.RaFlag, HiddenVaultScenes.RaAfter);
        Cine.End();
        busy = false;
    }
}
