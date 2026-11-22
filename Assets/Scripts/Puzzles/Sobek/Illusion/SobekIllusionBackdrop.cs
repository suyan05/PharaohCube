using UnityEngine;

// [추가] 착시 퍼즐 뒤 배경 그림 (예전: 하늘색 단색 -> 퍼즐마다 어울리는 그림)
// - 카메라 자식으로 화면을 꽉 채우는 판 2장: 배경 그림(맨 뒤) + 물띠(그 앞, 반투명)
// - 물띠는 물높이에 따라 위아래로 움직임 -> 물높이 퍼즐에서 물이 차오르면 배경의 물도 같이 차오름
// - 블록·물보다 항상 뒤에 그려짐 (렌더 큐 1000/1001, 깊이 안 씀)
// 붙이는 곳: 자동 (SobekIllusionBoard가 카메라에 만들어 붙임). 그림: Resources/Sobek/Illusion/Sobek_Backdrop_{S1~S4, M1~M3}
[RequireComponent(typeof(Camera))]
public class SobekIllusionBackdrop : MonoBehaviour
{
    private const string ShaderName = "Sobek/IllusionUnlit";

    [Header("물띠 (물높이에 따라 움직임)")]
    [Tooltip("기본 물높이일 때 물띠 높이 (화면 아래에서부터 비율)")]
    [Range(0f, 1f)] [SerializeField] private float bandBase = 0.16f;
    [Tooltip("물이 1칸 오를 때 물띠가 오르는 비율")]
    [Range(0f, 0.5f)] [SerializeField] private float bandPerUnit = 0.14f;
    [Tooltip("물띠 높이가 바뀌는 속도 (초당 비율)")]
    [Range(0.1f, 3f)] [SerializeField] private float bandSpeed = 0.6f;

    private Camera cam;
    private Transform back, band;
    private Material backMat, bandMat;
    private Texture2D bandTex;
    private Texture current;
    private float baseWaterY, targetFrac, frac = -1f;

    // 카메라에 붙은 배경을 찾거나 새로 만듦
    public static SobekIllusionBackdrop For(Camera c)
    {
        if (c == null) return null;
        var b = c.GetComponent<SobekIllusionBackdrop>();
        if (b == null) b = c.gameObject.AddComponent<SobekIllusionBackdrop>();
        return b;
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Shader sh = Shader.Find(ShaderName);
        if (sh == null) { Debug.LogError($"[SobekIllusionBackdrop] {ShaderName} 셰이더 없음 -> 배경 그림 생략", this); enabled = false; return; }

        backMat = MakeMat(sh, false, 1000);
        bandMat = MakeMat(sh, true, 1001);
        bandTex = MakeBandTexture();
        bandMat.SetTexture("_MainTex", bandTex);
        back = MakeQuad("Backdrop", backMat);
        band = MakeQuad("WaterBand", bandMat);
        band.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (backMat != null) Destroy(backMat);
        if (bandMat != null) Destroy(bandMat);
        if (bandTex != null) Destroy(bandTex);
    }

    // 퍼즐 판이 켜질 때: 그림 + 물 색 + 기본 물높이
    public void Apply(Texture painting, Color waterColor, float baseWater, float water)
    {
        if (backMat == null) return;
        current = painting;
        backMat.SetTexture("_MainTex", painting != null ? painting : Texture2D.whiteTexture);
        backMat.SetColor("_Color", painting != null ? Color.white : cam.backgroundColor);
        Color bc = waterColor; bc.a = 1f;
        bandMat.SetColor("_Color", Color.Lerp(bc, Color.white, 0.12f));
        baseWaterY = baseWater;
        band.gameObject.SetActive(true);
        frac = -1f; // 처음엔 바로 그 높이로
        SetWater(water);
        Debug.Log($"[SobekIllusionBackdrop] 배경 그림: {(painting != null ? painting.name : "없음 (단색)")}", this);
    }

    // 물높이 (월드 y) -> 물띠 목표 높이
    public void SetWater(float waterY)
    {
        targetFrac = Mathf.Clamp(bandBase + (waterY - baseWaterY) * bandPerUnit, 0f, 0.95f);
        if (frac < 0f) frac = targetFrac;
    }

    private void LateUpdate()
    {
        if (cam == null || back == null) return;
        float h = cam.orthographicSize * 2f, w = h * cam.aspect;
        float z = cam.farClipPlane * 0.95f;

        // 배경: 화면을 꽉 채우고 그림 비율이 다르면 넘치는 쪽을 잘라 냄 (cover)
        back.localPosition = new Vector3(0f, 0f, z);
        back.localScale = new Vector3(w, h, 1f);
        if (current != null && current.height > 0)
        {
            float texAspect = (float)current.width / current.height, scrAspect = w / Mathf.Max(0.01f, h);
            Vector2 scale = scrAspect > texAspect ? new Vector2(1f, texAspect / scrAspect) : new Vector2(scrAspect / texAspect, 1f);
            backMat.SetTextureScale("_MainTex", scale);
            backMat.SetTextureOffset("_MainTex", (Vector2.one - scale) * 0.5f);
        }

        // 물띠: 화면 아래에서 frac 높이까지 (위 끝은 부드럽게)
        frac = Mathf.MoveTowards(frac, targetFrac, bandSpeed * Time.deltaTime);
        float bh = Mathf.Max(0.001f, h * frac);
        band.localPosition = new Vector3(0f, -h * 0.5f + bh * 0.5f, z * 0.98f);
        band.localScale = new Vector3(w, bh, 1f);
    }

    private Material MakeMat(Shader sh, bool transparent, int queue)
    {
        var m = new Material(sh) { name = transparent ? "SobekBackdropBand (runtime)" : "SobekBackdrop (runtime)" };
        m.SetFloat("_SrcBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_DstBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : (float)UnityEngine.Rendering.BlendMode.Zero);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Cull", 0f);
        m.renderQueue = queue;
        return m;
    }

    private Transform MakeQuad(string objName, Material mat)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform, false);
        var mesh = new Mesh { name = objName };
        mesh.SetVertices(new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) });
        mesh.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
        mesh.SetColors(new[] { Color.white, Color.white, Color.white, Color.white });
        mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f); // 화면 밖으로 잘리지 않게
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }

    // 물띠 그림: 아래는 진하고 위로 갈수록 투명 + 맨 위에 밝은 수면 선
    private static Texture2D MakeBandTexture()
    {
        const int n = 64;
        var t = new Texture2D(1, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "SobekBandGradient" };
        for (int y = 0; y < n; y++)
        {
            float v = y / (n - 1f);
            float a = Mathf.Lerp(0.78f, 0.42f, v);
            float shade = Mathf.Lerp(0.7f, 1f, v);
            Color c = new Color(shade, shade, shade, a);
            if (y >= n - 3) c = new Color(1f, 1f, 1f, 0.85f); // 수면 반짝임
            t.SetPixel(0, y, c);
        }
        t.Apply(false, true);
        return t;
    }
}
